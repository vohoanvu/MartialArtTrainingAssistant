using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Domain.YoutubeSharingService;

namespace VideoSharing.Server.Repository
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ISharedVideoRepository
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Task<SharedVideo> GetByIdAsync(int id);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Task<List<SharedVideo>> GetAllAsync();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Task<int> SaveAsync(SharedVideo video);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class SharedVideoRepository : ISharedVideoRepository
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private readonly MyDatabaseContext _context;
        private readonly IHubContext<VideoShareHub> _hubContext;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public SharedVideoRepository(MyDatabaseContext context, IHubContext<VideoShareHub> hubContext)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            _context = context;
            _hubContext = hubContext;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<SharedVideo?> GetByIdAsync(int id)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            return await _context.SharedVideos.FindAsync(id);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<List<SharedVideo>> GetAllAsync()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var videoList = _context.SharedVideos.Include(v => v.SharedBy).OrderByDescending(v => v.DateShared).ToList();
            return await Task.FromResult(videoList);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<int> SaveAsync(SharedVideo video)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            // Check if a video with the same URL already exists
            var existingVideo = await _context.SharedVideos
                .FirstOrDefaultAsync(v => v.VideoId == video.VideoId);
            var appUser = await _context.Users.FindAsync(video.UserId);
            var userName = appUser!.UserName;

            if (existingVideo == null)
            {
                // If the video doesn't exist, add it as a new video
                _context.SharedVideos.Add(video);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("ReceiveVideoSharedNotification", "New Video Shared!", video.Title, userName);

                return video.Id;
            }
            else
            {
                // If the video exists, update the existing video's properties
                existingVideo.Title = video.Title;
                existingVideo.Description = video.Description;
                existingVideo.DateShared = video.DateShared;
                existingVideo.UserId = video.UserId;
                // Note: No need to call Update() as EF tracks changes to existing entities

                await _hubContext.Clients.All.SendAsync("ReceiveVideoSharedNotification", "This video has already been shared!", video.Title, userName);

                await _context.SaveChangesAsync();
                return existingVideo.Id;
            }
        }

    }
}
