using Microsoft.AspNetCore.Mvc;

namespace VideoAnalysis.Server.Domain.AccessControl
{
    /// <summary>The 429 body every quota-guarded endpoint returns, so the client handles them all the same way.</summary>
    public static class AnalysisQuotaResults
    {
        public const string QuotaExceededCode = "ANALYSIS_QUOTA_EXCEEDED";
        public const string InProgressCode = "ANALYSIS_IN_PROGRESS";

        public static IActionResult AnalysisQuotaExceeded(this ControllerBase controller, AnalysisQuotaStatusDto status)
        {
            if (status.ResetsAt is { } resetsAt)
            {
                var seconds = Math.Max(1, (int)Math.Ceiling((resetsAt - DateTime.UtcNow).TotalSeconds));
                controller.Response.Headers.RetryAfter = seconds.ToString();
            }

            var message = status.Limit == 0
                ? "Video analysis is currently paused for demo accounts."
                : $"This free demo allows {status.Limit} video analyses per {status.WindowHours} hours, and you've used them all. Please try again once one frees up.";

            return controller.StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                code = QuotaExceededCode,
                message,
                limit = status.Limit,
                used = status.Used,
                windowHours = status.WindowHours,
                resetsAt = status.ResetsAt,
            });
        }

        public static IActionResult AnalysisInProgress(this ControllerBase controller) =>
            controller.Conflict(new { code = InProgressCode, message = "This video is already being analysed. Results will appear when it finishes." });
    }
}
