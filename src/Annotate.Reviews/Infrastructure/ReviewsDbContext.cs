using Annotate.Reviews.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Reviews.Infrastructure;

internal sealed class ReviewsDbContext(DbContextOptions<ReviewsDbContext> options) : DbContext(options)
{
    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<ReviewFence> Fences => Set<ReviewFence>();

    public DbSet<ReviewOption> Options => Set<ReviewOption>();

    public DbSet<StoredAnswer> Answers => Set<StoredAnswer>();

    public DbSet<StoredAnnotation> Annotations => Set<StoredAnnotation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => ReviewsModel.Configure(modelBuilder);
}