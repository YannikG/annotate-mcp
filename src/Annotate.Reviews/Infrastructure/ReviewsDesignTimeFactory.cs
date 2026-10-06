using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Annotate.Reviews.Infrastructure;

internal sealed class ReviewsDesignTimeFactory : IDesignTimeDbContextFactory<ReviewsDbContext>
{
    public ReviewsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ReviewsDbContext> options = new();
        ReviewsDatabase.Configure(options, "Data Source=:memory:");
        return new ReviewsDbContext(options.Options);
    }
}