using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;

namespace CodeJitsu.Tests.Helpers;

/// <summary>
/// Factory for creating <see cref="MyDatabaseContext"/> instances backed by the EF Core
/// InMemory provider. Each call with no explicit name gets a unique database so tests
/// remain fully isolated. Passing the same name across calls shares state within a test.
/// </summary>
public static class InMemoryDbContextFactory
{
    /// <summary>
    /// Creates a new <see cref="MyDatabaseContext"/> that uses the InMemory provider.
    /// </summary>
    /// <param name="dbName">
    /// Optional database name. Defaults to a new <see cref="Guid"/> string so every
    /// call is isolated unless the caller deliberately shares a name.
    /// </param>
    public static MyDatabaseContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<MyDatabaseContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        // Passing options to the parameterised constructor prevents OnConfiguring from
        // running (EF Core skips it when the provider is already set), which avoids the
        // Global.AccessAppEnvironmentVariable call that requires a real connection string.
        var context = new MyDatabaseContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}

