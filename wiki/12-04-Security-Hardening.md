# 12-04 Security Hardening

This guide covers how FDW Reference Solutions address security: OWASP compliance, security headers, CORS, database credential isolation, and JWT authentication.

## OWASP Top 10 Compliance

| # | OWASP Category | FDW Mitigation |
|---|---------------|----------------|
| A01 | Broken Access Control | Database-backed RBAC with `fdw:{resource}:{action}` policies on every endpoint. JWT authentication with role claims, `InternalApiKeyMiddleware` for service-to-service, schema-level DB permissions with least-privilege logins per domain. See [12-05 Authorization](12-05-Authorization.md). |
| A02 | Cryptographic Failures | Secrets resolved via `ISecretManager` (environment variables or Azure Key Vault). Never hardcoded in appsettings.json. JWT signing keys injected at runtime. |
| A03 | Injection | Parameterized SQL via `DataCommand` fluent API. No string concatenation in queries. Input validation at endpoint layer. |
| A04 | Insecure Design | Three-phase fail-fast validation at startup. Missing required configuration causes `return 1` with structured MessageLogging, not silent empty defaults. |
| A05 | Security Misconfiguration | `SecurityHeadersMiddleware` adds OWASP headers by default. HSTS enabled in production. CSP configured per-service. |
| A06 | Vulnerable Components | Central Package Management (`Directory.Packages.props`) ensures consistent dependency versions across all solutions. |
| A07 | Identity/Auth Failures | JWT with configurable lifetime, refresh tokens, clock skew limits. Passwords verified with bcrypt. Account lockout support. |
| A08 | Software/Data Integrity Failures | Dacpac-based schema deployment with drift detection. Version-on-write pattern for configuration audit trail. |
| A09 | Logging/Monitoring Failures | MessageLogging with structured EventIds (`FDW-NNNN`). Seq aggregation. Distributed tracing via `Serilog.Enrichers.Span` and OpenTelemetry. |
| A10 | Server-Side Request Forgery | Proxy services use named `HttpClient` instances with fixed base URLs configured in appsettings.json. No user-controlled URL construction. |

## Security Headers

`SecurityHeadersMiddleware` adds OWASP-recommended headers to every HTTP response. It is registered automatically by `UseFrameworkMiddleware()` or standalone via `UseFrameworkSecurityHeaders()`.

