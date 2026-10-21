using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedEntities.Data;
using VideoAnalysis.Server.Configuration;

namespace VideoAnalysis.Server.Domain.AccessControl
{
    /// <summary>
    /// Admin = the user's email is on the <c>AiConsole:AdminEmails</c> allow-list. There is no admin role
    /// (every SSO user is an Instructor), so this is the single rule for the AI Console and quota exemption.
    /// </summary>
    public interface IAdminAccessService
    {
        /// <summary>The user's email when it is on the admin allow-list, else null.</summary>
        Task<string?> GetAdminEmailAsync(string? userId, CancellationToken ct = default);
    }

    /// <inheritdoc cref="IAdminAccessService"/>
    public class AdminAccessService(MyDatabaseContext db, IOptions<AiConsoleOptions> options) : IAdminAccessService
    {
        public async Task<string?> GetAdminEmailAsync(string? userId, CancellationToken ct = default)
        {
            var admins = options.Value.AdminEmailSet();
            if (admins.Count == 0 || string.IsNullOrEmpty(userId)) return null;

            var email = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync(ct);
            return email != null && admins.Contains(email) ? email : null;
        }
    }
}
