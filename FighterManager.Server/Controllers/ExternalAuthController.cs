using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FighterManager.Server.Domain.FighterService;
using SharedEntities.Models;
using System.Security.Claims;

namespace FighterManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalAuthController : ControllerBase
    {
        private readonly SignInManager<AppUserEntity> _signInManager;
        private readonly UserManager<AppUserEntity> _userManager;
        private readonly FighterSignInService<AppUserEntity> _fighterSignInService;
        private readonly ILogger<ExternalAuthController> _logger;

        public ExternalAuthController(
            SignInManager<AppUserEntity> signInManager,
            UserManager<AppUserEntity> userManager,
            FighterSignInService<AppUserEntity> fighterSignInService,
            ILogger<ExternalAuthController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _fighterSignInService = fighterSignInService;
            _logger = logger;
        }

        [HttpGet("signin-google")]
        public IActionResult SignInWithGoogle(string returnUrl = "/")
        {
            var redirectUrl = "/signin-google-callback";
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            properties.Items["returnUrl"] = returnUrl;
            return Challenge(properties, "Google");
        }

        [NonAction]
        public static async Task HandleGoogleTicketReceived(TicketReceivedContext context)
        {
            var signInService = context.HttpContext.RequestServices.GetRequiredService<FighterSignInService<AppUserEntity>>();
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUserEntity>>();
            var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<AppUserEntity>>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ExternalAuthController>>();

            try
            {
                var info = context.Principal;
                var email = info!.FindFirst(ClaimTypes.Email)?.Value;
                AppUserEntity? user = null;
                if (email != null)
                {
                    user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new AppUserEntity
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true,
                            Fighter = new()
                            {
                                FighterName = info.FindFirst(ClaimTypes.Name)?.Value ?? "SSO User",
                                Height = 0.0,
                                Weight = 0.0,
                                BMI = 0.0,
                                Gender = Enum.TryParse<Gender>(info.FindFirst(ClaimTypes.Gender)?.Value, true, out var gender) ? gender : Gender.Male,
                                Role = FighterRole.Instructor,
                                Birthdate = DateTime.TryParse(info.FindFirst(ClaimTypes.DateOfBirth)?.Value, out var birthdate) ? birthdate : DateTime.MinValue,
                                MaxWorkoutDuration = 30,
                                BelkRank = BeltColor.Black,
                                Experience = TrainingExperience.MoreThanFiveYears
                            }
                        };
                        var createResult = await userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            logger.LogError("Failed to create user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                            context.HttpContext.Response.Redirect($"/sso-callback?error={Uri.EscapeDataString("UserCreationFailed")}");
                            context.HandleResponse();
                            return;
                        }
                        var loginInfo = new ExternalLoginInfo(info, "Google", info.FindFirstValue(ClaimTypes.NameIdentifier)!, null!);
                        await userManager.AddLoginAsync(user, loginInfo);
                    }
                }
                if (user == null)
                {
                    logger.LogError("Failed to find or create user for Google SSO");
                    context.HttpContext.Response.Redirect($"/sso-callback?error={Uri.EscapeDataString("UserCreationFailed")}");
                    context.HandleResponse();
                    return;
                }
                await signInManager.SignInAsync(user, isPersistent: true);
                var accessToken = await signInService.GenerateJwtTokenAsync(user);
                var refreshToken = signInService.GenerateRefreshToken(user);
                var returnUrl = context.Properties!.GetString("returnUrl") ?? "/sso-callback";
                var redirectUrl = $"{returnUrl}{(returnUrl.Contains("?") ? "&" : "?")}token={Uri.EscapeDataString(accessToken)}&refreshToken={Uri.EscapeDataString(refreshToken)}";
                logger.LogInformation("Google SSO callback processed successfully. Redirecting to {RedirectUrl}", redirectUrl);
                context.HttpContext.Response.Redirect(redirectUrl);
                context.HandleResponse();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Google SSO callback");
                context.HttpContext.Response.Redirect($"/sso-callback?error={Uri.EscapeDataString("AuthenticationFailed")}");
                context.HandleResponse();
            }
        }
    }
}