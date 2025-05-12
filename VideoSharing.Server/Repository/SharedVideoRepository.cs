using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Domain.YoutubeSharingService;

namespace VideoSharing.Server.Repository
{
    public interface ISharedVideoRepository
    {
        Task<VideoMetadata?> GetByIdAsync(int id);
        Task<List<VideoMetadata>> GetAllAsync();
        Task<int> SaveAsync(VideoMetadata video);
    }

    public class SharedVideoRepository : ISharedVideoRepository
    {
        private readonly MyDatabaseContext _context;
        private readonly IHubContext<VideoShareHub> _hubContext;

        public SharedVideoRepository(MyDatabaseContext context, IHubContext<VideoShareHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<VideoMetadata?> GetByIdAsync(int id)
        {
            return await _context.Videos.FindAsync(id);
        }

        public async Task<List<VideoMetadata>> GetAllAsync()
        {
            var videoList = _context.Videos.Include(v => v.AppUser).Where(v => v.Type == VideoType.Shared)
                .OrderByDescending(v => v.UploadedAt).ToList();
            return await Task.FromResult(videoList);
        }
        public async Task<int> SaveAsync(VideoMetadata video)
        {
            // Check if a video with the same URL already exists
            var existingVideo = await _context.Videos
                .FirstOrDefaultAsync(v => v.YoutubeVideoId == video.YoutubeVideoId);
            var appUser = await _context.Users.FindAsync(video.UserId);
            var userName = appUser!.UserName;

            if (existingVideo == null)
            {
                // If the video doesn't exist, add it as a new video
                _context.Videos.Add(video);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("ReceiveVideoSharedNotification", "New Video Shared!", video.Title, userName);

                return video.Id;
            }
            else
            {
                // If the video exists, update the existing video's properties
                existingVideo.Title = video.Title;
                existingVideo.Description = video.Description;
                existingVideo.UploadedAt = video.UploadedAt;
                existingVideo.UserId = video.UserId;
                // Note: No need to call Update() as EF tracks changes to existing entities

                await _hubContext.Clients.All.SendAsync("ReceiveVideoSharedNotification", "This video has already been shared!", video.Title, userName);

                await _context.SaveChangesAsync();
                return existingVideo.Id;
            }
        }

    }
}
