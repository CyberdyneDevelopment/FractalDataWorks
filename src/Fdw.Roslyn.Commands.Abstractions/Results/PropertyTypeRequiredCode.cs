using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Property type is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "PropertyTypeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PropertyTypeRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyTypeRequiredCode"/> class.
    /// </summary>
    public PropertyTypeRequiredCode()
        : base(21011, "PropertyTypeRequired",
            ResultSeverities.ByName("Error"),
            "Property type is required",
            isRetryable: false)
    {
    }
}
