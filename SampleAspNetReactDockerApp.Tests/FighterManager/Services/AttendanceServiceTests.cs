using AutoMapper;
using FighterManager.Server.Domain.AttendanceService;
using FighterManager.Server.Models.Dtos;
using FighterManager.Server.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using SharedEntities.Models;
using SampleAspNetReactDockerApp.Tests.Helpers;

namespace SampleAspNetReactDockerApp.Tests.FighterManager.Services;

public class AttendanceServiceTests
{
    private readonly Mock<IAttendanceRepository> _repoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<UserManager<AppUserEntity>> _userManagerMock;
    private readonly Mock<ILogger<AttendanceService>> _loggerMock;
    private readonly AttendanceService _service;

    public AttendanceServiceTests()
    {
        _repoMock = new Mock<IAttendanceRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<AttendanceService>>();

        var store = new Mock<IUserStore<AppUserEntity>>();
        _userManagerMock = new Mock<UserManager<AppUserEntity>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _service = new AttendanceService(
            _repoMock.Object,
            _mapperMock.Object,
            _userManagerMock.Object,
            _loggerMock.Object);
    }

    private static AttendanceRecordDto MakeRecord(string name = "Alice") => new()
    {
        FighterName = name,
        Birthdate = new DateTime(1995, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        Weight = 65,
        Height = 170,
        BeltColor = "White",
        Gender = "Female"
    };

    [Fact]
    public async Task Should_ReturnFailure_When_NoRecordsProvided()
    {
        // Arrange
        var records = new List<AttendanceRecordDto>();

        // Act
        var result = await _service.ProcessAttendanceAsync(1, records, "instructor-id");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No attendance records provided", result.Message);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_DuplicateFighterNamesInRecords()
    {
        // Arrange
        var records = new List<AttendanceRecordDto>
        {
            MakeRecord("Alice"),
            MakeRecord("alice") // duplicate (case-insensitive)
        };

        // Act
        var result = await _service.ProcessAttendanceAsync(1, records, "instructor-id");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("alice", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_SessionNotFound()
    {
        // Arrange
        var records = new List<AttendanceRecordDto> { MakeRecord() };
        _repoMock.Setup(r => r.GetSessionWithDetailsAsync(1)).ReturnsAsync((TrainingSession?)null);

        // Act
        var result = await _service.ProcessAttendanceAsync(1, records, "instructor-id");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Training Session not found", result.Message);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_CallerIsNotSessionInstructor()
    {
        // Arrange
        var records = new List<AttendanceRecordDto> { MakeRecord() };
        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 42);
        var instructorUser = TestFixtures.CreateAppUser(id: "real-instructor-id");

        _repoMock.Setup(r => r.GetSessionWithDetailsAsync(1)).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetAppUserByFighterIdAsync(42)).ReturnsAsync(instructorUser);

        // Act
        var result = await _service.ProcessAttendanceAsync(1, records, "wrong-instructor-id");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Unauthorized User to access this session", result.Message);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_FighterAlreadyRegisteredForSession()
    {
        // Arrange
        var records = new List<AttendanceRecordDto> { MakeRecord("Bob") };
        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 42);
        var instructorUser = TestFixtures.CreateAppUser(id: "instructor-id");
        var existingFighter = TestFixtures.CreateFighter(id: 5, fighterName: "Bob");
        var existingJoint = new TrainingSessionFighterJoint { TrainingSessionId = 1, FighterId = 5 };

        _repoMock.Setup(r => r.GetSessionWithDetailsAsync(1)).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetAppUserByFighterIdAsync(42)).ReturnsAsync(instructorUser);
        _repoMock.Setup(r => r.GetFighterByNameAsync("Bob")).ReturnsAsync(existingFighter);
        _repoMock.Setup(r => r.UpdateFighterAsync(It.IsAny<Fighter>())).ReturnsAsync(existingFighter);
        _repoMock.Setup(r => r.GetSessionFighterJointAsync(1, 5)).ReturnsAsync(existingJoint);

        // Act
        var result = await _service.ProcessAttendanceAsync(1, records, "instructor-id");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Bob", result.Message);
        Assert.Contains("already registered", result.Message);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_NewWalkInFighterAttends()
    {
        // Arrange
        var records = new List<AttendanceRecordDto> { MakeRecord("Charlie") };
        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 42);
        var instructorUser = TestFixtures.CreateAppUser(id: "instructor-id");
        var newFighter = TestFixtures.CreateFighter(id: 10, fighterName: "Charlie", isWalkIn: true);
        var updatedSession = TestFixtures.CreateTrainingSession(id: 1, instructorId: 42);
        var sessionDetailResponse = new GetSessionDetailResponse
        {
            Instructor = new ViewFighterDto { Id = 42, FighterName = "Instructor", Height = 175, Weight = 80, Gender = "Male", FighterRole = "Instructor", MaxWorkoutDuration = 60, BeltColor = "Black", Experience = TrainingExperience.MoreThanFiveYears }
        };

        _repoMock.SetupSequence(r => r.GetSessionWithDetailsAsync(1))
            .ReturnsAsync(session)
            .ReturnsAsync(updatedSession);
        _repoMock.Setup(r => r.GetAppUserByFighterIdAsync(42)).ReturnsAsync(instructorUser);
        _repoMock.Setup(r => r.GetFighterByNameAsync("Charlie")).ReturnsAsync((Fighter?)null);
        _mapperMock.Setup(m => m.Map<Fighter>(It.IsAny<AttendanceRecordDto>())).Returns(newFighter);
        _repoMock.Setup(r => r.AddFighterAsync(It.IsAny<Fighter>())).ReturnsAsync(newFighter);
        _repoMock.Setup(r => r.GetSessionFighterJointAsync(1, 10)).ReturnsAsync((TrainingSessionFighterJoint?)null);
        _repoMock.Setup(r => r.AddSessionFighterJointAsync(It.IsAny<TrainingSessionFighterJoint>()))
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapperMock.Setup(m => m.Map<GetSessionDetailResponse>(It.IsAny<TrainingSession>()))
            .Returns(sessionDetailResponse);

        // Act
        var result = await _service.ProcessAttendanceAsync(1, records, "instructor-id");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Attendance recorded successfully", result.Message);
    }

    [Fact]
    public async Task Should_UpdateExistingFighter_When_WalkInFighterAlreadyInDatabase()
    {
        // Arrange
        var records = new List<AttendanceRecordDto> { MakeRecord("Dave") };
        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 42);
        var instructorUser = TestFixtures.CreateAppUser(id: "instructor-id");
        var existingFighter = TestFixtures.CreateFighter(id: 7, fighterName: "Dave");
        var updatedSession = TestFixtures.CreateTrainingSession(id: 1, instructorId: 42);
        var sessionDetailResponse = new GetSessionDetailResponse
        {
            Instructor = new ViewFighterDto { Id = 42, FighterName = "Instructor", Height = 175, Weight = 80, Gender = "Male", FighterRole = "Instructor", MaxWorkoutDuration = 60, BeltColor = "Black", Experience = TrainingExperience.MoreThanFiveYears }
        };

        _repoMock.SetupSequence(r => r.GetSessionWithDetailsAsync(1))
            .ReturnsAsync(session)
            .ReturnsAsync(updatedSession);
        _repoMock.Setup(r => r.GetAppUserByFighterIdAsync(42)).ReturnsAsync(instructorUser);
        _repoMock.Setup(r => r.GetFighterByNameAsync("Dave")).ReturnsAsync(existingFighter);
        _repoMock.Setup(r => r.UpdateFighterAsync(It.IsAny<Fighter>())).ReturnsAsync(existingFighter);
        _repoMock.Setup(r => r.GetSessionFighterJointAsync(1, 7)).ReturnsAsync((TrainingSessionFighterJoint?)null);
        _repoMock.Setup(r => r.AddSessionFighterJointAsync(It.IsAny<TrainingSessionFighterJoint>()))
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapperMock.Setup(m => m.Map<GetSessionDetailResponse>(It.IsAny<TrainingSession>()))
            .Returns(sessionDetailResponse);

        // Act
        var result = await _service.ProcessAttendanceAsync(1, records, "instructor-id");

        // Assert
        Assert.True(result.Success);
        _repoMock.Verify(r => r.UpdateFighterAsync(It.Is<Fighter>(f => f.IsWalkIn)), Times.Once);
    }

    [Fact]
    public async Task Should_DetermineCorrectExperienceLevel_When_BeltColorIsBlue()
    {
        // Arrange
        var record = new AttendanceRecordDto
        {
            FighterName = "Eve",
            Birthdate = new DateTime(1995, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Weight = 60,
            Height = 165,
            BeltColor = "Blue",
            Gender = "Female"
        };
        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 42);
        var instructorUser = TestFixtures.CreateAppUser(id: "instructor-id");
        Fighter capturedFighter = null!;
        var newFighter = TestFixtures.CreateFighter(id: 11, fighterName: "Eve");
        var updatedSession = TestFixtures.CreateTrainingSession(id: 1, instructorId: 42);
        var sessionDetailResponse = new GetSessionDetailResponse
        {
            Instructor = new ViewFighterDto { Id = 42, FighterName = "Instructor", Height = 175, Weight = 80, Gender = "Male", FighterRole = "Instructor", MaxWorkoutDuration = 60, BeltColor = "Black", Experience = TrainingExperience.MoreThanFiveYears }
        };

        _repoMock.SetupSequence(r => r.GetSessionWithDetailsAsync(1))
            .ReturnsAsync(session)
            .ReturnsAsync(updatedSession);
        _repoMock.Setup(r => r.GetAppUserByFighterIdAsync(42)).ReturnsAsync(instructorUser);
        _repoMock.Setup(r => r.GetFighterByNameAsync("Eve")).ReturnsAsync((Fighter?)null);
        _mapperMock.Setup(m => m.Map<Fighter>(It.IsAny<AttendanceRecordDto>())).Returns(newFighter);
        _repoMock.Setup(r => r.AddFighterAsync(It.IsAny<Fighter>()))
            .Callback<Fighter>(f => capturedFighter = f)
            .ReturnsAsync(newFighter);
        _repoMock.Setup(r => r.GetSessionFighterJointAsync(1, 11)).ReturnsAsync((TrainingSessionFighterJoint?)null);
        _repoMock.Setup(r => r.AddSessionFighterJointAsync(It.IsAny<TrainingSessionFighterJoint>()))
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapperMock.Setup(m => m.Map<GetSessionDetailResponse>(It.IsAny<TrainingSession>()))
            .Returns(sessionDetailResponse);

        // Act
        await _service.ProcessAttendanceAsync(1, new List<AttendanceRecordDto> { record }, "instructor-id");

        // Assert - experience on the mapped fighter object is set by the service
        _repoMock.Verify(r => r.AddFighterAsync(It.Is<Fighter>(f =>
            f.Experience == TrainingExperience.FromTwoToFiveYears)), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_ExceptionOccurs()
    {
        // Arrange
        var records = new List<AttendanceRecordDto> { MakeRecord() };
        _repoMock.Setup(r => r.GetSessionWithDetailsAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB connection failed"));

        // Act
        var result = await _service.ProcessAttendanceAsync(1, records, "instructor-id");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("DB connection failed", result.Message);
    }
}
