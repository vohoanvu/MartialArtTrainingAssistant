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

        public ExternalAuthController(SignInManager<AppUserEntity> signInManager,
            UserManager<AppUserEntity> userManager,
            FighterSignInService<AppUserEntity> fighterSignInService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _fighterSignInService = fighterSignInService;
        }

        [HttpGet("signin-google")]
        public IActionResult SignInWithGoogle(string returnUrl = "/")
        {
            var redirectUrl = Url.Action(
                nameof(ExternalLoginCallback),
                "ExternalAuth", new { returnUrl },
                "https"
            );
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        [HttpGet("signin-facebook")]
        public IActionResult SignInWithFacebook(string returnUrl = "/")
        {
            var redirectUrl = Url.Action(
                nameof(ExternalLoginCallback),
                "ExternalAuth", new { returnUrl },
                "https"
            );
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Facebook", redirectUrl);
            return Challenge(properties, "Facebook");
        }

        [HttpGet("externallogincallback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                var htmlPage = "<html><head><script>window.location='/login?error=External login failed'</script></head><body></body></html>";
                return Content(htmlPage, "text/html");
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            AppUserEntity user;
            if (!signInResult.Succeeded)
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                user = new AppUserEntity { UserName = email, Email = email };
                await _userManager.CreateAsync(user);
                await _userManager.AddLoginAsync(user, info);
            }
            else
            {
                user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
                    ?? throw new Exception("External login failed: user not found.");
            }

            var jwt = await _fighterSignInService.GenerateJwtTokenAsync(user);
            var redirectUrl = $"{returnUrl}?token={jwt}";
            var html = $"<html><head><script>window.location.replace('{redirectUrl}')</script></head><body></body></html>";
            return Content(html, "text/html");
        }
    }
}