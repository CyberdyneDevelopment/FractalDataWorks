using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// Structural drift analysis for a family of types descending from a root.
/// </summary>
public sealed class FamilyDriftReport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FamilyDriftReport"/> class.
    /// </summary>
    public FamilyDriftReport(
        string rootType,
        int implementationCount,
        IReadOnlyList<string> implementations,
        IReadOnlyList<FamilyDriftMember> driftMembers,
        IReadOnlyList<FamilyExtensionMethod> extensionMethods)
    {
        RootType = rootType;
        ImplementationCount = implementationCount;
        Implementations = implementations;
        DriftMembers = driftMembers;
        ExtensionMethods = extensionMethods;
    }

    /// <summary>Gets the family root type.</summary>
    public string RootType { get; }

    /// <summary>Gets the count of implementations analyzed.</summary>
    public int ImplementationCount { get; }

    /// <summary>Gets the simple names of all implementations analyzed.</summary>
    public IReadOnlyList<string> Implementations { get; }

    /// <summary>Gets the divergent members grouped by drift bucket.</summary>
    public IReadOnlyList<FamilyDriftMember> DriftMembers { get; }

    /// <summary>Gets the extension methods that target the family root or any derived type.</summary>
    public IReadOnlyList<FamilyExtensionMethod> ExtensionMethods { get; }
}
