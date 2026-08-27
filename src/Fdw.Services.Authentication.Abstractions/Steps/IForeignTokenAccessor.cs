namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Supplies the externally-issued token the caller presented.
/// </summary>
/// <remarks>
/// A seam rather than a parameter, because a step's signature is fixed by the pipeline. A host binds
/// this to wherever the token actually arrives — a form field on the exchange grant, a header, a
/// request body — without the step knowing which.
/// </remarks>
public interface IForeignTokenAccessor
{
    /// <summary>Gets the presented token, or null when none was.</summary>
    string? Token { get; }
}
