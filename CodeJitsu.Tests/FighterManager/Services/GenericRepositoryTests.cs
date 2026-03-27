using FighterManager.Server.Helpers;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using CodeJitsu.Tests.Helpers;

namespace CodeJitsu.Tests.FighterManager.Services;

public class GenericRepositoryTests
{
    private static MyDatabaseContext CreateContext() => InMemoryDbContextFactory.Create();

    [Fact]
    public async Task Should_AddAndRetrieveEntity_When_EntityAddedAsync()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new Repository<Fighter>(context);
        var fighter = TestFixtures.CreateFighter(id: 0, fighterName: "Repo Test Fighter");

        // Act
        await repo.AddAsync(fighter);
        await context.SaveChangesAsync();
        var retrieved = await repo.GetByIdAsync(fighter.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Repo Test Fighter", retrieved.FighterName);
    }

    [Fact]
    public async Task Should_ReturnAllEntities_When_GetAllAsyncCalled()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new Repository<Fighter>(context);

        var f1 = TestFixtures.CreateFighter(id: 0, fighterName: "Fighter One");
        var f2 = TestFixtures.CreateFighter(id: 0, fighterName: "Fighter Two");
        await repo.AddAsync(f1);
        await repo.AddAsync(f2);
        await context.SaveChangesAsync();

        // Act
        var all = (await repo.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Should_UpdateEntity_When_UpdateCalled()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new Repository<Fighter>(context);
        var fighter = TestFixtures.CreateFighter(id: 0, fighterName: "Original Name");
        await repo.AddAsync(fighter);
        await context.SaveChangesAsync();

        // Act
        fighter.FighterName = "Updated Name";
        repo.Update(fighter);
        await context.SaveChangesAsync();

        var updated = await repo.GetByIdAsync(fighter.Id);

        // Assert
        Assert.Equal("Updated Name", updated!.FighterName);
    }

    [Fact]
    public async Task Should_DeleteEntity_When_DeleteCalled()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new Repository<Fighter>(context);
        var fighter = TestFixtures.CreateFighter(id: 0, fighterName: "To Be Deleted");
        await repo.AddAsync(fighter);
        await context.SaveChangesAsync();
        var savedId = fighter.Id;

        // Act
        repo.Delete(fighter);
        await context.SaveChangesAsync();

        var deleted = await repo.GetByIdAsync(savedId);

        // Assert
        Assert.Null(deleted);
    }
}

