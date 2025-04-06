using System.Text.RegularExpressions;

namespace VideoSharing.Server.Domain.YoutubeSharingService
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class YouTubeHelper
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string? ExtractVideoId(string videoUrl)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Regex pattern for standard YouTube links
            var regex = new Regex(@"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})", RegexOptions.IgnoreCase);

            // Try to match the pattern
            var match = regex.Match(videoUrl);

            if (match.Success)
            {
                // Return the captured VideoId
                return match.Groups[1].Value;
            }

            // If no pattern matched, return null or throw an exception as appropriate
            return null;
        }
    }
}