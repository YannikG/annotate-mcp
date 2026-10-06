using Annotate.Plans.Application;
using Annotate.Plans.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Plans.Infrastructure;

internal static class PlansModel
{
    private const int IdLength = 36;

    private const int FolderPathLength = 4096;

    private const int DisplayNameLength = 255;

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Id).HasColumnName("id").HasMaxLength(IdLength);
            entity.Property(project => project.FolderPath).HasColumnName("folder_path").HasMaxLength(FolderPathLength);
            entity.Property(project => project.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(DisplayNameLength)
                .IsRequired();
            entity.Property(project => project.CreatedAt).HasColumnName("created_at");
            entity.Property(project => project.ArchivedAt).HasColumnName("archived_at");
            entity.HasIndex(project => project.FolderPath)
                .IsUnique()
                .HasFilter("archived_at IS NULL");
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("plans");
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Id).HasColumnName("id").HasMaxLength(IdLength);
            entity.Property(plan => plan.ProjectId).HasColumnName("project_id").HasMaxLength(IdLength);
            entity.Property(plan => plan.Title).HasColumnName("title").HasMaxLength(PlanTitle.MaxLength).IsRequired();
            entity.Property(plan => plan.SessionId).HasColumnName("session_id").HasMaxLength(RevisionLimits.MaxSessionId);
            entity.Property(plan => plan.CreatedAt).HasColumnName("created_at");
            entity.Property(plan => plan.UpdatedAt).HasColumnName("updated_at");
            entity.Property(plan => plan.ArchivedAt).HasColumnName("archived_at");
            entity.HasOne<Project>()
                .WithMany()
                .HasForeignKey(plan => plan.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Revision>(entity =>
        {
            entity.ToTable("revisions");
            entity.HasKey(revision => revision.Id);
            entity.Property(revision => revision.Id).HasColumnName("id").HasMaxLength(IdLength);
            entity.Property(revision => revision.PlanId).HasColumnName("plan_id").HasMaxLength(IdLength);
            entity.Property(revision => revision.Number).HasColumnName("number");
            entity.Property(revision => revision.ParentRevisionId)
                .HasColumnName("parent_revision_id")
                .HasMaxLength(IdLength);
            entity.Property(revision => revision.Markdown)
                .HasColumnName("markdown")
                .HasMaxLength(RevisionLimits.MaxMarkdown)
                .IsRequired();
            entity.Property(revision => revision.Summary).HasColumnName("summary").HasMaxLength(RevisionLimits.MaxSummary);
            entity.Property(revision => revision.StoryUrl).HasColumnName("story_url").HasMaxLength(StoryLink.MaxLength);
            entity.Property(revision => revision.AcceptanceCriteria)
                .HasColumnName("acceptance_criteria")
                .HasMaxLength(RevisionLimits.MaxAcceptanceCriteria);
            entity.Property(revision => revision.Agent)
                .HasColumnName("agent")
                .HasMaxLength(AttributionRules.MaxLength);
            entity.Property(revision => revision.Model)
                .HasColumnName("model")
                .HasMaxLength(AttributionRules.MaxLength);
            entity.Property(revision => revision.ClientName)
                .HasColumnName("client_name")
                .HasMaxLength(AttributionRules.MaxLength);
            entity.Property(revision => revision.ClientVersion)
                .HasColumnName("client_version")
                .HasMaxLength(AttributionRules.MaxLength);
            entity.Property(revision => revision.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(revision => new { revision.PlanId, revision.Number }).IsUnique();
            entity.HasOne<Plan>()
                .WithMany()
                .HasForeignKey(revision => revision.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Revision>()
                .WithMany()
                .HasForeignKey(revision => revision.ParentRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoredBlock>(entity =>
        {
            entity.ToTable("blocks");
            entity.HasKey(block => new { block.RevisionId, block.Ordinal });
            entity.Property(block => block.RevisionId).HasColumnName("revision_id").HasMaxLength(IdLength);
            entity.Property(block => block.Ordinal).HasColumnName("ordinal").ValueGeneratedNever();
            entity.Property(block => block.BlockKey).HasColumnName("block_key").HasMaxLength(IdLength).IsRequired();
            entity.Property(block => block.Kind).HasColumnName("kind").HasMaxLength(BlockKeys.KindLength).IsRequired();
            entity.Property(block => block.SectionPath)
                .HasColumnName("section_path")
                .HasMaxLength(BlockKeys.SectionPathLength)
                .IsRequired();
            entity.Property(block => block.SourceStart).HasColumnName("source_start");
            entity.Property(block => block.SourceEnd).HasColumnName("source_end");
            entity.Property(block => block.ContentHash)
                .HasColumnName("content_hash")
                .HasMaxLength(BlockKeys.HashLength)
                .IsRequired();
            entity.HasOne<Revision>()
                .WithMany()
                .HasForeignKey(block => block.RevisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}