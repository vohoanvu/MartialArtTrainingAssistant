namespace VideoAnalysis.Server.Configuration
{
    /// <summary>
    /// Strongly-typed options for the GCP Transcoder API integration that produces a browser-playable
    /// H.264/AAC copy of each uploaded tape (HEVC tapes are otherwise unrenderable in browsers).
    /// Bound from the <c>Transcoder</c> configuration section; environment overrides use the standard
    /// ASP.NET nesting delimiter, e.g. <c>Transcoder__Location</c>, <c>Transcoder__AutoTranscodeOnUpload</c>.
    /// </summary>
    public class TranscoderOptions
    {
        public const string SectionName = "Transcoder";

        /// <summary>Feature flag. When false, transcode endpoints/jobs no-op (playback falls back to the original).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// When true, an analyze-v2 run also enqueues a playback transcode for the uploaded video.
        /// Set false to make transcoding strictly on-demand (the manual "Convert for playback" button),
        /// which avoids paying for tapes that already play in-browser (H.264).
        /// </summary>
        public bool AutoTranscodeOnUpload { get; set; } = true;

        /// <summary>Transcoder API location (regional; the API has no global endpoint). Matches the bucket region.</summary>
        public string Location { get; set; } = "us-central1";

        /// <summary>Output vertical resolution. Width is omitted so the source aspect ratio is preserved.</summary>
        public int HeightPixels { get; set; } = 720;

        /// <summary>H.264 target bitrate (bps). ~2.5 Mbps is a sensible 720p default.</summary>
        public int BitrateBps { get; set; } = 2_500_000;

        /// <summary>Output frame rate.</summary>
        public int FrameRate { get; set; } = 30;

        /// <summary>AAC audio bitrate (bps).</summary>
        public int AudioBitrateBps { get; set; } = 64_000;

        /// <summary>Seconds between job-state polls.</summary>
        public int PollIntervalSeconds { get; set; } = 15;

        /// <summary>Maximum minutes to wait for a job before marking it Failed.</summary>
        public int TimeoutMinutes { get; set; } = 30;
    }
}
