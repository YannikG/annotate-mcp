using Annotate.Plans.Domain;

using Microsoft.EntityFrameworkCore;

namespace Annotate.Plans.Infrastructure;

internal sealed class PlansDbContext(DbContextOptions<PlansDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Revision> Revisions => Set<Revision>();

    public DbSet<StoredBlock> Blocks => Set<StoredBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => PlansModel.Configure(modelBuilder);
}