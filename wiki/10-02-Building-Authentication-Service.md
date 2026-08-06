# Building an Authentication Service

This guide walks through the implementation of the `FractalDataWorks.Reference.Authentication` and `FractalDataWorks.Reference.Authentication.MultiTenant` packages, demonstrating how to build a SQL database-backed authentication service that implements FractalDataWorks abstractions.

## Overview

The authentication service consists of two packages:

| Package | Purpose |
|---------|---------|
| `FractalDataWorks.Reference.Authentication` | Core user management and password validation |
| `FractalDataWorks.Reference.Authentication.MultiTenant` | Tenant resolution and Row-Level Security |

Both packages implement existing FDW interfaces rather than creating new abstractions.

## Prerequisites

Before starting, ensure you understand:

- [ReferenceSolutions Architecture](10-01-ReferenceSolutions-Architecture.md) - Package management
- [FDW Results Pattern](03-01-Results-Pattern.md) - `IGenericResult<T>` usage
- [MessageLogging](07-02-MessageLogging-Attribute.md) - Structured logging

## Part 1: FractalDataWorks.Reference.Authentication

### Step 1: Create Project Structure

```powershell
cd SharedPackages/src
dotnet new classlib -n FractalDataWorks.Reference.Authentication
cd FractalDataWorks.Reference.Authentication
```

Configure the `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>true</IsPackable>
    <PackageId>FractalDataWorks.Reference.Authentication</PackageId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FractalDataWorks.Abstractions" />
    <PackageReference Include="FractalDataWorks.Results" />
    <PackageReference Include="FractalDataWorks.Messages" />
    <PackageReference Include="Microsoft.Data.SqlClient" />
    <PackageReference Include="Microsoft.AspNetCore.Cryptography.KeyDerivation" />
  </ItemGroup>
</Project>
```

### Step 2: Define the User Model

Create `Models/User.cs`:

```csharp
namespace FractalDataWorks.Reference.Authentication.Models;

/// <summary>
/// Represents an authenticated user.
/// </summary>
public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = [];
}
```

### Step 3: Implement Password Hashing

Create `Hashing/IPasswordHasher.cs`:

```csharp
namespace FractalDataWorks.Reference.Authentication.Hashing;

/// <summary>
/// Password hashing service using PBKDF2.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password with a randomly generated salt.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    bool VerifyPassword(string password, string hash);
}
```

Create `Hashing/Pbkdf2PasswordHasher.cs`:

```csharp
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace FractalDataWorks.Reference.Authentication.Hashing;

/// <summary>
/// PBKDF2-based password hasher with OWASP-recommended settings.
/// Format: {base64-salt}${base64-hash}
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;        // 128 bits
    private const int HashSize = 32;        // 256 bits
    private const int Iterations = 100_000; // OWASP minimum recommendation

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        return $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        var parts = hash.Split('$');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);

        var computedHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }
}
```

**Key Security Points:**
- 100,000 iterations (OWASP recommendation)
- HMAC-SHA256 pseudo-random function
- Constant-time comparison to prevent timing attacks
- Unique salt per password

### Step 4: Define the Service Interface

Create `Services/IAuthenticationService.cs`:

```csharp
using FractalDataWorks.Results;
using FractalDataWorks.Reference.Authentication.Models;

namespace FractalDataWorks.Reference.Authentication.Services;

/// <summary>
/// SQL database-backed authentication service.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Validates user credentials against the database.
    /// </summary>
    Task<IGenericResult<User>> ValidateCredentials(
        string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    Task<IGenericResult<User>> GetUser(
        string username, CancellationToken ct = default);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<IGenericResult<User>> GetUserById(
        int userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new user with the specified roles.
    /// </summary>
    Task<IGenericResult<int>> CreateUser(
        string username, string password, string? email,
        IEnumerable<string> roles, CancellationToken ct = default);

    /// <summary>
    /// Updates the last login timestamp.
    /// </summary>
    Task<IGenericResult> UpdateLastLogin(
        int userId, CancellationToken ct = default);

    /// <summary>
    /// Gets tenant IDs assigned to a user.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<Guid>>> GetUserTenants(
        int userId, CancellationToken ct = default);
}
```

**Design Notes:**
- All methods return `IGenericResult<T>` (Railway-Oriented Programming)
- No `Async` suffix on method names (FDW convention)
- Cancellation token support throughout
- Separation of concerns: validation vs retrieval

