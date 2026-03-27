using MatchMaker.Server.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Models;
using System.Security.Claims;

namespace CodeJitsu.Tests.MatchMaker.Controllers;

public class MatchMakerControllerTests
{
    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static MatchMakerController CreateController(SharedEntities.Data.MyDatabaseContext db)
    {
        var controller = new MatchMakerController(db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
        return controller;
    }

    // â”€â”€ GeneratePairs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnOkWithPairs_When_ValidStudentsAndInstructorProvided()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();

        var instructor = TestFixtures.CreateFighter(1, "Instructor Joe", role: FighterRole.Instructor,
            weight: 80, height: 180);
        var student1 = TestFixtures.CreateFighter(2, "Student Alice", role: FighterRole.Student,
            weight: 60, height: 165);
        var student2 = TestFixtures.CreateFighter(3, "Student Bob", role: FighterRole.Student,
            weight: 75, height: 178);

        db.Fighters.AddRange(instructor, student1, student2);
        db.SaveChanges();

        var controller = CreateController(db);
        var request = new MatchMakerDto
        {
            StudentFighterIds = new List<int> { 2, 3 },
            InstructorFighterId = 1
        };

        // Act
        var result = await controller.GeneratePairs(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_NoValidStudentsFoundForGivenIds()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var instructor = TestFixtures.CreateFighter(1, "Instructor Joe", role: FighterRole.Instructor);
        db.Fighters.Add(instructor);
        db.SaveChanges();

        var controller = CreateController(db);
        var request = new MatchMakerDto
        {
            StudentFighterIds = new List<int> { 999, 1000 }, // non-existent IDs
            InstructorFighterId = 1
        };

        // Act
        var result = await controller.GeneratePairs(request);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("No valid students", bad.Value!.ToString());
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_InstructorNotFound()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var student = TestFixtures.CreateFighter(1, "Student A", role: FighterRole.Student);
        db.Fighters.Add(student);
        db.SaveChanges();

        var controller = CreateController(db);
        var request = new MatchMakerDto
        {
            StudentFighterIds = new List<int> { 1 },
            InstructorFighterId = 999 // non-existent instructor
        };

        // Act
        var result = await controller.GeneratePairs(request);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("No valid instructor", bad.Value!.ToString());
    }

    [Fact]
    public async Task Should_ReturnOkWithPairs_When_HowManyUniquePairsIsSpecified()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();

        var instructor = TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor);
        var student1 = TestFixtures.CreateFighter(2, "Alice", role: FighterRole.Student);
        var student2 = TestFixtures.CreateFighter(3, "Bob", role: FighterRole.Student);
        var student3 = TestFixtures.CreateFighter(4, "Charlie", role: FighterRole.Student);

        db.Fighters.AddRange(instructor, student1, student2, student3);
        db.SaveChanges();

        var controller = CreateController(db);
        var request = new MatchMakerDto
        {
            StudentFighterIds = new List<int> { 2, 3, 4 },
            InstructorFighterId = 1,
            HowManyUniquePairs = 2
        };

        // Act
        var result = await controller.GeneratePairs(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Should_ReturnOkWithSinglePair_When_OnlyOneStudentAndInstructorAvailable()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();

        var instructor = TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor,
            weight: 80, height: 180);
        var student = TestFixtures.CreateFighter(2, "Lone Student", role: FighterRole.Student,
            weight: 70, height: 175);

        db.Fighters.AddRange(instructor, student);
        db.SaveChanges();

        var controller = CreateController(db);
        var request = new MatchMakerDto
        {
            StudentFighterIds = new List<int> { 2 },
            InstructorFighterId = 1
        };

        // Act
        var result = await controller.GeneratePairs(request);

        // Assert â€” with one student, PairMatchingService should still return OK
        Assert.IsType<OkObjectResult>(result);
    }
}

