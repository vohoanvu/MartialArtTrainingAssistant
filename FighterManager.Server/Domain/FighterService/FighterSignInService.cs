using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedEntities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FighterManager.Server.Domain.FighterService
{
    public class FighterSignInService<TUser> : SignInManager<TUser> where TUser : class
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SignInManager<TUser>> _logger;
        private readonly UserManager<TUser> _userManager;
        private readonly IUserClaimsPrincipalFactory<TUser> _claimsPrincipalFactory;
        private readonly IHttpContextAccessor _contextAccessor; 

        public FighterSignInService(UserManager<TUser> userManager, IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<TUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, 
            ILogger<SignInManager<TUser>> logger, IAuthenticationSchemeProvider schemes, 
            IUserConfirmation<TUser> confirmation, IConfiguration configuration)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _claimsPrincipalFactory = claimsFactory;
            _contextAccessor = contextAccessor;
        }

        public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            var result = await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);
            if (!result.Succeeded)
            {
                return result;
            }

            return SignInResult.Success;
        }

        public async Task<string> GenerateJwtTokenAsync(TUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.GetType().GetProperty("Id")?.GetValue(user, null)?.ToString() ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, await _userManager.GetEmailAsync(user))
            };


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtKey)));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtIssuer),
                audience: Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtAudience),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(3),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken(TUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.GetType().GetProperty("Id")?.GetValue(user, null)?.ToString() ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, _userManager.GetEmailAsync(user).Result)  // Alternatively use await in async context
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtKey)));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var refreshToken = new JwtSecurityToken(
                issuer: Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtIssuer),
                audience: Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.JwtAudience),
                claims: claims,
                expires: DateTime.Now.AddDays(7),  // Refresh tokens usually have a longer expiry time
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(refreshToken);
        }
    }
}