### Step 5: Implement the Service

Create `Services/SqlAuthenticationService.cs`:

```csharp
using FractalDataWorks.Results;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using FractalDataWorks.Reference.Authentication.Hashing;
using FractalDataWorks.Reference.Authentication.Models;

namespace FractalDataWorks.Reference.Authentication.Services;

public sealed class SqlAuthenticationService : IAuthenticationService
{
    private readonly string _connectionString;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<SqlAuthenticationService> _logger;

    public SqlAuthenticationService(
        string connectionString,
        IPasswordHasher passwordHasher,
        ILogger<SqlAuthenticationService> logger)
    {
        _connectionString = connectionString;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<IGenericResult<User>> ValidateCredentials(
        string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return GenericResult<User>.Failure("Username is required.");

        if (string.IsNullOrWhiteSpace(password))
            return GenericResult<User>.Failure("Password is required.");

        var userResult = await GetUser(username, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess)
        {
            _logger.LogWarning("Authentication failed for {Username}: user not found", username);
            return GenericResult<User>.Failure("Invalid username or password.");
        }

        var user = userResult.Value!;

        if (!user.IsActive)
        {
            _logger.LogWarning("Authentication failed for {Username}: account inactive", username);
            return GenericResult<User>.Failure("Account is inactive.");
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Authentication failed for {Username}: invalid password", username);
            return GenericResult<User>.Failure("Invalid username or password.");
        }

        _logger.LogInformation("User {Username} authenticated successfully", username);
        return GenericResult<User>.Success(user);
    }

    public async Task<IGenericResult<User>> GetUser(
        string username, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Username, PasswordHash, Email, IsActive, CreatedAt, LastLoginAt
            FROM cfg.Users
            WHERE Username = @Username
            """;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                return GenericResult<User>.Failure($"User '{username}' not found.");

            var user = new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                IsActive = reader.GetBoolean(4),
                CreatedAt = reader.GetDateTime(5),
                LastLoginAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
            };

            // Load roles in separate query
            user.Roles = await GetUserRoles(connection, user.Id, ct).ConfigureAwait(false);

            return GenericResult<User>.Success(user);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error getting user {Username}", username);
            return GenericResult<User>.Failure($"Database error: {ex.Message}");
        }
    }

    private static async Task<List<string>> GetUserRoles(
        SqlConnection connection, int userId, CancellationToken ct)
    {
        const string sql = "SELECT Role FROM cfg.UserRoles WHERE UserId = @UserId";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        var roles = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            roles.Add(reader.GetString(0));

        return roles;
    }

    // Additional methods omitted for brevity - see source code
}
```

### Step 6: Create DI Extension

Create `Extensions/AuthenticationServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using FractalDataWorks.Reference.Authentication.Hashing;
using FractalDataWorks.Reference.Authentication.Services;

namespace FractalDataWorks.Reference.Authentication.Extensions;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddReferenceAuthentication(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddScoped<IAuthenticationService>(sp =>
            new SqlAuthenticationService(
                connectionString,
                sp.GetRequiredService<IPasswordHasher>(),
                sp.GetRequiredService<ILogger<SqlAuthenticationService>>()));

        return services;
    }
}
```

### Step 7: Create Database Schema

Add to your `init.sql`:

```sql
-- Users table
CREATE TABLE cfg.Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Email NVARCHAR(256) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2 NULL
);

-- User roles (many-to-many)
CREATE TABLE cfg.UserRoles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES cfg.Users(Id),
    Role NVARCHAR(50) NOT NULL,
    UNIQUE(UserId, Role)
);

CREATE INDEX IX_Users_Username ON cfg.Users(Username);
CREATE INDEX IX_UserRoles_UserId ON cfg.UserRoles(UserId);
```

## Part 2: FractalDataWorks.Reference.Authentication.MultiTenant

### Step 1: Understand FDW Multitenancy Abstractions

The FDW framework provides these interfaces in `FractalDataWorks.Services.Multitenancy.Abstractions`:

