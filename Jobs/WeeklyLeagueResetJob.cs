using Quartz;
using NipponQuest.Models;
using NipponQuest.Data;
using Microsoft.EntityFrameworkCore;

namespace NipponQuest.Jobs
{
    [DisallowConcurrentExecution]
    public class WeeklyLeagueResetJob : IJob
    {
        private readonly ApplicationDbContext _context;

        public WeeklyLeagueResetJob(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("---> QUARTZ JOB RUNNING: " + DateTime.Now);

            var allUsers = await _context.Users.ToListAsync();

            foreach (var user in allUsers)
            {
                int rewardAmount = GetWeeklyGold(user.CurrentLeague);
                user.Gold += rewardAmount;
                user.WeeklyXP = 0;
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("---> QUARTZ JOB SUCCESSFUL");
        }

        // Tier reward table:
        // Sprout 150, +100 per league through Master,
        // then Challenger / Dragon / Legend each gain an extra +100 on top of the base step.
        private int GetWeeklyGold(LeagueRank rank) => rank switch
        {
            LeagueRank.Sprout => 150,
            LeagueRank.Wood => 250,
            LeagueRank.Iron => 350,
            LeagueRank.Gold => 450,
            LeagueRank.Diamond => 550,
            LeagueRank.Master => 650,
            LeagueRank.Challenger => 850,
            LeagueRank.Dragon => 1050,
            LeagueRank.Legend => 1250,
            _ => 150
        };
    }
}