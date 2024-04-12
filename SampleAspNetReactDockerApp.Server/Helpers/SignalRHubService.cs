using Microsoft.AspNetCore.SignalR;

namespace SampleAspNetReactDockerApp.Server.Helpers
{
    public class VideoShareHub : Hub
    {
        public VideoShareHub()
        {
        }

        public async Task SendVideoSharedNotification(string videoTitle, string userName)
        {
            await Clients.Others.SendAsync("ReceiveVideoSharedNotification", videoTitle, userName);
        }

        public override async Task OnConnectedAsync()
        {
            // Set a breakpoint here
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Set a breakpoint here
            await base.OnDisconnectedAsync(exception);
        }

    }
}
