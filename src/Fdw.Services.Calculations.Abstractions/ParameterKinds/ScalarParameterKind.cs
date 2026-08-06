using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Parameter kind representing a single scalar value (number, string, etc.).
/// </summary>
[TypeOption(typeof(OperationParameterKinds), "Scalar")]
[ExcludeFromCodeCoverage]
public sealed class ScalarParameterKind : OperationParameterKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScalarParameterKind"/> class.
    /// </summary>
    public ScalarParameterKind()
        : base(id: 1, name: "Scalar", description: "A single scalar value (number, string)")
    {
    }
}
