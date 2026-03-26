using AutoMapper;
using FighterManager.Server.Controllers;
using FighterManager.Server.Domain.AttendanceService;
using FighterManager.Server.Helpers;
using FighterManager.Server.Models.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SampleAspNetReactDockerApp.Tests.Helpers;
using SharedEntities.Data;
using SharedEntities.Models;
using System.Security.Claims;

namespace SampleAspNetReactDockerApp.Tests.FighterManager.Controllers;

public class TrainingSessionControllerTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static TrainingSessionController CreateController(
        MyDatabaseContext dbContext,
        Mock<IMapper>? mapperMock = null,
        Mock<IAttendanceService>? attendanceMock = null,
        string? authUserId = "instructor-user-id")
    {
        mapperMock ??= new Mock<IMapper>();
        attendanceMock ??= new Mock<IAttendanceService>();

        // Build a real UnitOfWork backed by the in-memory db
        var unitOfWork = new UnitOfWork(dbContext);

        var controller = new TrainingSessionController(unitOfWork, mapperMock.Object, attendanceMock.Object);

        var claims = new List<Claim>();
        if (authUserId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, authUserId));

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    private static (MyDatabaseContext db, AppUserEntity instructor, Fighter instructorFighter, TrainingSession session)
        SeedSessionWithInstructor(string userId = "instructor-user-id")
    {
        var db = InMemoryDbContextFactory.Create();

        var fighter = TestFixtures.CreateFighter(1, "Instructor Fighter", role: FighterRole.Instructor);
        db.Fighters.Add(fighter);

        var user = TestFixtures.CreateAppUser(id: userId, fighterId: fighter.Id);
        db.Users.Add(user);

        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: fighter.Id);
        db.TrainingSessions.Add(session);

        db.SaveChanges();
        return (db, user, fighter, session);
    }

    // ── GetSessionsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnOkWithSessions_When_InstructorHasSessions()
    {
        // Arrange
        var (db, _, _, session) = SeedSessionWithInstructor();

        var dtos = new List<TrainingSessionDtoBase> { new() { Id = session.Id, Capacity = session.Capacity } };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<TrainingSession>, List<TrainingSessionDtoBase>>(It.IsAny<List<TrainingSession>>()))
                  .Returns(dtos);

        var controller = CreateController(db, mapperMock);

        // Act
        var result = await controller.GetSessionsAsync();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<TrainingSessionDtoBase>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task Should_ReturnOkWithEmptyList_When_InstructorHasNoSessions()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var fighter = TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor);
        db.Fighters.Add(fighter);
        var user = TestFixtures.CreateAppUser(id: "instructor-user-id", fighterId: fighter.Id);
        db.Users.Add(user);
        db.SaveChanges();

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<TrainingSession>, List<TrainingSessionDtoBase>>(It.IsAny<List<TrainingSession>>()))
                  .Returns(new List<TrainingSessionDtoBase>());

        var controller = CreateController(db, mapperMock);

        // Act
        var result = await controller.GetSessionsAsync();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<TrainingSessionDtoBase>>(ok.Value);
        Assert.Empty(list);
    }

    // ── GetSessionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnOkWithSessionDetail_When_SessionExists()
    {
        // Arrange
        var (db, _, instructorFighter, session) = SeedSessionWithInstructor();

        var instructorDto = new ViewFighterDto { Id = instructorFighter.Id, FighterName = instructorFighter.FighterName, Height = instructorFighter.Height, Weight = instructorFighter.Weight, Gender = instructorFighter.Gender.ToString(), FighterRole = instructorFighter.Role.ToString(), MaxWorkoutDuration = instructorFighter.MaxWorkoutDuration, BeltColor = instructorFighter.BelkRank.ToString(), Experience = instructorFighter.Experience };
        var detailResponse = new GetSessionDetailResponse
        {
            Id = session.Id,
            Capacity = session.Capacity,
            Instructor = instructorDto
        };

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<TrainingSession, GetSessionDetailResponse>(It.IsAny<TrainingSession>()))
                  .Returns(detailResponse);

        var controller = CreateController(db, mapperMock);

        // Act
        var result = await controller.GetSessionAsync(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<GetSessionDetailResponse>(ok.Value);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_SessionDoesNotExist()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.GetSessionAsync(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── CreateSessionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task Should_Return201_When_InstructorCreatesSession()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var fighter = TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor);
        db.Fighters.Add(fighter);
        var user = TestFixtures.CreateAppUser(id: "instructor-user-id", fighterId: fighter.Id);
        db.Users.Add(user);
        db.SaveChanges();

        var input = new TrainingSessionDtoBase
        {
            TrainingDate = DateTime.UtcNow.AddDays(1),
            Capacity = 15,
            Duration = 1.5,
            Status = "Active"
        };

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<TrainingSessionDtoBase, TrainingSession>(input))
                  .Returns(TestFixtures.CreateTrainingSession());

        var controller = CreateController(db, mapperMock);

        // Act
        var result = await controller.CreateSessionAsync(input);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, statusResult.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_NonInstructorTriesToCreateSession()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var fighter = TestFixtures.CreateFighter(1, "Student Fighter", role: FighterRole.Student);
        db.Fighters.Add(fighter);
        var user = TestFixtures.CreateAppUser(id: "student-user-id", fighterId: fighter.Id);
        db.Users.Add(user);
        db.SaveChanges();

        var input = new TrainingSessionDtoBase { TrainingDate = DateTime.UtcNow.AddDays(1), Capacity = 10, Duration = 1.0, Status = "Active" };
        var controller = CreateController(db, authUserId: "student-user-id");

        // Act
        var result = await controller.CreateSessionAsync(input);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CreateSessionHasInvalidStatus()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var input = new TrainingSessionDtoBase
        {
            TrainingDate = DateTime.UtcNow.AddDays(1),
            Capacity = 10,
            Duration = 1.0,
            Status = "NotAValidStatus"
        };

        var controller = CreateController(db);

        // Act
        var result = await controller.CreateSessionAsync(input);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ── UpdateSessionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnOkWithUpdatedSession_When_UpdateSucceeds()
    {
        // Arrange
        var (db, _, instructorFighter, _) = SeedSessionWithInstructor();

        var input = new UpdateSessionDetailsRequest { Capacity = 25, Duration = 2.0, Status = "Active" };
        var instructorDto = new ViewFighterDto { Id = instructorFighter.Id, FighterName = instructorFighter.FighterName, Height = instructorFighter.Height, Weight = instructorFighter.Weight, Gender = instructorFighter.Gender.ToString(), FighterRole = instructorFighter.Role.ToString(), MaxWorkoutDuration = instructorFighter.MaxWorkoutDuration, BeltColor = instructorFighter.BelkRank.ToString(), Experience = instructorFighter.Experience };
        var responseDto = new GetSessionDetailResponse { Id = 1, Capacity = 25, Instructor = instructorDto };

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<GetSessionDetailResponse>(It.IsAny<TrainingSession>()))
                  .Returns(responseDto);

        var controller = CreateController(db, mapperMock);

        // Act
        var result = await controller.UpdateSessionAsync(1, input);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<GetSessionDetailResponse>(ok.Value);
        Assert.Equal(25, dto.Capacity);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdateTargetSessionDoesNotExist()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var input = new UpdateSessionDetailsRequest { Capacity = 25 };
        var controller = CreateController(db);

        // Act
        var result = await controller.UpdateSessionAsync(999, input);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ── CloseSessionAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnNoContent_When_CloseSessionSucceeds()
    {
        // Arrange
        var (db, _, _, _) = SeedSessionWithInstructor();
        var controller = CreateController(db);

        // Act
        var result = await controller.CloseSessionAsync(1);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify the status was updated
        var updatedSession = db.TrainingSessions.Find(1);
        Assert.Equal(SessionStatus.Completed, updatedSession!.Status);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CloseSessionTargetDoesNotExist()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.CloseSessionAsync(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    // ── DeleteSessionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnNoContent_When_DeleteSessionSucceeds()
    {
        // Arrange
        var (db, _, _, _) = SeedSessionWithInstructor();
        var controller = CreateController(db);

        // Act
        var result = await controller.DeleteSessionAsync(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Null(db.TrainingSessions.Find(1));
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_DeleteSessionTargetDoesNotExist()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.DeleteSessionAsync(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    // ── TakeAttendance ─────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnOk_When_AttendanceProcessedSuccessfully()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var request = new TakeAttendanceRequest
        {
            Records = new List<AttendanceRecordDto>
            {
                new() { FighterName = "Student A", Birthdate = new DateTime(2000, 1, 1), Weight = 70, Height = 170, BeltColor = "White", Gender = "Male" }
            }
        };

        var instructorDto = new ViewFighterDto { Id = 1, FighterName = "Instructor", Height = 175, Weight = 75, Gender = "Male", FighterRole = "Instructor", MaxWorkoutDuration = 60, BeltColor = "Black", Experience = TrainingExperience.MoreThanFiveYears };
        var successResponse = new TakeAttendanceResponse
        {
            Success = true,
            Message = "Attendance recorded successfully",
            UpdatedSession = new GetSessionDetailResponse { Id = 1, Instructor = instructorDto }
        };

        var attendanceMock = new Mock<IAttendanceService>();
        attendanceMock.Setup(s => s.ProcessAttendanceAsync(1, request.Records, "instructor-user-id"))
                      .ReturnsAsync(successResponse);

        var controller = CreateController(db, attendanceMock: attendanceMock);

        // Act
        var result = await controller.TakeAttendance(1, request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TakeAttendanceResponse>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_AttendanceProcessingFails()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var request = new TakeAttendanceRequest { Records = [] };

        var failResponse = new TakeAttendanceResponse { Success = false, Message = "No attendance records provided" };

        var attendanceMock = new Mock<IAttendanceService>();
        attendanceMock.Setup(s => s.ProcessAttendanceAsync(1, request.Records, "instructor-user-id"))
                      .ReturnsAsync(failResponse);

        var controller = CreateController(db, attendanceMock: attendanceMock);

        // Act
        var result = await controller.TakeAttendance(1, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ── RemoveStudentAttendanceAsync ───────────────────────────────────────

    [Fact]
    public async Task Should_ReturnNoContent_When_StudentRemovedFromSession()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();

        var instructorFighter = TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor);
        var studentFighter = TestFixtures.CreateFighter(2, "Student A", role: FighterRole.Student);
        db.Fighters.AddRange(instructorFighter, studentFighter);

        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 1);
        db.TrainingSessions.Add(session);

        var joint = new TrainingSessionFighterJoint { Id = 1, TrainingSessionId = 1, FighterId = 2, Fighter = studentFighter };
        db.TrainingSessionFighterJoints.Add(joint);
        db.SaveChanges();

        var controller = CreateController(db);

        // Act
        var result = await controller.RemoveStudentAttendanceAsync(1, 2);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.TrainingSessionFighterJoints.Where(j => j.FighterId == 2));
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_SessionNotFoundForAttendanceRemoval()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.RemoveStudentAttendanceAsync(999, 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_RemoveAttendanceFighterIdIsInvalid()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.RemoveStudentAttendanceAsync(1, 0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_FighterNotInSession()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var instructorFighter = TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor);
        db.Fighters.Add(instructorFighter);
        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 1);
        db.TrainingSessions.Add(session);
        db.SaveChanges();

        var controller = CreateController(db);

        // Act
        var result = await controller.RemoveStudentAttendanceAsync(1, 99);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
