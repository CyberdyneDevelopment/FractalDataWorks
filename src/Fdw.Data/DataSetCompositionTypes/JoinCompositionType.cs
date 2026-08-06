using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Join composition — combines multiple sources using JOIN semantics defined by <see cref="IDataSetJoin"/> entries.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetCompositionTypes), "Join", RestrictToCurrentCompilation = true)]
public sealed class JoinCompositionType : DataSetCompositionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinCompositionType"/> class.
    /// </summary>
    public JoinCompositionType()
        : base(
            id: 2,
            name: "Join",
            description: "Combines multiple sources using JOIN semantics with explicit join definitions",
            allowsMultipleSources: true,
            requiresJoins: true)
    {
    }
}
