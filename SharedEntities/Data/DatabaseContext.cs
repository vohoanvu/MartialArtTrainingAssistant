using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Models;

namespace SharedEntities.Data;

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

    public virtual DbSet<TrainingSessionTechniqueJoint> TrainingSessionTechniqueJoints { get; set; }

    public virtual DbSet<UploadedVideo> UploadedVideos { get; set; }

    public virtual DbSet<AiFeedback> AiFeedbacks { get; set; }

    public virtual DbSet<AiAnalysisResult> AiAnalysisResults { get; set; }

    public virtual DbSet<Techniques> Techniques { get; set; }
    public virtual DbSet<PointScoringTechnique> PointScoringTechniques { get; set; }

    public virtual DbSet<Demonstration> Demonstrations { get; set; }

    public virtual DbSet<Drills> Drills { get; set; }

    public virtual DbSet<TechniqueType> TechniqueTypes { get; set; }

    public virtual DbSet<PositionalScenario> PositionalScenarios { get; set; }

    public virtual DbSet<Curriculum> Curriculums { get; set; }

    public virtual DbSet<CurriculumScenario> CurriculumScenarios { get; set; }

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

        // Configure GIN index for AiAnalysisResult.AnalysisJson (JSONB)
        builder.Entity<AiAnalysisResult>()
            .HasIndex(a => a.AnalysisJson)
            .HasMethod("GIN");

        // Configure GIN index for AiFeedback.AnalysisJson (JSONB)
        builder.Entity<AiFeedback>()
            .HasIndex(a => a.AnalysisJson)
            .HasMethod("GIN");

        // Ensure JSONB columns are mapped correctly
        builder.Entity<AiAnalysisResult>()
            .Property(a => a.AnalysisJson)
            .HasColumnType("jsonb");

        builder.Entity<AiFeedback>()
            .Property(a => a.AnalysisJson)
            .HasColumnType("jsonb");
    }
}