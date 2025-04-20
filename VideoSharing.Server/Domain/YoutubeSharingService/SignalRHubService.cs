using Microsoft.AspNetCore.SignalR;

namespace VideoSharing.Server.Domain.YoutubeSharingService
{
    public class VideoShareHub : Hub
    {
        public VideoShareHub()
        {
        }

        //Unused method! the trigger of notification is done at the SharedVideoRepository level
        public async Task SendVideoSharedNotification(string videoTitle, string userName)
        {
            await Clients.Others.SendAsync("ReceiveVideoSharedNotification", videoTitle, userName);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

    }
}
