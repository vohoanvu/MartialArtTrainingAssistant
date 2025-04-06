using Microsoft.AspNetCore.SignalR;

namespace VideoSharing.Server.Domain.YoutubeSharingService
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class VideoShareHub : Hub
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public VideoShareHub()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
        }

        //Unused method! the trigger of notification is done at the SharedVideoRepository level
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task SendVideoSharedNotification(string videoTitle, string userName)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            await Clients.Others.SendAsync("ReceiveVideoSharedNotification", videoTitle, userName);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override async Task OnConnectedAsync()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            await base.OnConnectedAsync();
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override async Task OnDisconnectedAsync(Exception exception)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            await base.OnDisconnectedAsync(exception);
        }

    }
}
