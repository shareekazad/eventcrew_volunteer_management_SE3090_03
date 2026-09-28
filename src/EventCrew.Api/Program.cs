using System.Data.Common;
using EventCrew.Api.Services;
using EventCrew.Infrastructure.Data;
using EventCrew.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<EventCrewDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddHttpClient<IAgentService, AgentService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AiService:BaseUrl"] ?? "http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(45);
});
var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontends");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health/database", async (EventCrewDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ok" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch (DbException)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("DatabaseHealth")
.WithOpenApi();

app.Run();
