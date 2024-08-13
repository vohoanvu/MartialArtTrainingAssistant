using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VideoSharing.Server.Models;
using System.Reflection.Emit;

namespace VideoSharing.Server.Data;

/// <summary>
/// The database context for the application.
/// </summary>
public class MyDatabaseContext : IdentityDbContext<AppUserEntity>
{
    /// <summary>
    /// The constructor for the database context.
    /// </summary>
    /// <param name="options"></param>
    public MyDatabaseContext(DbContextOptions<MyDatabaseContext> options) : base(options)
    {
    }

    public MyDatabaseContext()
    {
    }

    public virtual DbSet<SharedVideo> SharedVideos { get; set; }

    public virtual DbSet<Fighter> Fighters { get; set; }

    public virtual DbSet<TrainingSessionFighterJoint> TrainingSessionFighterJoints { get; set; }

    public virtual DbSet<TrainingSession> TrainingSessions { get; set; }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.AppDb);
        optionsBuilder.UseNpgsql(connectionString);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUserEntity>(e =>
        {
            e.ToTable("app_users");
            e.HasKey(x => x.Id);
            
            e.Property(p => p.CreatedAt)
                .HasDefaultValue(DateTime.UtcNow)
                .ValueGeneratedOnAdd();
            
            e.Property(p => p.UpdatedAt)
                .HasDefaultValue(DateTime.UtcNow)
                .ValueGeneratedOnAddOrUpdate();

            e.HasOne(x => x.Fighter).WithOne().HasForeignKey<AppUserEntity>(x => x.FighterId);
        });
    }
}