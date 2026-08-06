using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// A known FDW JWT claim type. Implemented by <see cref="ClaimDefinitionBase"/> and every concrete
/// claim, enabling source-generated discovery via the <c>ClaimDefinitions</c> TypeCollection so any
/// assembly can contribute new claims with their own baking metadata (no FDW edit required).
/// </summary>
public interface IClaimDefinition : ITypeOption<int, IClaimDefinition>
{
    /// <summary>
    /// When true, the claim is serialized as a JSON array even for a single value
    /// (e.g. <c>roles</c>) so clients never have to branch on scalar-vs-array.
    /// </summary>
    bool IsArray { get; }

    /// <summary>
    /// The token destination name(s) the claim is written to (see <see cref="TokenDestinations"/>).
    /// </summary>
    IReadOnlyList<string> Destinations { get; }
}
