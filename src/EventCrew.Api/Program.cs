using EventCrew.Api.Services;
using EventCrew.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Authentication (JWT Bearer) ──────────────────────────────────────────────
var jwtKey     = builder.Configuration["Jwt:Key"]
                    ?? Environment.GetEnvironmentVariable("JWT_KEY")
                    ?? "DevFallbackSecretKeyForLocalTestingOnly12345!";
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey == "SET_VIA_ENV_VAR_OR_SECRET")
{
    jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
             ?? "DevFallbackSecretKeyForLocalTestingOnly12345!";
}
var jwtIssuer  = builder.Configuration["Jwt:Issuer"]   ?? "EventCrew";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EventCrewUsers";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization();

// ── Application Services ─────────────────────────────────────────────────────
// Student 1: Events and Venues
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<IEventService, EventService>();

// Student 2: Volunteer Profiles and Applications
builder.Services.AddScoped<IVolunteerService, VolunteerService>();

// AI service (Python) — typed HttpClient
builder.Services.AddHttpClient<IAgentService, AgentService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["AiService:BaseUrl"] ?? "http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(45);
});

// ── MVC ─────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "EventCrew API",
        Version = "v1",
        Description =
            "EventCrew Volunteer & Event Management REST API. " +
            "Includes Event Planning, Venues, AI Agent, Volunteer Profiles & Applications.",
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without the 'Bearer' prefix).",
    });

    c.AddSecurityRequirement(doc =>
    {
        var requirement = new OpenApiSecurityRequirement();
        requirement.Add(new OpenApiSecuritySchemeReference("Bearer"), new List<string>());
        return requirement;
    });
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Automatic database seeding on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
