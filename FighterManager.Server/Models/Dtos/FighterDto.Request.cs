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

    public class Waitlist
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Region { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
