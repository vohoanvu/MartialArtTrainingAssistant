using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Domain.GeminiService;
using VideoSharingDtos = VideoSharing.Server.Models.Dtos;
using SampleAspNetReactDockerApp.Tests.Helpers;

namespace SampleAspNetReactDockerApp.Tests.VideoSharing.Services;

public class AiAnalysisProcessorServiceTests
{
    private static MyDatabaseContext CreateContext() => InMemoryDbContextFactory.Create();

    private const string ValidAnalysisJson = """
        {
            "overall_description": "Student showed good fundamentals.",
            "techniques_identified": [
                {
                    "technique_name": "Armbar",
                    "description": "Executed from guard.",
                    "start_timestamp": "00:01:00",
                    "end_timestamp": "00:01:15",
                    "technique_type": "Submission",
                    "positional_scenario": "Guard"
                }
            ],
            "strengths": [
                {
                    "description": "Good hip movement.",
                    "related_technique": "Armbar"
                }
            ],
            "areas_for_improvement": [
                {
                    "description": "Needs better guard retention.",
                    "weakness_category": "Guard Passing/Retention",
                    "related_technique": null,
                    "keywords": "guard retention BJJ"
                }
            ],
            "suggested_drills": [
                {
                    "name": "Guard Retention Drill",
                    "description": "Start in open guard and prevent passing.",
                    "focus": "Hip movement and framing.",
                    "duration": "5 minutes",
                    "related_technique": "Armbar"
                }
            ]
        }
        """;

    private static async Task SeedVideoAndUser(MyDatabaseContext context, int videoId = 1)
    {
        var user = TestFixtures.CreateAppUser(id: "user-1");
        context.Users.Add(user);
        var video = TestFixtures.CreateVideoMetadata(id: videoId, userId: "user-1");
        context.Videos.Add(video);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_JsonIsNullOrEmpty()
    {
        // Arrange
        using var context = CreateContext();
        var service = new AiAnalysisProcessorService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProcessAnalysisJsonAsync(string.Empty, videoId: 1));
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_JsonIsNull()
    {
        // Arrange
        using var context = CreateContext();
        var service = new AiAnalysisProcessorService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ProcessAnalysisJsonAsync(null!, videoId: 1));
    }

    [Fact]
    public async Task Should_CreateNewAiAnalysisResult_When_NoExistingResultForVideo()
    {
        // Arrange
        using var context = CreateContext();
        await SeedVideoAndUser(context, videoId: 1);
        var service = new AiAnalysisProcessorService(context);

        // Act
        await service.ProcessAnalysisJsonAsync(ValidAnalysisJson, videoId: 1);

        // Assert
        var result = context.AiAnalysisResults.FirstOrDefault(a => a.VideoId == 1);
        Assert.NotNull(result);
        Assert.Equal(1, result.VideoId);
        Assert.Equal("Student showed good fundamentals.", result.OverallDescription);
    }

