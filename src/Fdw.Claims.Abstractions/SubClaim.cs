using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>Subject — the FDW user GUID (drives RLS via SESSION_CONTEXT). Standard JWT <c>sub</c>.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "sub")]
public sealed class SubClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="SubClaim"/> class.</summary>
    public SubClaim() : base(name: "sub", isArray: false, TokenDestinations.AccessToken) { }
}
