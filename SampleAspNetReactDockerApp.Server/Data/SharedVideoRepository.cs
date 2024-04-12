using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SampleAspNetReactDockerApp.Server.Helpers;
using SampleAspNetReactDockerApp.Server.Models;

namespace SampleAspNetReactDockerApp.Server.Data
{
    public interface ISharedVideoRepository
    {
        Task<SharedVideo> GetByIdAsync(int id);
        Task<List<SharedVideo>> GetAllAsync();
        Task<int> SaveAsync(SharedVideo video);
        Task DeleteAsync(int id);
    }

    public class SharedVideoRepository : ISharedVideoRepository
    {
        private readonly DatabaseContext _context;
        private readonly IHubContext<VideoShareHub> _hubContext;

        public SharedVideoRepository(DatabaseContext context, IHubContext<VideoShareHub> hubContext)
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
            return await _context.SharedVideos.Include(v => v.SharedBy).ToListAsync();
        }

        public async Task<int> SaveAsync(SharedVideo video)
        {
            // Check if a video with the same URL already exists
            var existingVideo = await _context.SharedVideos
                .FirstOrDefaultAsync(v => v.VideoId == video.VideoId);
            var appUser = await _context.Users.FindAsync(video.UserId);

            if (existingVideo == null)
            {
                // If the video doesn't exist, add it as a new video
                _context.SharedVideos.Add(video);
                await _context.SaveChangesAsync();

                var userName = appUser!.UserName;
                await _hubContext.Clients.All.SendAsync("ReceiveVideoSharedNotification", video.Title, userName);

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

                await _context.SaveChangesAsync();
                return existingVideo.Id;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var video = await _context.SharedVideos.FindAsync(id);
            if (video != null)
            {
                _context.SharedVideos.Remove(video);
                await _context.SaveChangesAsync();
            }
        }
    }
}
