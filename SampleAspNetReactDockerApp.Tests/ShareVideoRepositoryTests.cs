using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using SampleAspNetReactDockerApp.Server.Data;
using SampleAspNetReactDockerApp.Server.Helpers;
using SampleAspNetReactDockerApp.Server.Models;
using System.Globalization;
using System.Linq.Expressions;
using System.Net.Sockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SampleAspNetReactDockerApp.Tests
{
    public class SharedVideoRepositoryTests
    {
        private readonly Mock<DbSet<SharedVideo>> _mockDbSet;
        private readonly Mock<DatabaseContext> _mockContext;
        private readonly Mock<IHubContext<VideoShareHub>> _mockHubContext;
        private readonly ISharedVideoRepository _repository;


        public SharedVideoRepositoryTests()
        {
            _mockDbSet = new Mock<DbSet<SharedVideo>>();
            _mockContext = new Mock<DatabaseContext>();
            _mockHubContext = new Mock<IHubContext<VideoShareHub>>();

            _repository = new SharedVideoRepository(_mockContext.Object, _mockHubContext.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsVideo_WhenIdIsValid()
        {
            // Arrange
            var video = new SharedVideo()
            {
                Id = 1,
                Title = "Joe Rogan Experience #2112 - Dan Soder",
                VideoId = "7be7OxPr1Lo",
                Description = "Dan Soder is a stand-up comic, actor, on-air personality, and host of the \"Soder\" podcast. Check out his new special \"Dan Soder: On The Road\" available now on YouTube. https://youtu.be/1Lik3hSyhrY?si=NvFRtRwbFAbJBivL\r\n\r\nwww.dansoder.com",
                DateShared = DateTime.ParseExact("2024-04-12 18:08:49.235 +0700", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
                Url = "https://www.youtube.com/watch?v=7be7OxPr1Lo",
                UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82"
            };
            _mockContext.Setup(m => m.SharedVideos.FindAsync(1)).ReturnsAsync(video);

            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Joe Rogan Experience #2112 - Dan Soder", result.Title);
        }

        [Fact]
        public void GetAllAsync_ReturnsAllVideos()
        {
            // Arrange
            var data = new List<SharedVideo>
            {
                new()
                {
                    Id = 1,
                    Title = "Joe Rogan Experience #2112 - Dan Soder",
                    VideoId = "7be7OxPr1Lo",
                    Description = "Dan Soder is a stand-up comic, actor, on-air personality, and host of the \"Soder\" podcast. Check out his new special \"Dan Soder: On The Road\" available now on YouTube. https://youtu.be/1Lik3hSyhrY?si=NvFRtRwbFAbJBivL\r\n\r\nwww.dansoder.com",
                    DateShared = DateTime.ParseExact("2024-04-12 18:08:49.235 +0700", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
                    Url = "https://www.youtube.com/watch?v=7be7OxPr1Lo",
                    UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82"
                },
                new()
                {
                    Id = 2,
                    Title = "Test Title 2",
                    VideoId = "testvideoid2",
                    Description = "Test Description 2",
                    DateShared = DateTime.ParseExact("2024-04-10 18:08:49.235 +0700", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
                    Url = "https://www.youtube.com/watch?v=testvideoid2",
                    UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82"
                },
                new()
                {
                    Id = 3,
                    Title = "Test Title 3",
                    VideoId = "testvideoid3",
                    Description = "Test Description 3",
                    DateShared = DateTime.ParseExact("2024-04-11 18:08:49.235 +0700", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
                    Url = "https://www.youtube.com/watch?v=testvideoid3",
                    UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82"
                }
            }.AsQueryable();

            _mockDbSet.As<IQueryable<SharedVideo>>().Setup(m => m.Provider).Returns(data.Provider);
            _mockDbSet.As<IQueryable<SharedVideo>>().Setup(m => m.Expression).Returns(data.Expression);
            _mockDbSet.As<IQueryable<SharedVideo>>().Setup(m => m.ElementType).Returns(data.ElementType);
            _mockDbSet.As<IQueryable<SharedVideo>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            _mockContext.Setup(m => m.SharedVideos).Returns(_mockDbSet.Object);

            // Act
            var result = _repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(data.Count(), result.Count);
        }
    }

}

//var data = new List<SharedVideo>
//{
//    new()
//    {
//        Id = 1,
//        Title = "Joe Rogan Experience #2112 - Dan Soder",
//        VideoId = "7be7OxPr1Lo",
//        Description = "Dan Soder is a stand-up comic, actor, on-air personality, and host of the \"Soder\" podcast. Check out his new special \"Dan Soder: On The Road\" available now on YouTube. https://youtu.be/1Lik3hSyhrY?si=NvFRtRwbFAbJBivL\r\n\r\nwww.dansoder.com",
//        DateShared = DateTime.ParseExact("2024-04-12 18:08:49.235 +0700", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
//        Url = "https://www.youtube.com/watch?v=7be7OxPr1Lo",
//        UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82"
//    },
//    new()
//    {
//        Id = 2,
//        Title = "Test Title 2",
//        VideoId = "testvideoid2",
//        Description = "Test Description 2",
//        DateShared = DateTime.ParseExact("2024-04-10 18:08:49.235 +0700", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
//        Url = "https://www.youtube.com/watch?v=testvideoid2",
//        UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82"
//    },
//    new()
//    {
//        Id = 3,
//        Title = "Test Title 3",
//        VideoId = "testvideoid3",
//        Description = "Test Description 3",
//        DateShared = DateTime.ParseExact("2024-04-11 18:08:49.235 +0700", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
//        Url = "https://www.youtube.com/watch?v=testvideoid3",
//        UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82"
//    }
//}.AsQueryable();

//_mockDbSet.As<IQueryable<SharedVideo>>().Setup(m => m.Provider).Returns(data.Provider);
//_mockDbSet.As<IQueryable<SharedVideo>>().Setup(m => m.Expression).Returns(data.Expression);
//_mockDbSet.As<IQueryable<SharedVideo>>().Setup(m => m.ElementType).Returns(data.ElementType);
//_mockDbSet.As<IQueryable<SharedVideo>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

// This is the important part for async operations
//_mockDbSet.As<IAsyncEnumerable<SharedVideo>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
//    .Returns(new TestAsyncEnumerator<SharedVideo>(data.GetEnumerator()));

//_mockContext.Setup(m => m.SharedVideos).Returns(_mockDbSet.Object);
// Mock the Include methods
//_mockContext.Setup(m => m.SharedVideos.Include(It.IsAny<Expression<Func<SharedVideo, object>>>()))
//    .Returns((Expression<Func<SharedVideo, object>> include) => _mockDbSet.Object.Include(include));