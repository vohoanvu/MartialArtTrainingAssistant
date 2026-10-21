using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Data;
using VideoAnalysis.Server.Configuration;
using VideoAnalysis.Server.Controllers;
using VideoAnalysis.Server.Domain.AIServices;
using VideoAnalysis.Server.Models.Dtos;

namespace CodeJitsu.Tests.VideoAnalysis.Controllers;

public class AiConsoleControllerTests
{
    private const string AdminEmail = "admin@codejitsu.com";

    private static MyDatabaseContext SeedUsers()
    {
        var db = InMemoryDbContextFactory.Create();
        db.Users.Add(TestFixtures.CreateAppUser(id: "admin-id", userName: "admin", email: AdminEmail, fighterId: 1));
        db.Users.Add(TestFixtures.CreateAppUser(id: "user-id", userName: "user", email: "coach@codejitsu.com", fighterId: 2));
        db.SaveChanges();
        return db;
    }

    private static (AiConsoleController controller, Mock<IAiModelConfigService> config, Mock<IVertexRestClient> vertex)
        CreateController(MyDatabaseContext db, string userId, string adminEmails = AdminEmail)
    {
        var config = new Mock<IAiModelConfigService>();
        var definitions = new List<AiFeatureDefinition>
        {
            new(AiFeatureKeys.VideoAnalysisPipeline, AiFeatureKinds.Pipeline, true, false),
            new(AiFeatureKeys.ClassCurriculum, AiFeatureKinds.Generation, false, false),
        };
        config.Setup(c => c.Features).Returns(definitions);
        config.Setup(c => c.FindFeature(It.IsAny<string>())).Returns((string k) => definitions.FirstOrDefault(d => d.Key == k));
        config.Setup(c => c.GetConfigAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string k, CancellationToken _) => new AiFeatureConfigDto
            {
                Key = k, Kind = AiFeatureKinds.Generation, Defaults = new AiFeatureSettings(), Effective = new AiFeatureSettings(),
            });

        var vertex = new Mock<IVertexRestClient>();
        var controller = new AiConsoleController(config.Object, vertex.Object, db,
            Options.Create(new AiConsoleOptions { AdminEmails = adminEmails }),
            new Mock<ILogger<AiConsoleController>>().Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth")),
                },
            },
        };
        return (controller, config, vertex);
    }

    private static AiFeatureSettings CurriculumSettings(string modelId = "gemini-4-argon") => new()
    {
        ModelId = modelId, Location = "global", ThinkingLevel = "LOW", MaxOutputTokens = 4096,
    };

    [Theory]
    [InlineData("admin-id", AdminEmail, true)]
    [InlineData("admin-id", "Other@x.com; ADMIN@codejitsu.com", true)] // case-insensitive, ';' separated
    [InlineData("user-id", AdminEmail, false)]
    [InlineData("admin-id", "", false)]                                // empty allow-list = console disabled
    public async Task Should_ReportAccess_FromEmailAllowList(string userId, string adminEmails, bool expected)
    {
        using var db = SeedUsers();
        var (controller, _, _) = CreateController(db, userId, adminEmails);

        var result = await controller.GetAccessAsync();

        Assert.Equal(expected, result.Value!.IsAdmin);
    }

    [Fact]
    public async Task Should_Forbid_NonAdmin_OnEveryConsoleAction()
    {
        using var db = SeedUsers();
        var (controller, config, vertex) = CreateController(db, "user-id");

        Assert.IsType<ForbidResult>((await controller.GetOverviewAsync(default)).Result);
        Assert.IsType<ForbidResult>((await controller.UpdateAsync(AiFeatureKeys.ClassCurriculum, CurriculumSettings(), default)).Result);
        Assert.IsType<ForbidResult>((await controller.ResetAsync(AiFeatureKeys.ClassCurriculum, default)).Result);
        Assert.IsType<ForbidResult>((await controller.TestAsync(AiFeatureKeys.ClassCurriculum, CurriculumSettings(), default)).Result);
        config.Verify(c => c.SaveOverrideAsync(It.IsAny<string>(), It.IsAny<AiFeatureSettings>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        vertex.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Should_ReturnModelsAndFeatures_ForAdmin()
    {
        using var db = SeedUsers();
        var (controller, _, _) = CreateController(db, "admin-id");

        var overview = (await controller.GetOverviewAsync(default)).Value!;

        Assert.Equal(2, overview.Features.Count);
        var argon = Assert.Single(overview.Models, m => m.Id == "gemini-4-argon");
        Assert.Equal("level", argon.ThinkingMode);
        Assert.True(argon.SamplingParamsIgnored);
    }

    [Fact]
    public async Task Should_SaveValidatedOverride_AsAdmin()
    {
        using var db = SeedUsers();
        var (controller, config, _) = CreateController(db, "admin-id");
        config.Setup(c => c.SaveOverrideAsync(AiFeatureKeys.ClassCurriculum, It.IsAny<AiFeatureSettings>(), AdminEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiFeatureConfigDto { Key = AiFeatureKeys.ClassCurriculum, Kind = AiFeatureKinds.Generation, IsOverridden = true, Defaults = new(), Effective = new() });

        var result = await controller.UpdateAsync(AiFeatureKeys.ClassCurriculum, CurriculumSettings(), default);

        Assert.True(result.Value!.IsOverridden);
        config.Verify(c => c.SaveOverrideAsync(AiFeatureKeys.ClassCurriculum,
            It.Is<AiFeatureSettings>(s => s.ModelId == "gemini-4-argon" && s.Phases == null), AdminEmail, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_ForInvalidSettings()
    {
        using var db = SeedUsers();
        var (controller, config, _) = CreateController(db, "admin-id");

        var result = await controller.UpdateAsync(AiFeatureKeys.ClassCurriculum, CurriculumSettings("bad/model"), default);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.Equal(400, objectResult.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Contains("modelId", problem.Errors.Keys);
        config.Verify(c => c.SaveOverrideAsync(It.IsAny<string>(), It.IsAny<AiFeatureSettings>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_ReturnNotFound_ForUnknownFeature()
    {
        using var db = SeedUsers();
        var (controller, _, _) = CreateController(db, "admin-id");

        Assert.IsType<NotFoundObjectResult>((await controller.UpdateAsync("nope", CurriculumSettings(), default)).Result);
    }

    [Fact]
    public async Task Should_ReportOk_When_ModelTestSucceeds()
    {
        using var db = SeedUsers();
        var (controller, _, vertex) = CreateController(db, "admin-id");
        var response = JsonSerializer.SerializeToElement(new { modelVersion = "gemini-4-argon-001" });
        vertex.Setup(v => v.GenerateContentAsync("gemini-4-argon", "global", It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
        vertex.Setup(v => v.ExtractText(response)).Returns(" OK ");

        var result = (await controller.TestAsync(AiFeatureKeys.ClassCurriculum, CurriculumSettings(), default)).Value!;

        Assert.True(result.Ok);
        Assert.Equal("OK", result.Sample);
        Assert.Equal("gemini-4-argon-001", result.ResponseModelVersion);
    }

    [Fact]
    public async Task Should_ReportError_When_VertexRejectsModel()
    {
        using var db = SeedUsers();
        var (controller, _, vertex) = CreateController(db, "admin-id");
        vertex.Setup(v => v.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Vertex AI request failed with 404: model not found", null, HttpStatusCode.NotFound));

        var result = (await controller.TestAsync(AiFeatureKeys.ClassCurriculum, CurriculumSettings("gemini-9-nonexistent"), default)).Value!;

        Assert.False(result.Ok);
        Assert.Contains("404", result.Error);
    }
}