### Headers Applied

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME type sniffing attacks. |
| `X-Frame-Options` | `DENY` (default) or `SAMEORIGIN` | Prevents clickjacking by controlling iframe embedding. |
| `X-XSS-Protection` | `0` | Disables legacy browser XSS filter (modern CSP is the preferred protection). |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer information leaked to external sites. |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()` | Disables unused browser features that could be exploited. |
| `Content-Security-Policy` | (configurable, see below) | Restricts which origins can load resources. |

### Default Content Security Policy

When `EnableDefaultCsp = true` (the default), this CSP is generated:

```
default-src 'self';
script-src 'self' 'unsafe-inline';
style-src 'self' 'unsafe-inline';
img-src 'self' data: https:;
font-src 'self' https://fonts.gstatic.com;
connect-src 'self';
frame-ancestors 'none';
base-uri 'self';
form-action 'self'
```

To override with a custom CSP string, set `ContentSecurityPolicy` (this takes precedence over `EnableDefaultCsp`):

```csharp
app.UseFrameworkMiddleware(new SecurityHeadersOptions
{
    ContentSecurityPolicy = "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'"
});
```

### Sensitive Path Cache Control

Requests to paths listed in `SensitivePaths` receive additional no-cache headers to prevent browser or proxy caching of authentication and user data:

```
Cache-Control: no-store, no-cache, must-revalidate, proxy-revalidate
Pragma: no-cache
Expires: 0
```

Default sensitive paths: `/api/v1/auth`, `/api/v1/users`, `/api/v1/tenants`

### Configuration

In appsettings.json:

```json
{
  "SecurityHeaders": {
    "AllowFraming": false,
    "EnableDefaultCsp": true,
    "ContentSecurityPolicy": null,
    "SensitivePaths": ["/api/v1/auth", "/api/v1/users", "/api/v1/tenants"]
  }
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AllowFraming` | `bool` | `false` | `false` sets `X-Frame-Options: DENY`. `true` sets `SAMEORIGIN` (needed for Blazor iframe hosting). |
| `EnableDefaultCsp` | `bool` | `true` | Whether to generate the default CSP directives. |
| `ContentSecurityPolicy` | `string?` | `null` | Custom CSP string. Overrides the default when set. |
| `SensitivePaths` | `string[]` | `["/api/v1/auth", "/api/v1/users", "/api/v1/tenants"]` | URL path prefixes that receive no-cache headers. |

### Source Code

Implementation: [`Fdw.Hosting/Middleware/SecurityHeadersMiddleware.cs`](../src/Fdw.Hosting/Middleware/SecurityHeadersMiddleware.cs)

Options: [`Fdw.Hosting/Configuration/SecurityHeadersOptions.cs`](../src/Fdw.Hosting/Configuration/SecurityHeadersOptions.cs)

## CORS Configuration

CORS is configured via appsettings.json and registered with `AddFrameworkCors()`.

### Development Configuration

In development, configure CORS to allow the reference-ui origin:

```json
{
  "Cors": {
    "Enabled": true,
    "Origins": [
      "https://localhost:5007",
      "http://localhost:5007"
    ],
    "Methods": ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"],
    "Headers": ["Content-Type", "Authorization", "X-Requested-With", "X-Tenant-Id", "X-Correlation-Id"],
    "ExposedHeaders": ["X-Correlation-Id", "X-Request-Id", "WWW-Authenticate", "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset"],
    "AllowCredentials": true,
    "PreflightMaxAgeSeconds": 600
  }
}
```

### Production Configuration

In production, specify exact origins. Never use wildcard `*` with `AllowCredentials`:

```json
{
  "Cors": {
    "Enabled": true,
    "Origins": [
      "https://app.yourdomain.com"
    ],
    "AllowCredentials": true,
    "PreflightMaxAgeSeconds": 3600
  }
}
```

### Fallback Behavior

When `Origins` is empty, `AddFrameworkCors` falls back to allowing `localhost` origins only:

```csharp
policy.SetIsOriginAllowed(origin =>
    string.Equals(new Uri(origin).Host, "localhost", StringComparison.OrdinalIgnoreCase));
```

This is safe for development but must be overridden in production with explicit origins.

### Middleware Ordering

CORS must be placed before authentication in the middleware pipeline for preflight (OPTIONS) requests to succeed:

```csharp
app.UseFrameworkMiddleware();     // 1. Exception handler + HTTPS + Security Headers + Serilog
app.UseCors();              // 2. CORS (before auth, so OPTIONS passes)
app.UseAuthentication();    // 3. JWT Bearer validation
app.UseAuthorization();     // 4. Role/policy checks
```

### Source Code

Implementation: [`Fdw.Hosting/Extensions/CorsExtensions.cs`](../src/Fdw.Hosting/Extensions/CorsExtensions.cs)

Options: [`Fdw.Hosting/Configuration/CorsOptions.cs`](../src/Fdw.Hosting/Configuration/CorsOptions.cs)

## Database Credential Isolation

FDW enforces the principle of least privilege through schema-level SQL Server security. Each schema has a dedicated SQL login with only the permissions that schema requires.

### Schema Security Model

```
ConfigurationDb (single database, one schema per domain)
  |
  +-- conn, data, auth, authz, pipe, sched, notify, transform, workflow, …
  |     -> fdw_config (full CRUD across all domain schemas)
  |     -> fdw_config_ro (SELECT only — used by ETL / Scheduler servers)

AuthDb
  +-- auth schema -> fdw_auth (full CRUD; credentials, sessions)

OpsDb
  +-- ops, audit, health, sched, log -> fdw_ops (full CRUD)

DataDb (reference business data, e.g. NflData)
  +-- NflData schema -> fdw_nfl (full CRUD, scoped to NflData)
```

### SQL Login Details

| Login | Password Env Var | Database / Schemas | Permissions |
|-------|-----------------|---------|-------------|
| `fdw_config` | `FDW_SECRET_CONFIG_PASSWORD` | ConfigurationDb | Full CRUD on every domain schema |
| `fdw_config_ro` | `FDW_SECRET_CONFIG_RO_PASSWORD` | ConfigurationDb | SELECT only |
| `fdw_auth` | `FDW_SECRET_AUTH_PASSWORD` | AuthDb / auth | Full CRUD |
| `fdw_ops` | `FDW_SECRET_OPS_PASSWORD` | OpsDb | Full CRUD on every schema |
| `fdw_nfl` | `FDW_SECRET_NFL_PASSWORD` | DataDb / NflData | Full CRUD (reference business data) |

### Service-to-Login Mapping

Each service connects with the minimum-privilege set of logins:

| Service | Logins Used | Why |
|---------|------------|-----|
| **reference-api** | fdw_config, fdw_auth, fdw_tenant, fdw_ops | Full API: config CRUD + auth reads + tenant reads + ops tracking |
| **reference-etl** | fdw_config_ro, fdw_etl | Reads config (read-only), writes ETL execution history |
| **reference-scheduler** | fdw_config, fdw_sched | Writes schedules and config (via ConfigurationWriters) |
| **reference-ui** | (none) | No direct DB access; proxies all requests through reference-api |

### Read-Only vs Read-Write Config Access

Services that need read-only config access use `fdw_config_ro` in their `ConfigurationDb` section:

```json
{
  "ConfigurationDb": {
    "Authentication": {
      "Username": "fdw_config_ro",
      "SecretKeyName": "CONFIG_RO_PASSWORD"
    }
  }
}
```

Services that need write access (reference-api for ConfigurationWriters, reference-scheduler for schedule updates) use `fdw_config`:

```json
{
  "ConfigurationDb": {
    "Authentication": {
      "Username": "fdw_config",
      "SecretKeyName": "CONFIG_PASSWORD"
    }
  }
}
```

### Blast Radius

If a single service is compromised, the attacker gains access only to the schemas assigned to that service's login:

- Compromised reference-etl: can read config (read-only) and modify ETL execution history. Cannot modify configuration, authentication data, or scheduling.
- Compromised reference-scheduler: can modify schedules and configuration. Cannot read authentication data or ETL execution history.
- Compromised reference-ui: no direct database access at all.

## JWT Authentication Flow

reference-api implements JWT Bearer authentication for user-facing endpoints.

### Token Lifecycle

```
1. Client sends credentials:
   POST /api/v1/auth/login  { "username": "admin", "password": "..." }

2. Server validates against auth.Users table (bcrypt hash comparison):
   SELECT PasswordHash FROM auth.Users WHERE Username = @username

3. Server generates tokens:
   - Access token (configurable lifetime, default 60 min)
   - Refresh token (configurable lifetime, default 7 days)
   - Returns: { accessToken, refreshToken, expiresAt }

4. Client sends access token on subsequent requests:
   GET /api/v1/data/nfl/teams
   Authorization: Bearer <accessToken>

5. When access token expires, client refreshes:
   POST /api/v1/auth/refresh  { "refreshToken": "..." }
   Returns: new { accessToken, refreshToken, expiresAt }
```

### JWT Configuration

```json
{
  "Authentication": {
    "Jwt": [
      {
        "Name": "Default",
        "Issuer": "Fdw.Reference.Api",
        "Audience": "Fdw.Reference.Api",
        "SecretKey": "ThisIsADevelopmentSecretKeyThatShouldBeAtLeast32BytesLong!",
        "AccessTokenExpirationMinutes": 60,
        "RefreshTokenExpirationMinutes": 10080,
        "ValidateIssuer": true,
        "ValidateAudience": true,
        "ValidateLifetime": true,
        "ValidateIssuerSigningKey": true,
        "ClockSkewSeconds": 30,
        "RoleClaimType": "role",
        "NameClaimType": "name",
        "AvailableRoles": ["Admin", "User", "ReadOnly"]
      }
    ]
  }
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `SecretKey` | (dev key) | HMAC-SHA256 signing key. Must be at least 32 bytes. Override via environment variable in production. |
| `AccessTokenExpirationMinutes` | 60 | Access token lifetime. Reduce to 15-30 minutes in production. |
| `RefreshTokenExpirationMinutes` | 10080 | Refresh token lifetime (7 days). |
| `ClockSkewSeconds` | 30 | Tolerance for clock differences between services. |
| `RoleClaimType` | `role` | JWT claim name for user roles. Must match FastEndpoints config. |
| `NameClaimType` | `name` | JWT claim name for username. |
| `MapInboundClaims` | `false` | Disabled to prevent claim type remapping by the JWT handler. |

### Fail-Fast Validation

JWT configuration is loaded from ConfigurationDb and validated at startup:

```csharp
var jwtConfig = builder.Configuration.GetSection("Authentication:Jwt")
    .Get<List<JwtAuthenticationConfiguration>>()?.FirstOrDefault();

if (jwtConfig is null)
{
    ProgramLog.JwtConfigurationMissing(startupLogger);  // FDW-NNNN (Critical)
    return 1;
}
```

This prevents the server from starting without authentication configuration, which would leave all endpoints unprotected.

### Middleware Pipeline Order

The correct middleware order is critical for security:

```csharp
app.UseFrameworkMiddleware();                   // 1. Exception handler + HTTPS + Security Headers + Logging
app.UseCors();                            // 2. CORS (preflight must pass before auth)
app.UseAuthentication();                  // 3. JWT Bearer validation
app.UseAuthorization();                   // 4. Role/policy authorization
app.UseMultitenancy();                    // 5. Tenant context (needs auth claims)
app.UseRateLimiter();                     // 6. Rate limiting
app.UseFastEndpoints();                   // 7. Endpoint routing
```

### Production Checklist

| Item | Development | Production |
|------|-------------|------------|
| JWT SecretKey | Hardcoded in appsettings | Environment variable or secret store |
| AccessTokenExpiration | 60 minutes | 15-30 minutes |
| HTTPS | .NET dev certificate | Proper TLS certificate |
| HSTS | Disabled | Enabled (automatic in non-Development) |
| Internal API Key | `dev-internal-api-key-change-in-production` | Cryptographically random, 32+ bytes |
| CORS Origins | `localhost:5007` | Exact production domain |
| SQL Passwords | Docker compose env vars | Secret manager injection |
| Seq Authentication | Disabled (`NOAUTHENTICATION`) | Enabled with API keys |
| CSP | Default (includes `unsafe-inline`) | Tighten per application needs |

## Related Documentation

- [12-01 Creating a Server](12-01-Creating-A-Server.md) -- Extension method reference and `SecurityHeadersOptions`
- [12-02 Deployment Guide](12-02-Deployment-Guide.md) -- Docker setup, environment variables, production checklist
- [12-03 Service Communication](12-03-Service-Communication.md) -- API gateway proxy pattern, internal API keys, webhooks
- [08-02 Database Schema](08-02-Database-Schema.md) -- ConfigurationDb structure (84 tables across 9 schemas)
