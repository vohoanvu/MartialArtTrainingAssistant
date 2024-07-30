namespace SampleAspNetReactDockerApp.Server.Models.Dtos
{
    public class ViewFighterDto : FighterDtoBase
    {
        public int Id { get; set; }
    }

    public class CustomRegistrationResponse
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public int Status { get; set; }
        public string Detail { get; set; }
        public string Instance { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; }
        public object? CreatedObject { get; set; }
    }

}
