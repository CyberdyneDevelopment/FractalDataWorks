using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Property name is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "PropertyNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PropertyNameRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyNameRequiredCode"/> class.
    /// </summary>
    public PropertyNameRequiredCode()
        : base(21010, "PropertyNameRequired",
            ResultSeverities.ByName("Error"),
            "Property name is required",
            isRetryable: false)
    {
    }
}
