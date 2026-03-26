using AutoMapper;
using FighterManager.Server.Controllers;
using FighterManager.Server.Domain.FighterService;
using FighterManager.Server.Helpers;
using FighterManager.Server.Models.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SampleAspNetReactDockerApp.Tests.Helpers;
using SharedEntities.Models;
using System.Security.Claims;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace SampleAspNetReactDockerApp.Tests.FighterManager.Controllers;

public class FighterControllerTests
{
    // ── Shared infrastructure ──────────────────────────────────────────────

    private static Mock<UserManager<AppUserEntity>> CreateUserManagerMock() =>
        new(Mock.Of<IUserStore<AppUserEntity>>(), null, null, null, null, null, null, null, null);

    private static Mock<FighterSignInService<AppUserEntity>> CreateSignInServiceMock(
        Mock<UserManager<AppUserEntity>> userManagerMock) =>
        new(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<AppUserEntity>>(),
            null, null, null, null,
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>()
        );

    private static FighterController CreateController(
        Mock<IUnitOfWork> unitOfWorkMock,
        Mock<IMapper> mapperMock,
        Mock<FighterRegistrationService> registrationServiceMock,
        Mock<UserManager<AppUserEntity>> userManagerMock,
        Mock<FighterSignInService<AppUserEntity>> signInServiceMock,
        ClaimsPrincipal? user = null)
    {
        var controller = new FighterController(
            unitOfWorkMock.Object,
            mapperMock.Object,
            registrationServiceMock.Object,
            userManagerMock.Object,
            signInServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        if (user != null)
            httpContext.User = user;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    private static ClaimsPrincipal CreateAuthUser(string userId = "test-user-id") =>
        new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            "TestAuth"));

    // ── GetListAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnOkWithFighters_When_FightersExist()
    {
        // Arrange
        var fighters = new List<Fighter> { TestFixtures.CreateFighter(1), TestFixtures.CreateFighter(2) };
        var dtos = fighters.Select(f => new ViewFighterDto { Id = f.Id, FighterName = f.FighterName, Height = f.Height, Weight = f.Weight, Gender = f.Gender.ToString(), FighterRole = f.Role.ToString(), MaxWorkoutDuration = f.MaxWorkoutDuration, BeltColor = f.BelkRank.ToString(), Experience = f.Experience }).ToList();

        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(fighters);

        var unitMock = new Mock<IUnitOfWork>();
        unitMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<Fighter>, List<ViewFighterDto>>(fighters)).Returns(dtos);

        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.GetListAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDtos = Assert.IsType<List<ViewFighterDto>>(okResult.Value);
        Assert.Equal(2, returnedDtos.Count);
    }

    [Fact]
    public async Task Should_ReturnOkWithEmptyList_When_NoFightersExist()
    {
        // Arrange
        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Fighter>());

        var unitMock = new Mock<IUnitOfWork>();
        unitMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<Fighter>, List<ViewFighterDto>>(It.IsAny<List<Fighter>>()))
                  .Returns(new List<ViewFighterDto>());

        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.GetListAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<ViewFighterDto>>(okResult.Value);
        Assert.Empty(list);
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnOkWithFighter_When_FighterExists()
    {
        // Arrange
        var fighter = TestFixtures.CreateFighter(1);
        var dto = new ViewFighterDto { Id = 1, FighterName = fighter.FighterName, Height = fighter.Height, Weight = fighter.Weight, Gender = fighter.Gender.ToString(), FighterRole = fighter.Role.ToString(), MaxWorkoutDuration = fighter.MaxWorkoutDuration, BeltColor = fighter.BelkRank.ToString(), Experience = fighter.Experience };

        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fighter);

        var unitMock = new Mock<IUnitOfWork>();
        unitMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<Fighter, ViewFighterDto>(fighter)).Returns(dto);

        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.GetByIdAsync(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<ViewFighterDto>(okResult.Value);
        Assert.Equal(1, returnedDto.Id);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_FighterDoesNotExist()
    {
        // Arrange
        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Fighter)null!);

        var unitMock = new Mock<IUnitOfWork>();
        unitMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);

        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.GetByIdAsync(99);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────

    [Fact(Skip = "FighterRegistrationService.RegisterFighterAsync is not virtual; Moq cannot override it on a concrete class. Requires interface extraction or integration test setup.")]
    public async Task Should_Return201_When_FighterRegistrationSucceeds()
    {
        // Arrange
        var input = new CreateFighterDto
        {
            FighterName = "New Fighter",
            Email = "new@codejitsu.com",
            Password = "P@ssw0rd!",
            Height = 175,
            Weight = 75,
            Gender = "Male",
            FighterRole = "Student",
            BeltColor = "White",
            Experience = TrainingExperience.LessThanTwoYears,
            MaxWorkoutDuration = 60
        };
        var createdFighter = TestFixtures.CreateFighter(1, "New Fighter");

        var unitMock = new Mock<IUnitOfWork>();
        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);
        regMock.Setup(r => r.RegisterFighterAsync(input))
               .ReturnsAsync((IdentityResult.Success, createdFighter));

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.CreateAsync(input);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, statusResult.StatusCode);
        var response = Assert.IsType<CustomRegistrationResponse>(statusResult.Value);
        Assert.Equal(201, response.Status);
    }

    [Fact(Skip = "FighterRegistrationService.RegisterFighterAsync is not virtual; Moq cannot override it on a concrete class. Requires interface extraction or integration test setup.")]
    public async Task Should_ReturnBadRequest_When_FighterRegistrationFails()
    {
        // Arrange
        var input = new CreateFighterDto
        {
            FighterName = "Fail Fighter",
            Email = "fail@codejitsu.com",
            Password = "weak",
            Height = 175,
            Weight = 75,
            Gender = "Male",
            FighterRole = "Student",
            BeltColor = "White",
            Experience = TrainingExperience.LessThanTwoYears,
            MaxWorkoutDuration = 60
        };
        var identityErrors = new[] { new IdentityError { Code = "PasswordTooShort", Description = "Password is too short." } };
        var failedResult = IdentityResult.Failed(identityErrors);

        var unitMock = new Mock<IUnitOfWork>();
        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);
        regMock.Setup(r => r.RegisterFighterAsync(input))
               .ReturnsAsync((failedResult, (Fighter)null!));

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.CreateAsync(input);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<CustomRegistrationResponse>(badRequest.Value);
        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_InvalidEnumValuesProvided()
    {
        // Arrange
        var input = new CreateFighterDto
        {
            FighterName = "Bad Enum",
            Email = "badenum@codejitsu.com",
            Password = "P@ssw0rd!",
            Height = 175,
            Weight = 75,
            Gender = "InvalidGender",
            FighterRole = "Student",
            BeltColor = "White",
            Experience = TrainingExperience.LessThanTwoYears,
            MaxWorkoutDuration = 60
        };

        var unitMock = new Mock<IUnitOfWork>();
        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.CreateAsync(input);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnOkWithUpdatedFighter_When_UpdateSucceeds()
    {
        // Arrange
        var fighter = TestFixtures.CreateFighter(1);
        var input = new UpdateFighterDto
        {
            FighterName = "Updated Name",
            Height = 180,
            Weight = 80,
            Gender = "Male",
            FighterRole = "Student",
            BeltColor = "Blue",
            Experience = TrainingExperience.FromTwoToFiveYears,
            MaxWorkoutDuration = 60
        };
        var dto = new ViewFighterDto { Id = 1, FighterName = "Updated Name", Height = 180, Weight = 80, Gender = "Male", FighterRole = "Student", BeltColor = "Blue", Experience = TrainingExperience.FromTwoToFiveYears, MaxWorkoutDuration = 60 };

        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fighter);
        repoMock.Setup(r => r.Update(fighter));

        var unitMock = new Mock<IUnitOfWork>();
        unitMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);
        unitMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<Fighter, ViewFighterDto>(fighter)).Returns(dto);

        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.UpdateAsync(1, input);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<ViewFighterDto>(okResult.Value);
        Assert.Equal("Updated Name", returnedDto.FighterName);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdateTargetFighterDoesNotExist()
    {
        // Arrange
        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Fighter)null!);

        var unitMock = new Mock<IUnitOfWork>();
        unitMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);

        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        var input = new UpdateFighterDto
        {
            FighterName = "Ghost",
            Height = 170,
            Weight = 70,
            Gender = "Male",
            FighterRole = "Student",
            BeltColor = "White",
            Experience = TrainingExperience.LessThanTwoYears,
            MaxWorkoutDuration = 30
        };

        // Act
        var result = await controller.UpdateAsync(99, input);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_UpdateHasInvalidEnums()
    {
        // Arrange
        var unitMock = new Mock<IUnitOfWork>();
        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        var input = new UpdateFighterDto
        {
            FighterName = "Bad",
            Height = 170,
            Weight = 70,
            Gender = "NotAGender",
            FighterRole = "Student",
            BeltColor = "White",
            Experience = TrainingExperience.LessThanTwoYears,
            MaxWorkoutDuration = 30
        };

        // Act
        var result = await controller.UpdateAsync(1, input);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnNoContent_When_DeleteSucceeds()
    {
        // Arrange
        var fighter = TestFixtures.CreateFighter(1);

        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fighter);
        repoMock.Setup(r => r.Delete(fighter));

        var unitMock = new Mock<IUnitOfWork>();
        unitMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);
        unitMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.DeleteAsync(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_DeleteTargetFighterDoesNotExist()
    {
        // Arrange
        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Fighter)null!);

        var unitMock = new Mock<IUnitOfWork>();
        unitMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);

        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.DeleteAsync(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    // ── Login ──────────────────────────────────────────────────────────────

    [Fact(Skip = "FighterSignInService.GenerateJwtTokenAsync is not virtual; Moq cannot override it on a concrete class. Requires interface extraction or integration test setup.")]
    public async Task Should_ReturnOkWithToken_When_LoginSucceeds()
    {
        // Arrange
        var appUser = TestFixtures.CreateAppUser();
        var loginRequest = new CustomLoginRequest { Email = "testuser@codejitsu.com", Password = "P@ssw0rd!" };

        var unitMock = new Mock<IUnitOfWork>();
        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(u => u.FindByNameAsync(loginRequest.Email)).ReturnsAsync(appUser);

        var signInMock = CreateSignInServiceMock(userManagerMock);
        signInMock.Setup(s => s.PasswordSignInAsync(loginRequest.Email, loginRequest.Password, true, false))
                  .ReturnsAsync(IdentitySignInResult.Success);
        signInMock.Setup(s => s.GenerateJwtTokenAsync(appUser)).ReturnsAsync("fake-jwt-token");
        signInMock.Setup(s => s.GenerateRefreshToken(appUser)).Returns("fake-refresh-token");

        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);
        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CustomLoginResponse>(okResult.Value);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal("fake-jwt-token", response.AccessToken);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_LoginFails()
    {
        // Arrange
        var loginRequest = new CustomLoginRequest { Email = "wrong@codejitsu.com", Password = "wrongpassword" };

        var unitMock = new Mock<IUnitOfWork>();
        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();

        var signInMock = CreateSignInServiceMock(userManagerMock);
        signInMock.Setup(s => s.PasswordSignInAsync(loginRequest.Email, loginRequest.Password, true, false))
                  .ReturnsAsync(IdentitySignInResult.Failed);

        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);
        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.Login(loginRequest);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_LoginRequestIsNull()
    {
        // Arrange
        var unitMock = new Mock<IUnitOfWork>();
        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.Login(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── JoinWaitlist ───────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnOk_When_WaitlistJoinSucceeds()
    {
        // Arrange
        var request = new Waitlist { Email = "newuser@test.com", Role = "Student", Region = "US" };

        var repoMock = new Mock<IRepository<Waitlist>>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Waitlist>())).Returns(Task.CompletedTask);

        var unitMock = new Mock<IUnitOfWork>();
        unitMock.Setup(u => u.Repository<Waitlist>()).Returns(repoMock.Object);
        unitMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.JoinWaitlist(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_WaitlistEmailIsEmpty()
    {
        // Arrange
        var request = new Waitlist { Email = "" };

        var unitMock = new Mock<IUnitOfWork>();
        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.JoinWaitlist(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_WaitlistRequestIsNull()
    {
        // Arrange
        var unitMock = new Mock<IUnitOfWork>();
        var mapperMock = new Mock<IMapper>();
        var userManagerMock = CreateUserManagerMock();
        var signInMock = CreateSignInServiceMock(userManagerMock);
        var regMock = new Mock<FighterRegistrationService>(userManagerMock.Object, unitMock.Object, mapperMock.Object);

        var controller = CreateController(unitMock, mapperMock, regMock, userManagerMock, signInMock);

        // Act
        var result = await controller.JoinWaitlist(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
