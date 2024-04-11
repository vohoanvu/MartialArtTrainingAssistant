using Microsoft.AspNetCore.SignalR;

namespace SampleAspNetReactDockerApp.Server.Helpers
{
    public class VideoShareHub : Hub
    {
        public async Task SendVideoSharedNotification(string videoTitle, string userName)
        {
            await Clients.Others.SendAsync("ReceiveVideoSharedNotification", videoTitle, userName);
        }
    }
}
