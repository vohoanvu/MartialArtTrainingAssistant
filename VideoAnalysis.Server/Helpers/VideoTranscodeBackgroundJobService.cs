using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoAnalysis.Server.Configuration;
using VideoAnalysis.Server.Domain.GoogleCloudStorageService;
using VideoAnalysis.Server.Domain.TranscodeService;
using VideoAnalysis.Server.Domain.YoutubeSharingService;

namespace VideoAnalysis.Server.Helpers
{
    /// <summary>
    /// Hangfire job that drives a GCP Transcoder job to completion: creates an H.264/AAC playback
    /// rendition of an uploaded tape, polls until the job terminates, then records the playback path
    /// (or a failure) on the video. Runs on the default queue. DB writes use short-lived scopes so a
    /// long poll loop never holds a pooled connection open.
    /// </summary>
    public class VideoTranscodeBackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVideoTranscodeService _transcoder;
        private readonly IGoogleCloudStorageService _gcs;
        private readonly IHubContext<VideoAnalysisHub> _hub;
        private readonly TranscoderOptions _options;
        private readonly ILogger<VideoTranscodeBackgroundJobService> _logger;

        public VideoTranscodeBackgroundJobService(
            IServiceProvider serviceProvider,
            IVideoTranscodeService transcoder,
            IGoogleCloudStorageService gcs,
            IHubContext<VideoAnalysisHub> hub,
            IOptions<TranscoderOptions> options,
            ILogger<VideoTranscodeBackgroundJobService> logger)
        {
            _serviceProvider = serviceProvider;
            _transcoder = transcoder;
            _gcs = gcs;
            _hub = hub;
            _options = options.Value;
            _logger = logger;
        }

        // Attempts = 0: a thrown exception must NOT trigger Hangfire's default 10 retries — each retry
        // would call CreateJobAsync again (GCP auto-generates a fresh random job name), fanning a single
        // transient blip into up to 10 separate billable Transcoder jobs racing on the same output. The
        // catch already records Failed + notifies; the user retries explicitly via POST /transcode.
        [AutomaticRetry(Attempts = 0)]
        [Queue("transcode")]
        public async Task ProcessTranscodeAsync(int videoId)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Transcoder disabled; skipping transcode for video {VideoId}.", videoId);
                return;
            }

            var inputUri = await GetInputUriAsync(videoId);
            if (inputUri is null)
            {
                _logger.LogWarning("Transcode skipped: video {VideoId} not found or has no FilePath.", videoId);
                return;
            }

            // Output folder per video; the muxed file lands at "{prefix}playback.mp4".
            var outputPrefix = $"gs://{_gcs.BucketName}/transcoded/{videoId}/";
            var playbackPath = $"{outputPrefix}playback.mp4";

            try
            {
                await SetStatusAsync(videoId, TranscodeStatus.Processing, playbackPath: null);
                await NotifyAsync("TranscodeStatusChanged", videoId, TranscodeStatus.Processing.ToString());

                var jobName = await _transcoder.CreateJobAsync(inputUri, outputPrefix, CancellationToken.None);
                var finalState = await PollUntilTerminalAsync(jobName, videoId);

                if (finalState.IsSucceeded)
                {
                    await SetStatusAsync(videoId, TranscodeStatus.Ready, playbackPath);
                    await NotifyAsync("TranscodeReady", videoId, TranscodeStatus.Ready.ToString());
                    _logger.LogInformation("Transcode complete for video {VideoId} → {PlaybackPath}", videoId, playbackPath);
                }
                else
                {
                    await SetStatusAsync(videoId, TranscodeStatus.Failed, playbackPath: null);
                    await NotifyAsync("TranscodeFailed", videoId, TranscodeStatus.Failed.ToString());
                    _logger.LogError("Transcode {State} for video {VideoId}: {Error}",
                        finalState.State, videoId, finalState.Error ?? "(timed out)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transcode job threw for video {VideoId}.", videoId);
                await SetStatusAsync(videoId, TranscodeStatus.Failed, playbackPath: null);
                await NotifyAsync("TranscodeFailed", videoId, TranscodeStatus.Failed.ToString());
                throw; // surface to Hangfire for retry/visibility
            }
        }

        private async Task<TranscodeJobState> PollUntilTerminalAsync(string jobName, int videoId)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.PollIntervalSeconds));
            var maxAttempts = Math.Max(1, (int)(TimeSpan.FromMinutes(Math.Max(1, _options.TimeoutMinutes)) / interval));

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Poll first, then delay — so a fast job is detected on the first check rather than
                // after a wasted interval (the delay sits between polls, not before the first).
                var state = await _transcoder.GetJobAsync(jobName, CancellationToken.None);
                _logger.LogInformation("Transcode video {VideoId} poll {Attempt}/{Max}: {State}",
                    videoId, attempt + 1, maxAttempts, state.State);
                if (state.IsTerminal)
                    return state;
                await Task.Delay(interval, CancellationToken.None);
            }
            // Timed out — report as a non-succeeded, non-error terminal state.
            return new TranscodeJobState("TIMEOUT", $"Job did not complete within {_options.TimeoutMinutes} minutes.");
        }

        private async Task<string?> GetInputUriAsync(int videoId)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var video = await db.Videos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == videoId);
            return string.IsNullOrWhiteSpace(video?.FilePath) ? null : video!.FilePath;
        }

        private async Task SetStatusAsync(int videoId, TranscodeStatus status, string? playbackPath)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var video = await db.Videos.FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null) return;
            video.TranscodeStatus = status;
            if (playbackPath != null) video.PlaybackFilePath = playbackPath;
            await db.SaveChangesAsync();
        }

        private async Task NotifyAsync(string method, int videoId, string status)
        {
            try { await _hub.Clients.All.SendAsync(method, videoId, status); }
            catch (Exception ex) { _logger.LogWarning(ex, "SignalR {Method} notify failed for video {VideoId}", method, videoId); }
        }
    }
}
