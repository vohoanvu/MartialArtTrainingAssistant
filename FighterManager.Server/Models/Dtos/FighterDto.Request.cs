using System.ComponentModel.DataAnnotations;

namespace FighterManager.Server.Models.Dtos
{
    public class CreateFighterDto : FighterDtoBase
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class UpdateFighterDto : FighterDtoBase
    {
    }

    public class CustomLoginRequest
    {
        [EmailAddress]
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? TwoFactorCode { get; set; }
        public string? TwoFactorRecoveryCode { get; set; }
    }

}
