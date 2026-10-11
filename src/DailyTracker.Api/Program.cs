using System.Text.Json.Serialization;
using DailyTracker.Application;
using DailyTracker.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=/data/dailytracker.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IHabitService, HabitService>();
builder.Services.AddScoped<IDailyEntryService, DailyEntryService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins(
            builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedHabitsAsync(db);
}

app.UseCors("AngularDev");
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

// Bind to Render's PORT or fallback to 10000
var port = builder.Configuration["PORT"] ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

app.Run();
