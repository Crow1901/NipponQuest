using Microsoft.AspNetCore.Identity;
using NipponQuest.Models;

namespace NipponQuest.Middleware
{
    public class LoginStreakMiddleware
    {
        private readonly RequestDelegate _next;

        public LoginStreakMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);

                if (user != null)
                {
                    DateTime today = DateTime.UtcNow.Date;
                    DateTime? last = user.LastLoginDate?.Date;
                    bool changed = false;

                    if (last == null)
                    {
                        // First ever recorded login.
                        user.LoginStreak = 1;
                        user.LastLoginDate = today;
                        changed = true;
                    }
                    else if (last.Value == today)
                    {
                        // Already counted today. If somehow streak is zero, light the flame.
                        if (user.LoginStreak <= 0)
                        {
                            user.LoginStreak = 1;
                            changed = true;
                        }
                    }
                    else if (last.Value == today.AddDays(-1))
                    {
                        // Logged in yesterday, now today => streak continues.
                        user.LoginStreak = System.Math.Max(1, user.LoginStreak) + 1;
                        user.LastLoginDate = today;
                        changed = true;
                    }
                    else
                    {
                        // Missed at least one full day => reset to 1 for today's visit.
                        user.LoginStreak = 1;
                        user.LastLoginDate = today;
                        changed = true;
                    }

                    if (changed)
                    {
                        await userManager.UpdateAsync(user);
                    }
                }
            }

            await _next(context);
        }
    }
}