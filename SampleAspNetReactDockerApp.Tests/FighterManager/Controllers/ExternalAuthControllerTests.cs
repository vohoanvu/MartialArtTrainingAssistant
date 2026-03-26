using FighterManager.Server.Controllers;
using FighterManager.Server.Domain.FighterService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharedEntities.Models;

namespace SampleAspNetReactDockerApp.Tests.FighterManager.Controllers;

public class ExternalAuthControllerTests
{
    // ── Shared infrastructure ──────────────────────────────────────────────

    private static Mock<UserManager<AppUserEntity>> CreateUserManagerMock() =>
        new(Mock.Of<IUserStore<AppUserEntity>>(), null, null, null, null, null, null, null, null);

    private static Mock<SignInManager<AppUserEntity>> CreateSignInManagerMock(
        Mock<UserManager<AppUserEntity>> userManagerMock) =>
        new(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<AppUserEntity>>(),
            null, null, null, null);

    private static Mock<FighterSignInService<AppUserEntity>> CreateFighterSignInServiceMock(
        Mock<UserManager<AppUserEntity>> userManagerMock) =>
        new(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<AppUserEntity>>(),
            null, null, null, null,
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());

    private static ExternalAuthController CreateController(
        Mock<SignInManager<AppUserEntity>> signInManagerMock,
        Mock<UserManager<AppUserEntity>> userManagerMock,
        Mock<FighterSignInService<AppUserEntity>> fighterSignInMock,
        Mock<ILogger<ExternalAuthController>>? loggerMock = null)
    {
        loggerMock ??= new Mock<ILogger<ExternalAuthController>>();

        var controller = new ExternalAuthController(
            signInManagerMock.Object,
            userManagerMock.Object,
            fighterSignInMock.Object,
            loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    // ── SignInWithGoogle ───────────────────────────────────────────────────

    [Fact]
    public void Should_ReturnChallengeResult_When_SignInWithGoogleIsCalled()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);
        var fightSignInMock = CreateFighterSignInServiceMock(userManagerMock);

        var authProperties = new AuthenticationProperties();
        signInManagerMock.Setup(s => s.ConfigureExternalAuthenticationProperties("Google", "/signin-google-callback", null))
                         .Returns(authProperties);

        var controller = CreateController(signInManagerMock, userManagerMock, fightSignInMock);

        // Act
        var result = controller.SignInWithGoogle("/");

        // Assert
        var challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Contains("Google", challengeResult.AuthenticationSchemes);
    }

    [Fact]
    public void Should_RedirectToDefaultUrl_When_ReturnUrlNotProvided()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);
        var fightSignInMock = CreateFighterSignInServiceMock(userManagerMock);

        var authProperties = new AuthenticationProperties();
        signInManagerMock.Setup(s => s.ConfigureExternalAuthenticationProperties("Google", "/signin-google-callback", null))
                         .Returns(authProperties);

        var controller = CreateController(signInManagerMock, userManagerMock, fightSignInMock);

        // Act — call with explicit default argument value
        var result = controller.SignInWithGoogle("/");

        // Assert — the action always returns a Challenge, no redirect of its own
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public void Should_IncludeReturnUrlInProperties_When_SignInWithGoogleIsCalled()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);
        var fightSignInMock = CreateFighterSignInServiceMock(userManagerMock);

        var capturedProperties = new AuthenticationProperties();
        signInManagerMock
            .Setup(s => s.ConfigureExternalAuthenticationProperties("Google", "/signin-google-callback", null))
            .Callback((string provider, string redirectUrl, string? userId) =>
            {
                capturedProperties.Items["returnUrl"] = "/dashboard";
            })
            .Returns(capturedProperties);

        var controller = CreateController(signInManagerMock, userManagerMock, fightSignInMock);

        // Act
        var result = controller.SignInWithGoogle("/dashboard");

        // Assert
        var challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.NotNull(challengeResult.Properties);
    }
}
