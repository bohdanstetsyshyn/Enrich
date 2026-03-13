using System;
using System.Collections.Generic;
using Enrich.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Enrich.DAL.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bundle> Bundles { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CommunityImport> CommunityImports { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SessionResult> SessionResults { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<TrainingSession> TrainingSessions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Word> Words { get; set; }

    public virtual DbSet<WordProgress> WordProgresses { get; set; }

   

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bundle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Bundle_pkey");

            entity.ToTable("Bundle");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsSystem)
                .HasDefaultValue(false)
                .HasColumnName("is_system");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");

            entity.HasOne(d => d.Admin).WithMany(p => p.BundleAdmins)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("fk_bundle_admin");

            entity.HasOne(d => d.Owner).WithMany(p => p.BundleOwners)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bundle_owner");

            entity.HasMany(d => d.Words).WithMany(p => p.Bundles)
                .UsingEntity<Dictionary<string, object>>(
                    "BundleWord",
                    r => r.HasOne<Word>().WithMany()
                        .HasForeignKey("WordId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_bundleword_word"),
                    l => l.HasOne<Bundle>().WithMany()
                        .HasForeignKey("BundleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_bundleword_bundle"),
                    j =>
                    {
                        j.HasKey("BundleId", "WordId").HasName("BundleWord_pkey");
                        j.ToTable("BundleWord");
                        j.IndexerProperty<int>("BundleId").HasColumnName("bundle_id");
                        j.IndexerProperty<int>("WordId").HasColumnName("word_id");
                    });
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Category_pkey");

            entity.ToTable("Category");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<CommunityImport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CommunityImport_pkey");

            entity.ToTable("CommunityImport");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BundleId).HasColumnName("bundle_id");
            entity.Property(e => e.ImportedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("imported_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WordId).HasColumnName("word_id");

            entity.HasOne(d => d.Bundle).WithMany(p => p.CommunityImports)
                .HasForeignKey(d => d.BundleId)
                .HasConstraintName("fk_communityimport_bundle");

            entity.HasOne(d => d.User).WithMany(p => p.CommunityImports)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_communityimport_user");

            entity.HasOne(d => d.Word).WithMany(p => p.CommunityImports)
                .HasForeignKey(d => d.WordId)
                .HasConstraintName("fk_communityimport_word");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Role_pkey");

            entity.ToTable("Role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(20)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<SessionResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SessionResult_pkey");

            entity.ToTable("SessionResult");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.WordId).HasColumnName("word_id");

            entity.HasOne(d => d.Session).WithMany(p => p.SessionResults)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sessionresult_session");

            entity.HasOne(d => d.Word).WithMany(p => p.SessionResults)
                .HasForeignKey(d => d.WordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sessionresult_word");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Tag_pkey");

            entity.ToTable("Tag");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TrainingSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TrainingSession_pkey");

            entity.ToTable("TrainingSession");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BundleId).HasColumnName("bundle_id");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.StartTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.TrainingType)
                .HasMaxLength(30)
                .HasColumnName("training_type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Bundle).WithMany(p => p.TrainingSessions)
                .HasForeignKey(d => d.BundleId)
                .HasConstraintName("fk_trainingsession_bundle");

            entity.HasOne(d => d.User).WithMany(p => p.TrainingSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_trainingsession_user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("User_pkey");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "User_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.ThemePreference)
                .HasMaxLength(10)
                .HasColumnName("theme_preference");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_role");
        });

        modelBuilder.Entity<Word>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Word_pkey");

            entity.ToTable("Word");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatorId).HasColumnName("creator_id");
            entity.Property(e => e.Definition).HasColumnName("definition");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(5)
                .HasColumnName("difficulty_level");
            entity.Property(e => e.IsGlobal)
                .HasDefaultValue(false)
                .HasColumnName("is_global");
            entity.Property(e => e.Term)
                .HasMaxLength(100)
                .HasColumnName("term");
            entity.Property(e => e.Translation)
                .HasMaxLength(100)
                .HasColumnName("translation");

            entity.HasOne(d => d.Creator).WithMany(p => p.Words)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("fk_word_creator");

            entity.HasMany(d => d.Categories).WithMany(p => p.Words)
                .UsingEntity<Dictionary<string, object>>(
                    "WordCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_wordcategory_category"),
                    l => l.HasOne<Word>().WithMany()
                        .HasForeignKey("WordId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_wordcategory_word"),
                    j =>
                    {
                        j.HasKey("WordId", "CategoryId").HasName("WordCategory_pkey");
                        j.ToTable("WordCategory");
                        j.IndexerProperty<int>("WordId").HasColumnName("word_id");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("category_id");
                    });

            entity.HasMany(d => d.Tags).WithMany(p => p.Words)
                .UsingEntity<Dictionary<string, object>>(
                    "WordTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_wordtag_tag"),
                    l => l.HasOne<Word>().WithMany()
                        .HasForeignKey("WordId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_wordtag_word"),
                    j =>
                    {
                        j.HasKey("WordId", "TagId").HasName("WordTag_pkey");
                        j.ToTable("WordTag");
                        j.IndexerProperty<int>("WordId").HasColumnName("word_id");
                        j.IndexerProperty<int>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<WordProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("WordProgress_pkey");

            entity.ToTable("WordProgress");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LastReviewed)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_reviewed");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.SuccessRate).HasColumnName("success_rate");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WordId).HasColumnName("word_id");

            entity.HasOne(d => d.User).WithMany(p => p.WordProgresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wordprogress_user");

            entity.HasOne(d => d.Word).WithMany(p => p.WordProgresses)
                .HasForeignKey(d => d.WordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wordprogress_word");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
