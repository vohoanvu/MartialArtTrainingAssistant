using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using System.Globalization;
using SharedEntities.Data;
using VideoSharing.Server.Domain.YoutubeSharingService;
using SharedEntities.Models;
using VideoSharing.Server.Repository;


namespace SampleAspNetReactDockerApp.Tests
{
    public class SharedVideoRepositoryTests
    {
        private readonly Mock<DbSet<SharedVideo>> _mockDbSet;
        private readonly Mock<MyDatabaseContext> _mockContext;
        private readonly Mock<IHubContext<VideoShareHub>> _mockHubContext;
        private readonly ISharedVideoRepository _repository;

        public SharedVideoRepositoryTests()
        {
            _mockDbSet = new Mock<DbSet<SharedVideo>>();
            _mockContext = new Mock<MyDatabaseContext>();
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
        public async Task GetAllAsync_ReturnsAllVideos()
        {
            // Arrange
            var mockedSharedByUser = new AppUserEntity()
            {
                Id = "c5027cdc-de1f-4480-ade6-dbe5182f3a82",
                UserName = "testuser"
            };
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
                    UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82",
                    SharedBy = mockedSharedByUser
                },
                new()
                {
                    Id = 2,
                    Title = "Test Title 2",
                    VideoId = "testvideoid2",
                    Description = "Test Description 2",
                    DateShared = DateTime.ParseExact("2024-04-10 18:08:49.235 +0700", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
                    Url = "https://www.youtube.com/watch?v=testvideoid2",
                    UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82",
                    SharedBy = mockedSharedByUser
                },
                new()
                {
                    Id = 3,
                    Title = "Test Title 3",
                    VideoId = "testvideoid3",
                    Description = "Test Description 3",
                    DateShared = DateTime.ParseExact("2024-04-11 18:08:49.235 +0700", "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
                    Url = "https://www.youtube.com/watch?v=testvideoid3",
                    UserId = "c5027cdc-de1f-4480-ade6-dbe5182f3a82",
                    SharedBy = mockedSharedByUser
                }
            }.AsQueryable();

            _mockContext.Setup(m => m.SharedVideos).ReturnsDbSet(data);

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(data.Count(), result.Count);
            Assert.Equal(mockedSharedByUser.UserName, result[0].SharedBy.UserName);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyList_WhenNoVideosExist()
        {
            // Arrange
            var data = new List<SharedVideo>().AsQueryable();

            _mockContext.Setup(m => m.SharedVideos).ReturnsDbSet(data);

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SaveAsync_CreateNewVideo_NewVideoAddedSuccesfully()
        {
            // Arrange
            var newVideo = new SharedVideo 
            {
                Id = 2,
                Title = "new Title",
                VideoId = "newvideoid",
                Description = "New Description",
                DateShared = DateTime.Now,
                Url = "https://www.youtube.com/watch?v=existingvideoid",
                UserId = "newuserid"
            };

            var data = new List<SharedVideo>
            {
                new SharedVideo
                {
                    Id = 1, // different id than the new video
                    Title = "Existing Title",
                    VideoId = "existingvideoid",
                    Description = "Existing Description",
                    DateShared = DateTime.Now,
                    Url = "https://www.youtube.com/watch?v=existingvideoid",
                    UserId = "existinguserid"
                }
            };
            _mockContext.Setup(x => x.SharedVideos).ReturnsDbSet(data);
            _mockContext.Setup(m => m.SharedVideos.Add(It.IsAny<SharedVideo>())).Callback<SharedVideo>(v => data.Add(v));
            var mockedAppUser = new AppUserEntity
            {
                Id = "newuserid",
                UserName = "newuser"
            };
            _mockContext.Setup(m => m.Users.FindAsync("newuserid")).ReturnsAsync(mockedAppUser);
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(m => m.All).Returns(mockClientProxy.Object);
            _mockHubContext.Setup(m => m.Clients).Returns(mockClients.Object);

            // Act
            var result = await _repository.SaveAsync(newVideo);

            // Assert
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            //checking that the DbSet.Add() method was called with new video
            Assert.Contains(newVideo, data);
            Assert.Equal(newVideo.Title, data.Last().Title);
            Assert.Equal(newVideo.VideoId, data.Last().VideoId);
        }

        [Fact]
        public async Task SaveAsync_UpdatesExistingVideo_UpdatedVideoId_Equals_ExistingVideoId()
        {
            // Arrange
            var existingVideo = new SharedVideo
            {
                Id = 1,
                Title = "Existing Title",
                VideoId = "existingvideoid",
                Description = "Existing Description",
                DateShared = DateTime.Now,
                Url = "https://www.youtube.com/watch?v=existingvideoid",
                UserId = "existinguserid"
            };

            var updatedVideo = new SharedVideo
            {
                Id = 1,
                Title = "Updated Title",
                VideoId = "existingvideoid",
                Description = "Updated Description",
                DateShared = DateTime.Now,
                Url = "https://www.youtube.com/watch?v=existingvideoid",
                UserId = "existinguserid"
            };

            var data = new List<SharedVideo>
            {
                existingVideo
            };
            _mockContext.Setup(x => x.SharedVideos).ReturnsDbSet(data);
            _mockContext.Setup(m => m.Users.FindAsync("existinguserid")).ReturnsAsync(new AppUserEntity
            {
                Id = "existinguserid",
                UserName = "existinguser"
            });

            // Mock the IHubContext
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(m => m.All).Returns(mockClientProxy.Object);
            _mockHubContext.Setup(m => m.Clients).Returns(mockClients.Object);

            // Act
            var result = await _repository.SaveAsync(updatedVideo);

            // Assert
            Assert.Equal(existingVideo.Id, result);
            Assert.Equal(updatedVideo.Title, existingVideo.Title);
            Assert.Equal(updatedVideo.Description, existingVideo.Description);
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockClientProxy.Verify(clientProxy => clientProxy.SendCoreAsync(
                "ReceiveVideoSharedNotification", 
                It.Is<object[]>(o => o[0] as string == "This video has already been shared!" && o[1] as string == updatedVideo.Title && o[2] as string == "existinguser"), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_CreateNewVideo_SignalrNotificationWasTriggered()
        {
            // Arrange
            var newVideo = new SharedVideo 
            {
                Id = 2,
                Title = "new Title",
                VideoId = "newvideoid",
                Description = "New Description",
                DateShared = DateTime.Now,
                Url = "https://www.youtube.com/watch?v=existingvideoid",
                UserId = "newuserid"
            };

            var data = new List<SharedVideo>
            {
                new SharedVideo
                {
                    Id = 1, // different id than the new video
                    Title = "Existing Title",
                    VideoId = "existingvideoid",
                    Description = "Existing Description",
                    DateShared = DateTime.Now,
                    Url = "https://www.youtube.com/watch?v=existingvideoid",
                    UserId = "existinguserid"
                }
            };
            _mockContext.Setup(x => x.SharedVideos).ReturnsDbSet(data);
            var mockedAppUser = new AppUserEntity
            {
                Id = "newuserid",
                UserName = "newuser"
            };
            _mockContext.Setup(m => m.Users.FindAsync("newuserid")).ReturnsAsync(mockedAppUser);
            // Mock the IHubContext
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(m => m.All).Returns(mockClientProxy.Object);
            _mockHubContext.Setup(m => m.Clients).Returns(mockClients.Object);

            // Act
            var result = await _repository.SaveAsync(newVideo);

            // Assert
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockClientProxy.Verify(clientProxy => clientProxy.SendCoreAsync(
                "ReceiveVideoSharedNotification", 
                It.Is<object[]>(o => o[0] as string == "New Video Shared!" && o[1] as string == newVideo.Title && o[2] as string == "newuser"), 
                It.IsAny<CancellationToken>()), Times.Once);
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

//_mockDbSet.As<IAsyncEnumerable<SharedVideo>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
//    .Returns(new TestAsyncEnumerator<SharedVideo>(data.GetEnumerator()));

//_mockContext.Setup(m => m.SharedVideos).Returns(_mockDbSet.Object);
// Mock the Include methods
//_mockContext.Setup(m => m.SharedVideos.Include(It.IsAny<Expression<Func<SharedVideo, object>>>()))
//    .Returns((Expression<Func<SharedVideo, object>> include) => _mockDbSet.Object.Include(include));