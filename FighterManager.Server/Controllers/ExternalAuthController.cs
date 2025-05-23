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

        // [HttpGet("signin-google-callback")]
        // public async Task<IActionResult> SignInGoogleCallback(string returnUrl = "/")
        // {
        //     // Ensure response hasn’t started
        //     if (HttpContext.Response.HasStarted)
        //     {
        //         _logger.LogWarning("Response has already started in SignInGoogleCallback");
        //         return StatusCode(500, "Internal server error: Response already started");
        //     }

        //     // Get external login info
        //     var info = await _signInManager.GetExternalLoginInfoAsync();
        //     if (info == null)
        //     {
        //         _logger.LogError("External login info is null");
        //         return Redirect($"{returnUrl}?error={Uri.EscapeDataString("ExternalLoginFailed")}");
        //     }

        //     try
        //     {
        //         // Check if user exists or needs to be created
        //         var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        //         AppUserEntity user = null;
        //         if (email != null)
        //         {
        //             user = await _userManager.FindByEmailAsync(email);
        //             if (user == null)
        //             {
        //                 user = new AppUserEntity
        //                 {
        //                     UserName = email,
        //                     Email = email,
        //                     EmailConfirmed = true, // Trust Google-authenticated users
        //                     Fighter = new()
        //                     {
        //                         FighterName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "SSO User",
        //                         Height = 0.0,
        //                         Weight = 0.0,
        //                         BMI = 0.0,
        //                         Gender = Enum.TryParse<Gender>(info.Principal.FindFirstValue(ClaimTypes.Gender), true, out var gender) ? gender : Gender.Male,
        //                         Role = FighterRole.Instructor,
        //                         Birthdate = DateTime.TryParse(info.Principal.FindFirstValue(ClaimTypes.DateOfBirth), out var birthdate) ? birthdate : DateTime.MinValue,
        //                         MaxWorkoutDuration = 30,
        //                         BelkRank = BeltColor.Black
        //                     }
        //                 };
        //                 var createResult = await _userManager.CreateAsync(user);
        //                 if (!createResult.Succeeded)
        //                 {
        //                     _logger.LogError("Failed to create user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
        //                     return Redirect($"{returnUrl}?error={Uri.EscapeDataString("UserCreationFailed")}");
        //                 }
        //                 // Link Google login
        //                 var loginInfo = new ExternalLoginInfo(info.Principal, "Google", info.Principal.FindFirstValue(ClaimTypes.NameIdentifier), null);
        //                 await _userManager.AddLoginAsync(user, loginInfo);
        //             }
        //         }

        //         if (user == null)
        //         {
        //             _logger.LogError("Failed to find or create user for Google SSO");
        //             return Redirect($"{returnUrl}?error={Uri.EscapeDataString("UserCreationFailed")}");
        //         }

        //         // Sign in user to establish Identity cookie
        //         await _signInManager.SignInAsync(user, isPersistent: true);

        //         // Generate tokens
        //         var accessToken = await _fighterSignInService.GenerateJwtTokenAsync(user);
        //         var refreshToken = _fighterSignInService.GenerateRefreshToken(user);

        //         // Complete authentication to prevent middleware from writing response
        //         var properties = new AuthenticationProperties
        //         {
        //             RedirectUri = $"{returnUrl}{(returnUrl.Contains("?") ? "&" : "?")}token={Uri.EscapeDataString(accessToken)}&refreshToken={Uri.EscapeDataString(refreshToken)}"
        //         };
        //         await HttpContext.ChallengeAsync("Google", properties);

        //         _logger.LogInformation("Google SSO callback processed successfully. Redirecting to {RedirectUrl}", properties.RedirectUri);
        //         return new EmptyResult(); // Prevent further controller response
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error processing Google SSO callback");
        //         return Redirect($"{returnUrl}?error={Uri.EscapeDataString("AuthenticationFailed")}");
        //     }
        // }
    }
}