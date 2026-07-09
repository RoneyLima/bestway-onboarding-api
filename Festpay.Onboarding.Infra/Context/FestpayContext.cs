using Festpay.Onboarding.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Festpay.Onboarding.Infra.Context;

public class FestpayContext(DbContextOptions<FestpayContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; init; }
    public DbSet<Transaction> Transactions { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FestpayContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.ConfigureWarnings(warnings =>
        {
            warnings.Ignore(CoreEventId.NavigationBaseIncludeIgnored);
        });

        // new FestpayContextFactory().CreateDbContext();
    }

    public static FestpayContext CreateDbContext() => new FestpayContextFactory().CreateDbContext();
}
