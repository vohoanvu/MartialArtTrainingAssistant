using System.Text.RegularExpressions;

public static class YouTubeHelper
{
    public static string? ExtractVideoId(string videoUrl)
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
