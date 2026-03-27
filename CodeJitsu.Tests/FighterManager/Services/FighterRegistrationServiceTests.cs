using AutoMapper;
using FighterManager.Server.Domain.FighterService;
using FighterManager.Server.Helpers;
using FighterManager.Server.Models.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using SharedEntities.Models;
using CodeJitsu.Tests.Helpers;

namespace CodeJitsu.Tests.FighterManager.Services;

public class FighterRegistrationServiceTests
{
    private readonly Mock<UserManager<AppUserEntity>> _userManagerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly FighterRegistrationService _service;

    public FighterRegistrationServiceTests()
    {
        var store = new Mock<IUserStore<AppUserEntity>>();
        _userManagerMock = new Mock<UserManager<AppUserEntity>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();

        _service = new FighterRegistrationService(
            _userManagerMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    private static CreateFighterDto BuildCreateFighterDto() => new()
    {
        FighterName = "Test Fighter",
        Email = "fighter@codejitsu.com",
        Password = "TestPass123",
        Height = 175,
        Weight = 75,
        Gender = "Male",
        FighterRole = "Student",
        MaxWorkoutDuration = 60,
        BeltColor = "White",
        Experience = TrainingExperience.LessThanTwoYears,
        Birthdate = new DateTime(1995, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task Should_ReturnSuccessAndFighter_When_RegistrationSucceeds()
    {
        // Arrange
        var dto = BuildCreateFighterDto();
        var newFighter = TestFixtures.CreateFighter(id: 1);

        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(t => t.CommitAsync(default)).Returns(Task.CompletedTask);

        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Fighter>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<CreateFighterDto, Fighter>(dto)).Returns(newFighter);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<AppUserEntity>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var (result, fighter) = await _service.RegisterFighterAsync(dto);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(fighter);
        Assert.Equal(newFighter.Id, fighter.Id);
    }

    [Fact]
    public async Task Should_ReturnFailureAndNullFighter_When_IdentityCreateFails()
    {
        // Arrange
        var dto = BuildCreateFighterDto();
        var newFighter = TestFixtures.CreateFighter(id: 1);

        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(t => t.RollbackAsync(default)).Returns(Task.CompletedTask);

        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Fighter>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<CreateFighterDto, Fighter>(dto)).Returns(newFighter);

        var identityErrors = new[] { new IdentityError { Code = "DuplicateEmail", Description = "Email already taken." } };
        var failedResult = IdentityResult.Failed(identityErrors);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<AppUserEntity>(), dto.Password))
            .ReturnsAsync(failedResult);

        // Act
        var (result, fighter) = await _service.RegisterFighterAsync(dto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(fighter);
        transactionMock.Verify(t => t.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task Should_MapDtoToFighter_When_RegistrationCalled()
    {
        // Arrange
        var dto = BuildCreateFighterDto();
        var newFighter = TestFixtures.CreateFighter(id: 1);

        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(t => t.CommitAsync(default)).Returns(Task.CompletedTask);

        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Fighter>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<CreateFighterDto, Fighter>(dto)).Returns(newFighter);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<AppUserEntity>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _service.RegisterFighterAsync(dto);

        // Assert
        _mapperMock.Verify(m => m.Map<CreateFighterDto, Fighter>(dto), Times.Once);
    }

    [Fact]
    public async Task Should_AssignFighterIdToUser_When_FighterSavedBeforeUserCreation()
    {
        // Arrange
        var dto = BuildCreateFighterDto();
        var newFighter = TestFixtures.CreateFighter(id: 99);
        AppUserEntity? capturedUser = null;

        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(t => t.CommitAsync(default)).Returns(Task.CompletedTask);

        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Fighter>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<CreateFighterDto, Fighter>(dto)).Returns(newFighter);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<AppUserEntity>(), dto.Password))
            .Callback<AppUserEntity, string>((user, _) => capturedUser = user)
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _service.RegisterFighterAsync(dto);

        // Assert
        Assert.NotNull(capturedUser);
        Assert.Equal(99, capturedUser!.FighterId);
        Assert.Equal(dto.Email, capturedUser.Email);
    }

    [Fact]
    public async Task Should_CommitTransaction_When_RegistrationSucceeds()
    {
        // Arrange
        var dto = BuildCreateFighterDto();
        var newFighter = TestFixtures.CreateFighter(id: 1);

        var transactionMock = new Mock<IDbContextTransaction>();
        transactionMock.Setup(t => t.CommitAsync(default)).Returns(Task.CompletedTask);

        var repoMock = new Mock<IRepository<Fighter>>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Fighter>())).Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Fighter>()).Returns(repoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<CreateFighterDto, Fighter>(dto)).Returns(newFighter);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<AppUserEntity>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _service.RegisterFighterAsync(dto);

        // Assert
        transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
        transactionMock.Verify(t => t.RollbackAsync(default), Times.Never);
    }
}