```csharp
// Core tenant definition
public interface ITenant
{
    Guid Id { get; }
    string Name { get; }
    string Slug { get; }
    bool IsActive { get; }
    ITenantTheme? Theme { get; }
    ITenantOptions? Options { get; }
    IReadOnlyList<string> AvailableRoles { get; }
}

// Request-scoped tenant context
public interface ITenantContext
{
    bool HasTenant { get; }
    Guid TenantId { get; }
    ITenant? Tenant { get; }
}

// Mutable context for middleware
public interface IMutableTenantContext : ITenantContext
{
    void SetTenant(ITenant tenant);
    void ClearTenant();
}

// Tenant lookup and resolution
public interface ITenantProvider
{
    Task<IGenericResult<ITenant>> GetTenant(Guid tenantId, CancellationToken ct = default);
    Task<IGenericResult<ITenant>> GetTenantBySlug(string slug, CancellationToken ct = default);
    Task<IGenericResult<IEnumerable<ITenant>>> GetActiveTenants(CancellationToken ct = default);
    Task<IGenericResult<bool>> ValidateTenantAccess(Guid tenantId, string userId, CancellationToken ct = default);
    Task<IGenericResult<ITenant>> ResolveTenant(ITenantResolutionContext context, CancellationToken ct = default);
}
```

### Step 2: Implement ITenant

Create `Models/SqlTenant.cs`:

```csharp
using FractalDataWorks.Services.Multitenancy.Abstractions;

namespace FractalDataWorks.Reference.Authentication.MultiTenant.Models;

public sealed class SqlTenant : ITenant
{
    public Guid Id { get; }
    public string Name { get; }
    public string Slug { get; }
    public bool IsActive { get; }
    public string? ConnectionStringKey { get; }
    public ITenantTheme? Theme { get; }
    public ITenantOptions? Options { get; }
    public IReadOnlyList<string> AvailableRoles { get; }

    public SqlTenant(
        Guid id, string name, string slug, bool isActive,
        string? connectionStringKey, ITenantTheme? theme,
        ITenantOptions? options, IReadOnlyList<string> availableRoles)
    {
        Id = id;
        Name = name;
        Slug = slug;
        IsActive = isActive;
        ConnectionStringKey = connectionStringKey;
        Theme = theme;
        Options = options;
        AvailableRoles = availableRoles;
    }
}
```

### Step 3: Implement ITenantProvider

Create `Services/SqlTenantProvider.cs`:

```csharp
public sealed class SqlTenantProvider : ITenantProvider
{
    private readonly string _connectionString;
    private readonly ILogger<SqlTenantProvider> _logger;

    public async Task<IGenericResult<ITenant>> GetTenant(
        Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, Slug, DisplayName, IsActive, ConnectionStringKey,
                   PrimaryColor, SecondaryColor, LogoUrl, FaviconUrl,
                   CustomCssUrl, DarkModeDefault,
                   MaxUsers, StorageQuotaBytes, ApiRateLimitPerMinute
            FROM cfg.Tenants
            WHERE Id = @TenantId
            """;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TenantId", tenantId);

            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                return GenericResult<ITenant>.Failure($"Tenant '{tenantId}' not found.");

            var tenant = await MapTenant(connection, reader, ct).ConfigureAwait(false);
            return GenericResult<ITenant>.Success(tenant);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error getting tenant {TenantId}", tenantId);
            return GenericResult<ITenant>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<IGenericResult<ITenant>> ResolveTenant(
        ITenantResolutionContext context, CancellationToken ct = default)
    {
        // Priority: Claims > Header > Route > Host
        if (context.ClaimsTenantId.HasValue)
            return await GetTenant(context.ClaimsTenantId.Value, ct).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(context.TenantHeader))
        {
            if (Guid.TryParse(context.TenantHeader, out var tenantId))
                return await GetTenant(tenantId, ct).ConfigureAwait(false);
            return await GetTenantBySlug(context.TenantHeader, ct).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(context.RouteSlug))
            return await GetTenantBySlug(context.RouteSlug, ct).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(context.Host))
        {
            var parts = context.Host.Split('.');
            if (parts.Length >= 2)
            {
                var result = await GetTenantBySlug(parts[0], ct).ConfigureAwait(false);
                if (result.IsSuccess) return result;
            }
        }

        return GenericResult<ITenant>.Failure("Could not resolve tenant from request.");
    }

    // Additional methods...
}
```

### Step 4: Implement Tenant Middleware

Create `Middleware/TenantResolutionMiddleware.cs`:

