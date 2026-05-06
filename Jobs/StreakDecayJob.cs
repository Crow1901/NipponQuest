using Quartz;
using NipponQuest.Data;
using Microsoft.EntityFrameworkCore;

namespace NipponQuest.Jobs
{
    [DisallowConcurrentExecution]
    public class StreakDecayJob : IJob
    {
        private readonly ApplicationDbContext _context;

        public StreakDecayJob(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            DateTime today = DateTime.UtcNow.Date;

            // Anyone who did not log in yesterday or today gets their flame snuffed.
            var stale = await _context.Users
                .Where(u => u.LoginStreak > 0
                            && (u.LastLoginDate == null
                                || u.LastLoginDate.Value < today.AddDays(-1)))
                .ToListAsync();

            foreach (var u in stale)
            {
                u.LoginStreak = 0;
            }

            if (stale.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}