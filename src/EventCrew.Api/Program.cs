using System.Data.Common;
using System.Text;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EventCrew");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'EventCrew' is required.");
}

var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtSettings.Key) || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException("JWT signing key must be configured as Jwt:Key with at least 32 UTF-8 bytes (for example, via the Jwt__Key environment variable).");
}
if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience) || jwtSettings.AccessTokenMinutes <= 0)
{
    throw new InvalidOperationException("JWT issuer, audience, and positive access-token lifetime must be configured.");
}

builder.Services.AddDbContext<EventCrewDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "name"
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddHttpClient<IAgentService, AgentService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AiService:BaseUrl"] ?? "http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(45);
});

var app = builder.Build();
app.UseExceptionHandler();

// ---- Middleware pipeline ----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
