using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Annotate.Plans.Infrastructure;

internal sealed class PlansDesignTimeFactory : IDesignTimeDbContextFactory<PlansDbContext>
{
    public PlansDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PlansDbContext> options = new();
        PlansDatabase.Configure(options, "Data Source=:memory:");
        return new PlansDbContext(options.Options);
    }
}