using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using SharedEntities.Models;

namespace FighterManager.Server.Helpers
{
    public class EnhancedInfoResponse
    {
        public InfoResponseModel UserInfo { get; set; }
        public object FighterInfo { get; set; }
    }

    public class InfoResponseModel
    {
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }

        public InfoResponseModel(string email, bool emailConfirmed)
        {
            Email = email;
            EmailConfirmed = emailConfirmed;
        }
    }

    public interface IIdentityResponseEnhancer
    {
        Task<object> EnhanceManageInfoResponseAsync(HttpContext context, object originalResult);
    }

    public class IdentityResponseEnhancer : IIdentityResponseEnhancer
    {
        private readonly UserManager<AppUserEntity> _userManager;
        private readonly IUnitOfWork _unitOfWork; // Assume this is your data access layer

        public IdentityResponseEnhancer(UserManager<AppUserEntity> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<object> EnhanceManageInfoResponseAsync(HttpContext context, object originalResult)
        {
            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return Results.NotFound();
            }

            var defaultInfo = new InfoResponseModel(user.Email!, user.EmailConfirmed);

            // Example: Fetch additional data (e.g., fighter info)
            var fighter = await _unitOfWork.Repository<Fighter>().GetByIdAsync(user.FighterId);
            var fighterInfo = new
            {
                fighter.Id,
                fighter.FighterName,
                BeltRank = fighter.BelkRank.ToString(),
                fighter.Role,
                fighter.Birthdate,
                fighter.Height,
                fighter.Weight
            };

            return new EnhancedInfoResponse
            {
                UserInfo = defaultInfo,
                FighterInfo = fighterInfo
            };
        }
    }

    public class ResponseEnhancementMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseEnhancementMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            if (context.Request.Path.Equals("/api/auth/v1/manage/info", StringComparison.OrdinalIgnoreCase))
            {
                // Capture the original response stream
                var originalBodyStream = context.Response.Body;
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // Call the next middleware (executes the endpoint)
                await _next(context);

                // Read the original response
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();
                var originalResult = JsonSerializer.Deserialize<object>(responseText);

                // Create a scope to resolve scoped services
                using var scope = serviceProvider.CreateScope();
                var enhancer = scope.ServiceProvider.GetRequiredService<IIdentityResponseEnhancer>();

                // Enhance the response
                var enhancedResult = await enhancer.EnhanceManageInfoResponseAsync(context, originalResult);

                // Write the enhanced response back
                context.Response.Body = originalBodyStream;
                await context.Response.WriteAsJsonAsync(enhancedResult);
            }
            else
            {
                // Pass through other requests unchanged
                await _next(context);
            }
        }
    }
}