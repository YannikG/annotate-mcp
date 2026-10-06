using Annotate.Reviews.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Reviews.Infrastructure;

internal static class ReviewsModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(review => review.Id);
            entity.Property(review => review.Id).HasColumnName("id").HasMaxLength(ReviewLimits.Id);
            entity.Property(review => review.RevisionId)
                .HasColumnName("revision_id")
                .HasMaxLength(ReviewLimits.Id)
                .IsRequired();
            entity.Property(review => review.Status).HasColumnName("status").HasMaxLength(ReviewLimits.Status).IsRequired();
            entity.Property(review => review.Feedback).HasColumnName("feedback").HasMaxLength(ReviewLimits.Feedback);
            entity.Property(review => review.CreatedAt).HasColumnName("created_at");
            entity.Property(review => review.DecidedAt).HasColumnName("decided_at");
            entity.HasIndex(review => review.RevisionId).IsUnique();
        });

        modelBuilder.Entity<ReviewFence>(entity =>
        {
            entity.ToTable("review_decisions");
            entity.HasKey(fence => new { fence.ReviewId, fence.Ordinal });
            entity.Property(fence => fence.ReviewId).HasColumnName("review_id").HasMaxLength(ReviewLimits.Id);
            entity.Property(fence => fence.Ordinal).HasColumnName("ordinal").ValueGeneratedNever();
            entity.Property(fence => fence.FenceId).HasColumnName("fence_id").HasMaxLength(ReviewLimits.FenceId).IsRequired();
            entity.Property(fence => fence.Kind).HasColumnName("kind").HasMaxLength(ReviewLimits.DecisionKind).IsRequired();
            entity.Property(fence => fence.Prompt).HasColumnName("prompt").HasMaxLength(ReviewLimits.Prompt).IsRequired();
            entity.HasIndex(fence => new { fence.ReviewId, fence.FenceId }).IsUnique();
            entity.HasOne<Review>()
                .WithMany()
                .HasForeignKey(fence => fence.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReviewOption>(entity =>
        {
            entity.ToTable("review_options");
            entity.HasKey(option => new { option.ReviewId, option.FenceId, option.Ordinal });
            entity.Property(option => option.ReviewId).HasColumnName("review_id").HasMaxLength(ReviewLimits.Id);
            entity.Property(option => option.FenceId).HasColumnName("fence_id").HasMaxLength(ReviewLimits.FenceId);
            entity.Property(option => option.Ordinal).HasColumnName("ordinal").ValueGeneratedNever();
            entity.Property(option => option.Label).HasColumnName("label").HasMaxLength(ReviewLimits.Label).IsRequired();
            entity.HasOne<Review>()
                .WithMany()
                .HasForeignKey(option => option.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredAnswer>(entity =>
        {
            entity.ToTable("decision_answers");
            entity.HasKey(answer => new { answer.ReviewId, answer.FenceId });
            entity.Property(answer => answer.ReviewId).HasColumnName("review_id").HasMaxLength(ReviewLimits.Id);
            entity.Property(answer => answer.FenceId).HasColumnName("fence_id").HasMaxLength(ReviewLimits.FenceId);
            entity.Property(answer => answer.Answer).HasColumnName("answer").HasMaxLength(ReviewLimits.Answer).IsRequired();
            entity.Property(answer => answer.IsOther).HasColumnName("is_other");
            entity.HasOne<Review>()
                .WithMany()
                .HasForeignKey(answer => answer.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredAnnotation>(entity =>
        {
            entity.ToTable("annotations");
            entity.HasKey(annotation => new { annotation.ReviewId, annotation.Ordinal });
            entity.Property(annotation => annotation.ReviewId).HasColumnName("review_id").HasMaxLength(ReviewLimits.Id);
            entity.Property(annotation => annotation.Ordinal).HasColumnName("ordinal").ValueGeneratedNever();
            entity.Property(annotation => annotation.AnnotationId)
                .HasColumnName("annotation_id")
                .HasMaxLength(ReviewLimits.AnnotationId)
                .IsRequired();
            entity.Property(annotation => annotation.Kind)
                .HasColumnName("kind")
                .HasMaxLength(ReviewLimits.AnnotationKind)
                .IsRequired();
            entity.Property(annotation => annotation.BlockOrdinal).HasColumnName("block_ordinal");
            entity.Property(annotation => annotation.StartOffset).HasColumnName("start_offset");
            entity.Property(annotation => annotation.EndOffset).HasColumnName("end_offset");
            entity.Property(annotation => annotation.Text).HasColumnName("text").HasMaxLength(ReviewLimits.Text).IsRequired();
            entity.Property(annotation => annotation.Replacement).HasColumnName("replacement").HasMaxLength(ReviewLimits.Note);
            entity.Property(annotation => annotation.Comment).HasColumnName("comment").HasMaxLength(ReviewLimits.Note);
            entity.Property(annotation => annotation.CreatedAt)
                .HasColumnName("created_at")
                .HasMaxLength(ReviewLimits.CreatedAt)
                .IsRequired();
            entity.HasOne<Review>()
                .WithMany()
                .HasForeignKey(annotation => annotation.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}