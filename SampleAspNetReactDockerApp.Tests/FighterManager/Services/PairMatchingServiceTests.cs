using FighterManager.Server.Domain.PairMatchingService;
using SharedEntities.Models;
using SampleAspNetReactDockerApp.Tests.Helpers;

namespace SampleAspNetReactDockerApp.Tests.FighterManager.Services;

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

    // ---- GenerateNextFighterPairs ----

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
        var pairs = service.GenerateNextFighterPairs();

        // Assert
        Assert.Single(pairs);
        Assert.Equal(1, pairs[0].Item1.Id);
        Assert.Equal(2, pairs[0].Item2.Id);
    }

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
    public void Should_IncludeInstructorPair_When_OddNumberOfFighters()
    {
        // Arrange
        var instructor = CreateInstructor();
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 2, weight: 72, bmi: 23, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 3, weight: 90, bmi: 30, maxWorkoutDuration: 90, beltColor: BeltColor.Blue),
        };
        var service = new PairMatchingService(fighters, instructor);

        // Act
        var pairs = service.GenerateNextFighterPairs();

        // Assert
        Assert.Equal(2, pairs.Count);
        // Instructor should be in the first pair (paired with highest-ranked fighter)
        Assert.Equal(instructor.Id, pairs[0].Item1.Id);
    }

    [Fact]
    public void Should_ReturnEmptyList_When_EmptyFighterList()
    {
        // Arrange
        var fighters = new List<Fighter>();
        var service = new PairMatchingService(fighters, CreateInstructor());

        // Act
        var pairs = service.GenerateNextFighterPairs();

        // Assert
        Assert.Empty(pairs);
    }

    [Fact]
    public void Should_PairInstructorWithHighestRankedFighter_When_OddFighters()
    {
        // Arrange
        var instructor = CreateInstructor();
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60, beltColor: BeltColor.White),
            TestFixtures.CreateFighter(id: 2, weight: 72, bmi: 23, maxWorkoutDuration: 90, beltColor: BeltColor.White),
            TestFixtures.CreateFighter(id: 3, weight: 75, bmi: 24, maxWorkoutDuration: 90, beltColor: BeltColor.White),
        };
        var service = new PairMatchingService(fighters, instructor);

        // Act
        var pairs = service.GenerateNextFighterPairs();

        // Assert
        // Instructor paired with highest workout duration (id 2 or 3 - both white belt, 90 min, pick higher BMI = id 3)
        Assert.Equal(instructor.Id, pairs[0].Item1.Id);
        Assert.Equal(3, pairs[0].Item2.Id);
    }

    [Fact]
    public void Should_ReturnInstructorPairOnly_When_SingleFighter()
    {
        // Arrange
        var instructor = CreateInstructor();
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
        };
        var service = new PairMatchingService(fighters, instructor);

        // Act
        var pairs = service.GenerateNextFighterPairs();

        // Assert
        Assert.Single(pairs);
        Assert.Equal(instructor.Id, pairs[0].Item1.Id);
        Assert.Equal(1, pairs[0].Item2.Id);
    }

    // ---- FindNextMatchingPair ----

    [Fact]
    public void Should_SelectClosestMatchByDifference_When_MultipleCandidates()
    {
        // Arrange
        // Fighter 1 vs 2: diff = |70-71| + |22-22.5| + |60-60| = 1.5
        // Fighter 1 vs 3: diff = |70-90| + |22-30| + |60-90| = 58
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

    [Fact]
    public void Should_ReturnNull_When_AllPairsInHistory()
    {
        // Arrange
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 2, weight: 72, bmi: 23, maxWorkoutDuration: 60),
        };
        var service = new PairMatchingService(fighters, CreateInstructor());

        // Exhaust the one possible pair by calling it once
        service.FindNextMatchingPair(new List<Fighter>(fighters));

        // Act - second call should return null since the pair is in history
        var result = service.FindNextMatchingPair(new List<Fighter>(fighters));

        // Assert
        Assert.Null(result);
    }

    // ---- ComputeDifference (tested indirectly via pairing) ----

    [Fact]
    public void Should_PairFightersWithSmallestCombinedDifference_When_MatchingByStats()
    {
        // Arrange
        // f1(weight=70, bmi=22, duration=60) vs f2(weight=71, bmi=22, duration=60): diff = 1
        // f1 vs f3(weight=90, bmi=30, duration=90): diff = 48
        var f1 = TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60);
        var f2 = TestFixtures.CreateFighter(id: 2, weight: 71, bmi: 22, maxWorkoutDuration: 60);
        var f3 = TestFixtures.CreateFighter(id: 3, weight: 90, bmi: 30, maxWorkoutDuration: 90);
        var fighters = new List<Fighter> { f1, f2, f3 };
        var service = new PairMatchingService(fighters, CreateInstructor());

        // Act
        var pairs = service.GenerateNextFighterPairs();

        // f1+f2 should be one pair, f3 paired with instructor (odd count)
        var studentPair = pairs.FirstOrDefault(p => p.Item1.Id != 100 && p.Item2.Id != 100);
        Assert.NotNull(studentPair);
        var ids = new[] { studentPair.Item1.Id, studentPair.Item2.Id };
        Assert.Contains(1, ids);
        Assert.Contains(2, ids);
    }

    [Fact]
    public void Should_GenerateMultipleRounds_When_HowManyDifferentPairsSpecified()
    {
        // Arrange
        var fighters = new List<Fighter>
        {
            TestFixtures.CreateFighter(id: 1, weight: 70, bmi: 22, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 2, weight: 72, bmi: 23, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 3, weight: 74, bmi: 24, maxWorkoutDuration: 60),
            TestFixtures.CreateFighter(id: 4, weight: 76, bmi: 25, maxWorkoutDuration: 60),
        };
        var service = new PairMatchingService(fighters, CreateInstructor(), howManyDifferentPairs: 2);

        // Act
        var rounds = service.GenerateFighterPairs().Take(10).ToList();

        // Assert - should generate at least 1 round
        Assert.NotEmpty(rounds);
    }

    [Fact]
    public void Should_EachPairCoverAllFighters_When_EvenNumberOfFighters()
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

        // Assert - all 4 fighters should appear exactly once
        var allIds = pairs.SelectMany(p => new[] { p.Item1.Id, p.Item2.Id }).ToList();
        Assert.Equal(4, allIds.Count);
        Assert.Equal(4, allIds.Distinct().Count());
    }
}
