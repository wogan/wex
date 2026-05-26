using Wex.Model;

namespace Wex;

using Microsoft.EntityFrameworkCore;

public class Database(DbContextOptions<Database> options) : DbContext(options)
{
    public DbSet<Card> Cards { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>()
            .HasOne(transaction => transaction.Card)
            .WithMany()
            .HasForeignKey(transaction => transaction.CardId)
            .IsRequired();
    }
}