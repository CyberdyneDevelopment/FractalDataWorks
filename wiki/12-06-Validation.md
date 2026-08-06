# 12-06 Validation

This guide covers the FDW validation strategy. Validation is applied at three layers: client-side (DataAnnotations), API endpoints (FluentValidation), and configuration startup (IValidateOptions).

## Architecture

```
Client (Blazor)                 API Endpoint                  Configuration (Startup)
     |                               |                               |
DataAnnotationsValidator        Validator<T>                  FdwConfigurationValidator<T>
     |                               |                               |
EditForm validation             FastEndpoints auto-discovery  IValidateOptions<T>
     |                               |                               |
Immediate UI feedback           400 ProblemDetails response   Fail-fast on app.Run()
```

## Packages

| Package | Target | Purpose |
|---------|--------|---------|
| `Fdw.Validation.Abstractions` | netstandard2.0 | `IEntityValidator<T>` interface (no FluentValidation dependency) |
| `Fdw.Validation` | net10.0 | Base validators, common rules, DI integration, GenericResult bridge |
| `Fdw.Validation.FastEndpoints` | net10.0 | `FdwEndpointValidator<T>` base for FastEndpoints auto-discovery |

## Layer 1: API Endpoint Validation (FluentValidation)

FastEndpoints auto-discovers validators that inherit `Validator<TRequest>`. Validation runs before the endpoint handler -- if validation fails, FastEndpoints returns a 400 response with a ProblemDetails body automatically.

### Creating an Endpoint Validator

```csharp
using FastEndpoints;
using FluentValidation;

namespace Reference.Api.Validators;

public sealed class CreateConnectionRequestValidator : Validator<CreateConnectionRequest>
{
    public CreateConnectionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Connection name is required")
            .MaximumLength(100)
            .WithMessage("Connection name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Connection name must start with a letter and contain only letters, numbers, underscores, or hyphens");

        RuleFor(x => x.Server)
            .NotEmpty()
            .WithMessage("Server hostname or IP address is required");
    }
}
```

No registration code is needed -- FastEndpoints discovers validators by convention when the request type matches.

### Using FDW Common Rules

The `FdwValidationRules` extension methods provide reusable rules:

```csharp
using Fdw.Validation;

public sealed class CreatePipelineRequestValidator : Validator<CreatePipelineRequest>
{
    public CreatePipelineRequestValidator()
    {
        RuleFor(x => x.Name).IsValidName(100);
        RuleFor(x => x.Id).IsNotEmpty();
        RuleFor(x => x.Description).IsSafeString(500);
    }
}
```

### Available Common Rules

| Rule | Applies To | Behavior |
|------|-----------|----------|
| `IsValidName(maxLength)` | `string` | Required, starts with letter, alphanumeric + hyphens/underscores |
| `IsNotEmpty()` | `Guid` | Must not be `Guid.Empty` |
| `IsSafeString(maxLength)` | `string` | No control characters, max length |
| `IsValidConnectionString()` | `string` | Required, no SQL injection patterns |
| `IsValidCronExpression()` | `string` | Valid 5-6 part cron format |

### Using FdwEndpointValidator Base Class

For validators that need the common helper methods:

```csharp
using Fdw.Validation.FastEndpoints;

public sealed class CreateUserRequestValidator : FdwEndpointValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        ValidateName(x => x.Username, 100);
        ValidatePassword(x => x.Password);
        ValidateEmail(x => x.Email);
    }
}
```

## Layer 2: Configuration Validation (IValidateOptions)

Configuration loaded from the database should be validated at startup to fail fast on missing or invalid values.

### Creating a Configuration Validator

```csharp
using FluentValidation;
using Fdw.Validation;

public sealed class MsSqlConnectionConfigurationValidator
    : FdwConfigurationValidator<MsSqlConnectionConfiguration>
{
    public MsSqlConnectionConfigurationValidator()
    {
        RuleFor(x => x.Server)
            .NotEmpty()
            .WithMessage("Server is required for MsSql connections");

        RuleFor(x => x.Database)
            .NotEmpty()
            .WithMessage("Database is required for MsSql connections");
    }
}
```

### Registering for Startup Validation

In `Program.cs`, wire the validator to the options pipeline:

```csharp
services.AddOptions<MsSqlConnectionConfiguration>()
    .Bind(section)
    .ValidateWithFdw<MsSqlConnectionConfiguration, MsSqlConnectionConfigurationValidator>()
    .ValidateOnStart();
```

`ValidateWithFdw` registers the validator as an `IValidateOptions<T>`. Combined with `ValidateOnStart()`, the application will throw on startup if configuration is invalid.

## Layer 3: Client-Side Validation (DataAnnotations)

Blazor forms use DataAnnotations for immediate client-side feedback. Add attributes to request DTOs:

```csharp
using System.ComponentModel.DataAnnotations;

public sealed class CreateUserRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";

    [EmailAddress]
    public string? Email { get; set; }

    public List<string> Roles { get; set; } = [];
}
```

In the Blazor component:

```razor
<EditForm Model="@_request" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />
    <!-- form fields -->
</EditForm>
```

## Validation Error Flow

```
1. Client submits form
   → DataAnnotationsValidator catches missing/invalid fields
   → User sees inline validation errors immediately

2. Valid form POSTs to API
   → FastEndpoints runs Validator<TRequest> before handler
   → Invalid: returns 400 with ProblemDetails JSON
   → Valid: handler executes

3. ProblemDetails response example:
   {
     "statusCode": 400,
     "message": "One or more errors occurred!",
     "errors": {
       "name": ["Connection name is required"],
       "server": ["Server hostname or IP address is required"]
     }
   }

4. UI ApiClient receives 400
   → Displays error messages from response body
```

## GenericResult Integration

For service-layer validation (outside FastEndpoints), convert FluentValidation results to `IGenericResult`:

```csharp
using Fdw.Validation;

var validator = new CreateConnectionRequestValidator();
var result = validator.Validate(request);

IGenericResult genericResult = result.ToGenericResult();
// or with a value:
IGenericResult<Connection> typedResult = result.ToGenericResult(connection);
```

## IEntityValidator Interface

For Abstractions packages (netstandard2.0) that need to declare validation contracts without depending on FluentValidation:

```csharp
using Fdw.Validation.Abstractions;

public interface IConnectionService
{
    IGenericResult Create(ConnectionRequest request, IEntityValidator<ConnectionRequest> validator);
}
```

## DI Registration

Register all validators from an assembly:

```csharp
services.AddFrameworkValidation(typeof(Program).Assembly);
```

This scans the assembly for all `AbstractValidator<T>` implementations and registers them as scoped services. Note: FastEndpoints validators (`Validator<T>`) are auto-discovered separately by FastEndpoints and do not need this call.
