using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A configured step that admits only a principal holding a named permission.
/// </summary>
/// <remarks>
/// The flow provider asks each such step for its permission when the flow loads and states the result
/// on <see cref="AuthenticationFlow.RequiredPermissions"/>. Code that must answer "could this user sign
/// in through that flow?" without running it (setting a password, for example) reads the answer there,
/// instead of parsing a step's settings itself.
/// </remarks>
public interface IPermissionGatedAuthenticationStep : IConfiguredAuthenticationStep
{
    /// <summary>Reads the permission the step's settings require.</summary>
    /// <param name="configuration">The step row's Configuration column.</param>
    /// <returns>The permission name, or a failure when the settings do not name one.</returns>
    IGenericResult<string> RequiredPermission(string? configuration);
}
