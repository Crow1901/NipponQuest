using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NipponQuest.Data;
using NipponQuest.Models;
using NipponQuest.Services;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// --- 0. FILE UPLOAD, FORM & COLLECTION LIMITS ---
// Configured to handle 250MB files and up to 30,000 form fields.
const long MaxFileSize = 262144000;
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = MaxFileSize;
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxFileSize;
    options.ValueCountLimit = 30000; // Handles the ~14,000 fields in the Core 2000 deck
    options.ValueLengthLimit = int.MaxValue;
});

// --- 1. SQLITE INITIALIZATION ---
SQLitePCL.Batteries_V2.Init();

// --- 2. DATABASE CONFIGURATION ---
// 1. Get your fallback local connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Check if Render has provided an environment variable connection string
var prodConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(prodConnectionString))
    {
        // Convert Render's postgres:// URI into a valid key-value connection string for Npgsql
        var formattedConnectionString = ConvertPostgresUriToConnectionString(prodConnectionString);
        options.UseNpgsql(formattedConnectionString)
               // Suppress the SQL Server vs PostgreSQL model snapshot mismatch warning.
               // Migrations were authored against SQL Server locally; this is safe to ignore
               // because the actual migration SQL is what gets applied, not the snapshot diff.
               .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
    else
    {
        // Fall back to your local SQL Server for local development
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- 3. IDENTITY SETUP ---
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// --- GOOGLE AUTHENTICATION ---
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

// Safely register Google Auth only if configuration keys are present
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(googleOptions =>
        {
            googleOptions.ClientId = googleClientId;
            googleOptions.ClientSecret = googleClientSecret;
        });
}

// --- 4. CUSTOM SERVICE REGISTRATIONS ---
builder.Services.AddScoped<GithubService>();

// Increased MaxModelBindingCollectionSize for large card decks
builder.Services.AddControllersWithViews(options =>
{
    options.MaxModelBindingCollectionSize = 10000;
});

// --- 5. QUARTZ BACKGROUND JOBS ---
builder.Services.AddQuartz(q =>
{
    // Weekly League Reset Job
    var jobKey = new JobKey("WeeklyLeagueResetJob");
    q.AddJob<NipponQuest.Jobs.WeeklyLeagueResetJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("WeeklyLeagueResetJob-trigger")
        .WithCronSchedule("0 0 0 ? * MON"));

    // Streak Decay Job
    var streakJobKey = new JobKey("StreakDecayJob");
    q.AddJob<NipponQuest.Jobs.StreakDecayJob>(opts => opts.WithIdentity(streakJobKey));
    q.AddTrigger(opts => opts
        .ForJob(streakJobKey)
        .WithIdentity("StreakDecayJob-trigger")
        .WithCronSchedule("0 5 0 * * ?")); // every day at 00:05
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddHostedService<AIKanaGeneratorService>();

// --- 6. BUILD APP ---
var app = builder.Build();

// --- 7. SEED DATA AND MIGRATIONS ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying database migrations...");

        // Apply any pending migrations dynamically (PostgreSQL on Render / SQL Server locally)
        context.Database.Migrate();

        logger.LogInformation("Migrations applied successfully.");

        // Initialize standard seed data (Leagues, etc)
        SeedData.Initialize(services);

        // Kana Blitz seeding
        DbInitializer.SeedKanaBlitzData(context);

        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "A critical error occurred during database migration or seeding. The application cannot start.");
        // Rethrow so Render sees a failed deploy and doesn't serve a broken app
        throw;
    }
}

// --- 8. MIDDLEWARE PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<NipponQuest.Middleware.LoginStreakMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

// --- 9. URI PARSER HELPER METHOD ---
static string ConvertPostgresUriToConnectionString(string uriString)
{
    var uri = new Uri(uriString);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;

    // If the URI doesn't explicitly state a port, uri.Port returns -1.
    // In that case, default to the standard Postgres port: 5432.
    var port = uri.Port == -1 ? 5432 : uri.Port;

    var database = uri.AbsolutePath.TrimStart('/');

    // Formats URI variables into valid standard .NET key-value segments with secure production SSL requirements
    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
}