    [Fact]
    public async Task Should_UpdateExistingAiAnalysisResult_When_ResultAlreadyExistsForVideo()
    {
        // Arrange
        using var context = CreateContext();
        await SeedVideoAndUser(context, videoId: 2);

        var existing = new AiAnalysisResult
        {
            VideoId = 2,
            AnalysisJson = "{}",
            Strengths = "[]",
            AreasForImprovement = "[]",
            OverallDescription = "Old description",
            Techniques = new List<Techniques>(),
            Drills = new List<Drills>(),
            GeneratedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.AiAnalysisResults.Add(existing);
        await context.SaveChangesAsync();

        var service = new AiAnalysisProcessorService(context);

        // Act
        await service.ProcessAnalysisJsonAsync(ValidAnalysisJson, videoId: 2);

        // Assert
        var updated = context.AiAnalysisResults.FirstOrDefault(a => a.VideoId == 2);
        Assert.NotNull(updated);
        Assert.Equal("Student showed good fundamentals.", updated.OverallDescription);
        Assert.NotNull(updated.LastUpdatedAt);
    }

    [Fact]
    public async Task Should_CreateTechniqueAndType_When_ValidJsonWithTechniquesProcessed()
    {
        // Arrange
        using var context = CreateContext();
        await SeedVideoAndUser(context, videoId: 3);
        var service = new AiAnalysisProcessorService(context);

        // Act
        await service.ProcessAnalysisJsonAsync(ValidAnalysisJson, videoId: 3);

        // Assert
        var technique = context.Techniques.FirstOrDefault(t => t.Name == "Armbar");
        Assert.NotNull(technique);

        var techniqueType = context.TechniqueTypes.FirstOrDefault(tt => tt.Name == "Submission");
        Assert.NotNull(techniqueType);
    }

    [Fact]
    public async Task Should_CreateDrill_When_ValidJsonWithDrillsProcessed()
    {
        // Arrange
        using var context = CreateContext();
        await SeedVideoAndUser(context, videoId: 4);
        var service = new AiAnalysisProcessorService(context);

        // Act
        await service.ProcessAnalysisJsonAsync(ValidAnalysisJson, videoId: 4);

        // Assert
        var drill = context.Drills.FirstOrDefault(d => d.Name == "Guard Retention Drill");
        Assert.NotNull(drill);
        Assert.Equal("5 minutes", drill.Duration);
    }

    [Fact]
    public async Task Should_ThrowInvalidOperationException_When_AnalysisResultNotFoundForGetDto()
    {
        // Arrange
        using var context = CreateContext();
        var service = new AiAnalysisProcessorService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetAnalysisResultDtoByVideoId(videoId: 999));
    }

    [Fact]
    public async Task Should_ReturnAnalysisResultDto_When_ResultExistsForVideoId()
    {
        // Arrange
        using var context = CreateContext();
        await SeedVideoAndUser(context, videoId: 5);
        var service = new AiAnalysisProcessorService(context);
        await service.ProcessAnalysisJsonAsync(ValidAnalysisJson, videoId: 5);

        // Act
        var dto = await service.GetAnalysisResultDtoByVideoId(videoId: 5);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("Student showed good fundamentals.", dto.OverallDescription);
        Assert.NotNull(dto.Techniques);
        Assert.NotEmpty(dto.Techniques);
    }

    [Fact]
    public async Task Should_CreateWeaknessCategory_When_AreasForImprovementHaveWeaknessCategory()
    {
        // Arrange
        using var context = CreateContext();
        await SeedVideoAndUser(context, videoId: 6);
        var service = new AiAnalysisProcessorService(context);

        // Act
        await service.ProcessAnalysisJsonAsync(ValidAnalysisJson, videoId: 6);

        // Assert
        var category = context.WeaknessCategories.FirstOrDefault(w => w.Name == "Guard Passing/Retention");
        Assert.NotNull(category);
    }

    [Fact]
    public async Task Should_CreateVideoSegmentFeedback_When_TechniqueHasTimestamps()
    {
        // Arrange
        using var context = CreateContext();
        await SeedVideoAndUser(context, videoId: 7);
        var service = new AiAnalysisProcessorService(context);

        // Act
        await service.ProcessAnalysisJsonAsync(ValidAnalysisJson, videoId: 7);

        // Assert
        var feedback = context.VideoSegmentFeedbacks.FirstOrDefault(f => f.VideoId == 7);
        Assert.NotNull(feedback);
        Assert.Equal(new TimeSpan(0, 1, 0), feedback.StartTimestamp);
        Assert.Equal(new TimeSpan(0, 1, 15), feedback.EndTimestamp);
    }

    [Fact]
    public async Task Should_ThrowInvalidOperationException_When_AnalysisResultNotFoundForSavePartial()
    {
        // Arrange
        using var context = CreateContext();
        var service = new AiAnalysisProcessorService(context);
        var partialDto = new VideoSharingDtos.PartialAnalysisResultDto
        {
            OverallDescription = "Updated description"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SavePartialAnalysisResultDtoByVideoId(videoId: 999, partialDto));
    }
}

