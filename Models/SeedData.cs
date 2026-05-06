using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NipponQuest.Data;
using System;
using System.Linq;

namespace NipponQuest.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Ensure the database is actually reachable before seeding
                if (!context.Database.CanConnect())
                {
                    return;
                }

                // Look for any Hiragana already in the database
                if (context.Hiraganas.Any())
                {
                    return;
                }

                context.Hiraganas.AddRange(
                    new Hiragana { Symbol = "あ", Romaji = "a", UnlockLevel = 1 },
                    new Hiragana { Symbol = "い", Romaji = "i", UnlockLevel = 1 },
                    new Hiragana { Symbol = "う", Romaji = "u", UnlockLevel = 1 },
                    new Hiragana { Symbol = "え", Romaji = "e", UnlockLevel = 1 },
                    new Hiragana { Symbol = "お", Romaji = "o", UnlockLevel = 1 }
                );

                context.SaveChanges();
            }
        }
    }
}