# Fdw.Services.Users.Abstractions

The user contracts and the `IUser` model.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IUser` | interface | User interface. |
| `IUserCredentialService` | interface | Service for managing user credentials stored in auth.UserSecret. Handles hashing at the service boundary… |

## Base types (2)

| Type | Kind | Purpose |
|---|---|---|
| `UserResultCodeBase` | class | Base class for User result codes. |
| `UserResultCodes` | class | TypeCollection for User result codes. EventId range: 7850-7899 |

## Models and supporting types (14)

| Type | Kind | Purpose |
|---|---|---|
| `AssignRoleRequest` | class | Data transfer object for assigning a role to a user. |
| `CreateUserRequest` | class | Data transfer object for creating a new user. |
| `InvalidCredentialsCode` | class | Invalid credentials. |
| `MissingTenantClaimCode` | class | The caller has no tenant_id JWT claim — every user is tenant-scoped, so creating a user without a tenant… |
| `PasswordPolicyOptions` | class | Configuration options for password policy. |
| `QueryFailedCode` | class | Query failed. |
| `UpdateUserPayload` | class | Data transfer object payload for updating an existing user. |
| `UserAlreadyExistsCode` | class | User already exists. |
| `UserDetailPayload` | class | Represents detailed information about a user. |
| `UserInactiveCode` | class | User is inactive. |
| `UserInfo` | class | User information implementation. |
| `UserNotFoundCode` | class | User not found. |

## Installation

```bash
dotnet add package Fdw.Services.Users.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Data.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services.Credentials.Abstractions` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Data.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
