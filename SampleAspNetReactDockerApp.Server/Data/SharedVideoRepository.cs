using Microsoft.EntityFrameworkCore;
using SampleAspNetReactDockerApp.Server.Models;

namespace SampleAspNetReactDockerApp.Server.Data
{
    public interface ISharedVideoRepository
    {
        Task<SharedVideo> GetByIdAsync(int id);
        Task<List<SharedVideo>> GetAllAsync();
        Task SaveAsync(SharedVideo video);
        Task DeleteAsync(int id);
    }

    public class SharedVideoRepository : ISharedVideoRepository
    {
        private readonly DatabaseContext _context;

        public SharedVideoRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<SharedVideo?> GetByIdAsync(int id)
        {
            return await _context.SharedVideos.FindAsync(id);
        }

        public async Task<List<SharedVideo>> GetAllAsync()
        {
            return await _context.SharedVideos.Include(v => v.SharedBy).ToListAsync();
        }

        public async Task SaveAsync(SharedVideo video)
        {
            // Check if a video with the same URL already exists
            var existingVideo = await _context.SharedVideos
                .FirstOrDefaultAsync(v => v.VideoId == video.VideoId);

            if (existingVideo == null)
            {
                // If the video doesn't exist, add it as a new video
                _context.SharedVideos.Add(video);
            }
            else
            {
                // If the video exists, update the existing video's properties
                existingVideo.Title = video.Title;
                existingVideo.Description = video.Description;
                existingVideo.DateShared = video.DateShared;
                existingVideo.UserId = video.UserId;
                // Note: No need to call Update() as EF tracks changes to existing entities
            }

            // Save changes to the database
            await _context.SaveChangesAsync();
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
