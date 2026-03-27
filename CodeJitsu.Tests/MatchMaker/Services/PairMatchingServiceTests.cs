using MatchMaker.Server.Domain.PairMatchingService;
using SharedEntities.Models;
using CodeJitsu.Tests.Helpers;

namespace CodeJitsu.Tests.MatchMaker.Services;

public class PairMatchingServiceTests
{
    private static Fighter CreateInstructor() => TestFixtures.CreateFighter(
        id: 100,
        fighterName: "Instructor",
        role: FighterRole.Instructor,
        beltColor: BeltColor.Black,
        weight: 80,
        bmi: 25,
        maxWorkoutDuration: 120);

    // ---- GenerateNonUniquePairs ----

    [Fact]
    public void Should_ReturnOnePair_When_TwoFightersProvided()
    {
        // Arrange
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 2, weight: 72, bmi: 23, maxWorkoutDuration: 60),
        };
        var service = new PairMatchingService(fighters, CreateInstructor());

        // Act
        var pairs = service.GenerateNonUniquePairs();

        // Assert
        Assert.Single(pairs);
        Assert.Equal(1, pairs[0].Item1.Id);
        Assert.Equal(2, pairs[0].Item2.Id);
    }

    [Fact]
    public void Should_ReturnEmptyList_When_EmptyFighterList()
    {
        // Arrange
        var fighters = new List<Fighter>();
        var service = new PairMatchingService(fighters, CreateInstructor());

        // Act
        var pairs = service.GenerateNonUniquePairs();

        // Assert
        Assert.Empty(pairs);
    }

    [Fact]
    public void Should_IncludeInstructorPair_When_OddNumberOfFighters()
    {
        // Arrange
        var instructor = CreateInstructor();
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 2, weight: 72, bmi: 23, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 3, weight: 90, bmi: 30, maxWorkoutDuration: 90),
        };
        var service = new PairMatchingService(fighters, instructor);

        // Act
        var pairs = service.GenerateNonUniquePairs();

        // Assert
        Assert.Equal(2, pairs.Count);
        // Instructor is in the first pair
        Assert.Equal(instructor.Id, pairs[0].Item1.Id);
    }

    // ---- GenerateNextFighterPairs ----

    [Fact]
    public void Should_ReturnTwoPairs_When_FourFightersProvided()
    {
        // Arrange
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 2, weight: 72, bmi: 23, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 3, weight: 90, bmi: 30, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 4, weight: 88, bmi: 29, maxWorkoutDuration: 60),
        };
        var service = new PairMatchingService(fighters, CreateInstructor());

        // Act
        var pairs = service.GenerateNextFighterPairs();

        // Assert
        Assert.Equal(2, pairs.Count);
    }

    [Fact]
    public void Should_AllFightersAppearExactlyOnce_When_EvenNumberOfFighters()
    {
        // Arrange
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 2, weight: 72, bmi: 23, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 3, weight: 85, bmi: 27, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 4, weight: 87, bmi: 28, maxWorkoutDuration: 60),
        };
        var service = new PairMatchingService(fighters, CreateInstructor());

        // Act
        var pairs = service.GenerateNextFighterPairs();

        // Assert
        var allIds = pairs.SelectMany(p => new[] { p.Item1.Id, p.Item2.Id }).ToList();
        Assert.Equal(4, allIds.Count);
        Assert.Equal(4, allIds.Distinct().Count());
    }

    // ---- FindNextMatchingPair with history ----

    [Fact]
    public void Should_ReturnNull_When_AllPairsAlreadyInHistory()
    {
        // Arrange
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 2, weight: 72, bmi: 23, maxWorkoutDuration: 60),
        };
        var service = new PairMatchingService(fighters, CreateInstructor());

        // Record the only possible pair in history
        service.FindNextMatchingPair(new List<Fighter>(fighters));

        // Act - second call should return null as the only pair is in history
        var result = service.FindNextMatchingPair(new List<Fighter>(fighters));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Should_PairClosestMatchFirst_When_MultipleCandidatesPresent()
    {
        // Arrange
        // f1 vs f2 diff = 1+0.5+0 = 1.5
        // f1 vs f3 diff = 20+8+30 = 58
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 2, weight: 71, bmi: 22.5, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 3, weight: 90, bmi: 30, maxWorkoutDuration: 90),
        };
        var service = new PairMatchingService(fighters, CreateInstructor());

        // Act
        var pair = service.FindNextMatchingPair(new List<Fighter>(fighters));

        // Assert
        var ids = new[] { pair.Item1.Id, pair.Item2.Id };
        Assert.Contains(1, ids);
        Assert.Contains(2, ids);
    }
}

