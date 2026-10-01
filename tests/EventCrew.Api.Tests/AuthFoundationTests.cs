using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventCrew.Api.Controllers;
using EventCrew.Api.Dtos;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventCrew.Api.Tests;

public class AuthFoundationTests
{
    private const string SigningKey = "test-signing-key-with-more-than-32-bytes-long";

    [Fact]
    public void PasswordServiceHashesAndVerifiesPasswords()
    {
        var service = CreatePasswordService();
        var user = CreateUser();

        user.PasswordHash = service.HashPassword(user, "correct horse battery staple");

        Assert.NotEqual("correct horse battery staple", user.PasswordHash);
        Assert.True(service.VerifyPassword(user, "correct horse battery staple"));
        Assert.False(service.VerifyPassword(user, "incorrect password"));
    }

    [Fact]
    public void JwtContainsIdentityClaimsButNoCredentialClaims()
    {
        var user = CreateUser();
        user.PasswordHash = "sensitive-hash-value";
        var service = CreateJwtService();

        var generated = service.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(generated.AccessToken);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(claim => claim.Type == "sub").Value);
        Assert.Equal(user.Email, jwt.Claims.Single(claim => claim.Type == "email").Value);
        Assert.Equal(user.Role, jwt.Claims.Single(claim => claim.Type == "role").Value);
        Assert.Equal(user.FullName, jwt.Claims.Single(claim => claim.Type == "name").Value);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.True(generated.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void PasswordHashMapsToExistingColumnInBothContexts()
    {
        const string modelOnlyConnection = "Host=localhost;Database=eventcrew_model_test;Username=eventcrew";
        using var appContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(modelOnlyConnection).Options);
        using var featureContext = new EventCrewDbContext(new DbContextOptionsBuilder<EventCrewDbContext>().UseNpgsql(modelOnlyConnection).Options);

        AssertPasswordHashMapping(appContext.Model.FindEntityType(typeof(User))!);
        AssertPasswordHashMapping(featureContext.Model.FindEntityType(typeof(User))!);
    }

    [Fact]
    public async Task LoginWithValidCredentialsReturnsTokenAndSafeUser()
    {
        await using var db = CreateDb();
        var user = await SeedUserAsync(db, "member@example.com", "a secure password", active: true);

        var result = await CreateController(db).Login(new LoginRequest { Email = " MEMBER@example.com ", Password = "a secure password" }, CancellationToken.None);

        var response = Assert.IsType<LoginResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.True(response.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal(user.Id, response.User.Id);
        Assert.Equal(user.Role, response.User.Role);
        Assert.DoesNotContain("PasswordHash", typeof(AuthenticatedUserResponse).GetProperties().Select(property => property.Name));
    }

    [Fact]
    public async Task LoginRejectsWrongPasswordWithGenericFailure()
    {
        await using var db = CreateDb();
        await SeedUserAsync(db, "member@example.com", "right password", active: true);

        var result = await CreateController(db).Login(new LoginRequest { Email = "member@example.com", Password = "wrong password" }, CancellationToken.None);

        AssertGenericAuthenticationFailure(result.Result);
    }

    [Fact]
    public async Task LoginRejectsUnknownEmailWithGenericFailure()
    {
        await using var db = CreateDb();

        var result = await CreateController(db).Login(new LoginRequest { Email = "unknown@example.com", Password = "some password" }, CancellationToken.None);

        AssertGenericAuthenticationFailure(result.Result);
    }

    [Fact]
    public async Task LoginRejectsInactiveUserWithGenericFailure()
    {
        await using var db = CreateDb();
        await SeedUserAsync(db, "inactive@example.com", "right password", active: false);

        var result = await CreateController(db).Login(new LoginRequest { Email = "inactive@example.com", Password = "right password" }, CancellationToken.None);

        AssertGenericAuthenticationFailure(result.Result);
    }

    [Fact]
    public async Task MeIsAuthorizeProtectedAndRejectsMissingSubject()
    {
        await using var db = CreateDb();
        var controller = CreateController(db);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.Me(CancellationToken.None);

        Assert.NotNull(typeof(AuthController).GetMethod(nameof(AuthController.Me))!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task MeUsesSubjectFromValidatedJwtToLoadCurrentUser()
    {
        await using var db = CreateDb();
        var user = await SeedUserAsync(db, "member@example.com", "right password", active: true);
        var accessToken = CreateJwtService().CreateAccessToken(user).AccessToken;
        var principal = ValidateToken(accessToken);
        var controller = CreateController(db);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

        var result = await controller.Me(CancellationToken.None);

        var response = Assert.IsType<AuthenticatedUserResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.Role, response.Role);
    }

    private static void AssertPasswordHashMapping(IEntityType entity)
    {
        var property = entity.FindProperty(nameof(User.PasswordHash));
        Assert.NotNull(property);
        var tableName = StoreObjectIdentifier.Create(entity, StoreObjectType.Table)!.Value;
        Assert.Equal("password_hash", property.GetColumnName(tableName));
        Assert.Equal(255, property.GetMaxLength());
        Assert.False(property.IsNullable);
    }

    private static void AssertGenericAuthenticationFailure(ActionResult? actionResult)
    {
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(unauthorized.Value);
        Assert.Equal("Authentication failed", problem.Title);
        Assert.Equal("The email or password is incorrect.", problem.Detail);
    }

    private static AuthController CreateController(AppDbContext db) =>
        new(db, CreatePasswordService(), CreateJwtService());

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<User> SeedUserAsync(AppDbContext db, string email, string password, bool active)
    {
        var user = CreateUser(email, active);
        user.PasswordHash = CreatePasswordService().HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static User CreateUser(string email = "member@example.com", bool active = true) => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Test Volunteer",
        Email = email,
        Role = "Volunteer",
        IsActive = active
    };

    private static PasswordService CreatePasswordService() => new(new PasswordHasher<User>());

    private static JwtTokenService CreateJwtService() => new(Options.Create(new JwtOptions
    {
        Key = SigningKey,
        Issuer = "EventCrew.Tests",
        Audience = "EventCrew.Tests",
        AccessTokenMinutes = 15
    }));

    private static ClaimsPrincipal ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = "EventCrew.Tests",
            ValidateAudience = true,
            ValidAudience = "EventCrew.Tests",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "name"
        }, out _);
    }
}
