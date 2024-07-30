using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SampleAspNetReactDockerApp.Server.Data;
using SampleAspNetReactDockerApp.Server.Domain.YoutubeSharingService;
using SampleAspNetReactDockerApp.Server.Models;

namespace SampleAspNetReactDockerApp.Server.Repository
{
    public interface ISharedVideoRepository
    {
        Task<SharedVideo> GetByIdAsync(int id);
        Task<List<SharedVideo>> GetAllAsync();
        Task<int> SaveAsync(SharedVideo video);
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

        public async Task<SharedVideo?> GetByIdAsync(int id)
        {
            return await _context.SharedVideos.FindAsync(id);
        }

        public async Task<List<SharedVideo>> GetAllAsync()
        {
            var videoList = _context.SharedVideos.Include(v => v.SharedBy).OrderByDescending(v => v.DateShared).ToList();
            return await Task.FromResult(videoList);
        }

        public async Task<int> SaveAsync(SharedVideo video)
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
