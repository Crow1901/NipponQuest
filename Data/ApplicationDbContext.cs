using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NipponQuest.Models;

namespace NipponQuest.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hiragana> Hiraganas { get; set; }
        public DbSet<Deck> Decks { get; set; }
        public DbSet<Flashcard> Flashcards { get; set; }
        public DbSet<DeckVote> DeckVotes { get; set; }
        public DbSet<DeckPurchase> DeckPurchases { get; set; }
        public DbSet<UserColorPurchase> UserColorPurchases { get; set; }
        public DbSet<KanaWord> KanaWords { get; set; }
        public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; }
        public DbSet<RewardLedger> RewardLedgers { get; set; } = default!;
        public DbSet<BlitzPersonalBest> BlitzPersonalBests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<BlitzPersonalBest>()
                .HasIndex(p => new { p.ApplicationUserId, p.Difficulty, p.Alphabet })
                .IsUnique();
        }
    }
}