namespace VideoSharing.Server.Models.Dtos
{
    public class CreateFighterDto : FighterDtoBase
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class UpdateFighterDto : FighterDtoBase
    {
    }
}
