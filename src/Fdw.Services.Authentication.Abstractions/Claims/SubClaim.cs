using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>Subject — the FDW user GUID (drives RLS via SESSION_CONTEXT). Standard JWT <c>sub</c>.</summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "sub")]
public sealed class SubClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="SubClaim"/> class.</summary>
    public SubClaim() : base(id: 1, name: "sub", isArray: false, TokenDestinations.AccessToken) { }
}
