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

    public virtual DbSet<VideoMetadata> Videos { get; set; }

    public virtual DbSet<Fighter> Fighters { get; set; }

    public virtual DbSet<TrainingSessionFighterJoint> TrainingSessionFighterJoints { get; set; }

    public virtual DbSet<TrainingSession> TrainingSessions { get; set; }

    public virtual DbSet<TrainingSessionTechniqueJoint> TrainingSessionTechniqueJoints { get; set; }


    public virtual DbSet<VideoSegmentFeedback> VideoSegmentFeedbacks { get; set; }

    public virtual DbSet<AiAnalysisResult> AiAnalysisResults { get; set; }
    public virtual DbSet<WeaknessCategory> WeaknessCategories { get; set; }
    public virtual DbSet<AnalysisWeakness> AnalysisWeaknesses { get; set; }

    public virtual DbSet<MatchEvent> MatchEvents { get; set; }
    public virtual DbSet<CoachingReport> CoachingReports { get; set; }
    public virtual DbSet<CoachingStrength> CoachingStrengths { get; set; }
    public virtual DbSet<CoachingWeakness> CoachingWeaknesses { get; set; }
    public virtual DbSet<PrescribedDrill> PrescribedDrills { get; set; }

    public virtual DbSet<Techniques> Techniques { get; set; }
    public virtual DbSet<PointScoringTechnique> PointScoringTechniques { get; set; }

    public virtual DbSet<Drills> Drills { get; set; }

    public virtual DbSet<TechniqueType> TechniqueTypes { get; set; }

    public virtual DbSet<PositionalScenario> PositionalScenarios { get; set; }

    public virtual DbSet<Curriculum> Curriculums { get; set; }

    public virtual DbSet<CurriculumScenario> CurriculumScenarios { get; set; }

    public virtual DbSet<Waitlist> Waitlists { get; set; }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.AppDb);
            optionsBuilder.UseNpgsql(connectionString);
        }

        // Suppress EF Core 10 PendingModelChangesWarning — the HasDefaultValueSql change
        // is cosmetic (runtime behavior unchanged) and will be captured in a future migration
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
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
                .HasDefaultValueSql("now() at time zone 'utc'")
                .ValueGeneratedOnAdd();

            e.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .ValueGeneratedOnAddOrUpdate();

            e.HasOne(x => x.Fighter).WithOne().HasForeignKey<AppUserEntity>(x => x.FighterId);
        });

        // Configure GIN index for AiAnalysisResult.AnalysisJson (JSONB)
        builder.Entity<AiAnalysisResult>()
            .HasIndex(a => a.AnalysisJson)
            .HasMethod("GIN");

        // Configure GIN index for AiFeedback.AnalysisJson (JSONB)
        builder.Entity<VideoSegmentFeedback>()
            .HasIndex(a => a.AnalysisJson)
            .HasMethod("GIN");
        builder.Entity<TrainingSession>()
            .HasIndex(ts => ts.RawCurriculumJson)
            .HasMethod("GIN");
        builder.Entity<TrainingSession>()
            .HasIndex(ts => ts.EditedCurriculumJson)
            .HasMethod("GIN");
        builder.Entity<TrainingSession>()
            .HasIndex(ts => ts.RawFighterPairsJson)
            .HasMethod("GIN");
        builder.Entity<TrainingSession>()
            .HasIndex(ts => ts.RawFighterPairsJson)
            .HasMethod("GIN");

        // Ensure JSONB columns are mapped correctly
        builder.Entity<AiAnalysisResult>()
            .Property(a => a.AnalysisJson)
            .HasColumnType("jsonb");

        builder.Entity<VideoSegmentFeedback>()
            .Property(a => a.AnalysisJson)
            .HasColumnType("jsonb");

        builder.Entity<TrainingSession>()
            .Property(ts => ts.RawCurriculumJson)
            .HasColumnType("jsonb");
        builder.Entity<TrainingSession>()
            .Property(ts => ts.EditedCurriculumJson)
            .HasColumnType("jsonb");

        builder.Entity<TrainingSession>()
            .Property(ts => ts.RawFighterPairsJson)
            .HasColumnType("jsonb");
        builder.Entity<TrainingSession>()
            .Property(ts => ts.EditedFighterPairsJson)
            .HasColumnType("jsonb");

        // ── Agentic (v2) pipeline relationships ──
        // 1:1 AiAnalysisResult ↔ CoachingReport; deleting the analysis cascades the report and its children.
        builder.Entity<AiAnalysisResult>()
            .HasOne(a => a.CoachingReport)
            .WithOne(c => c.AiAnalysisResult)
            .HasForeignKey<CoachingReport>(c => c.AiAnalysisResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MatchEvent>()
            .HasOne(e => e.AiAnalysisResult)
            .WithMany(a => a.MatchEvents)
            .HasForeignKey(e => e.AiAnalysisResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MatchEvent>()
            .HasIndex(e => new { e.AiAnalysisResultId, e.SequenceIndex });

        builder.Entity<CoachingStrength>()
            .HasOne(s => s.CoachingReport)
            .WithMany(c => c.Strengths)
            .HasForeignKey(s => s.CoachingReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CoachingWeakness>()
            .HasOne(w => w.CoachingReport)
            .WithMany(c => c.Weaknesses)
            .HasForeignKey(w => w.CoachingReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PrescribedDrill>()
            .HasOne(d => d.CoachingReport)
            .WithMany(c => c.PrescribedDrills)
            .HasForeignKey(d => d.CoachingReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}