```csharp
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IMutableTenantContext tenantContext,
        ITenantProvider tenantProvider)
    {
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;

        if (Guid.TryParse(tenantClaim, out var tenantId))
        {
            var result = await tenantProvider.GetTenant(tenantId);
            if (result.IsSuccess)
                tenantContext.SetTenant(result.Value!);
        }

        await _next(context);
    }
}
```

Create `Middleware/TenantSessionContextMiddleware.cs`:

```csharp
public class TenantSessionContextMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IDbConnection connection)
    {
        if (tenantContext.HasTenant)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await using var cmd = new SqlCommand("""
                EXEC sp_set_session_context @key=N'TenantId', @value=@TenantId;
                EXEC sp_set_session_context @key=N'UserId', @value=@UserId;
                """, (SqlConnection)connection);

            cmd.Parameters.AddWithValue("@TenantId", tenantContext.TenantId);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        await _next(context);
    }
}
```

### Step 5: Configure Row-Level Security

```sql
-- Security predicate using SESSION_CONTEXT
CREATE FUNCTION cfg.fn_TenantSecurityPredicate(@TenantId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS result
WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER)
   OR SESSION_CONTEXT(N'TenantId') IS NULL  -- Admin access
   OR @TenantId IN (
       SELECT ut.TenantId FROM cfg.UserTenants ut
       WHERE ut.UserId = CAST(SESSION_CONTEXT(N'UserId') AS INT)
   );

-- Apply to data tables
CREATE SECURITY POLICY cfg.TeamsSecurityPolicy
ADD FILTER PREDICATE cfg.fn_TenantSecurityPredicate(TenantId) ON nfl.Teams
WITH (STATE = ON);
```

## Part 3: Integration

### Register Services in Program.cs

```csharp
// Add authentication services
builder.Services.AddReferenceAuthentication(connectionString);
builder.Services.AddReferenceMultiTenant(connectionString);

// ...

var app = builder.Build();

// Add middleware after UseAuthentication
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<TenantSessionContextMiddleware>();
app.UseAuthorization();
```

### Update TokenEndpoint

```csharp
public sealed class TokenEndpoint : Endpoint<TokenRequest, TokenResponse>
{
    private readonly IAuthenticationService _authService;

    public override async Task HandleAsync(TokenRequest req, CancellationToken ct)
    {
        var authResult = await _authService.ValidateCredentials(
            req.Username, req.Password, ct);

        if (!authResult.IsSuccess)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = authResult.Value!;

        // Get user's tenant assignments
        var tenantsResult = await _authService.GetUserTenants(user.Id, ct);
        Guid? tenantId = tenantsResult.IsSuccess && tenantsResult.Value?.Count == 1
            ? tenantsResult.Value[0]
            : null;

        // Generate JWT with tenant_id claim
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Roles.FirstOrDefault() ?? "User")
        };

        if (tenantId.HasValue)
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));

        // Create and return JWT...
    }
}
```

## Testing the Implementation

### Test Users

| Username | Password | Role | Tenant | Expected Behavior |
|----------|----------|------|--------|-------------------|
| testuser | TestPassword123! | User | - | Access without tenant |
| admin | AdminPassword123! | Admin | - | Cross-tenant access |
| afcuser | AfcPassword123! | User | AFC | Only sees AFC teams |
| nfcuser | NfcPassword123! | User | NFC | Only sees NFC teams |

### Test Scenarios

1. **Login as afcuser** - Token contains `tenant_id` for AFC
2. **GET /api/v1/nfl/teams** - Only AFC teams returned (RLS filter)
3. **Login as admin** - No `tenant_id` in token
4. **GET /api/v1/nfl/teams** - All teams returned (no RLS filter)

## Key Takeaways

1. **Implement existing abstractions** - FDW provides `ITenant`, `ITenantProvider`, `ITenantContext`
2. **Results over exceptions** - Always return `IGenericResult<T>`
3. **RLS for data isolation** - SQL Server SESSION_CONTEXT + security policies
4. **Middleware order matters** - Resolution before authorization
5. **No Async suffix** - FDW convention omits the suffix

## Related Documentation

- [ReferenceSolutions Architecture](10-01-ReferenceSolutions-Architecture.md)
- [Results Pattern](03-01-Results-Pattern.md)
- [FDW Multitenancy Abstractions](06-15-Multitenancy-Abstractions.md)
