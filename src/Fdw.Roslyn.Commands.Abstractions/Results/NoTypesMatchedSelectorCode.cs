using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The selector matched zero types.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoTypesMatchedSelector", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoTypesMatchedSelectorCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoTypesMatchedSelectorCode"/> class.
    /// </summary>
    public NoTypesMatchedSelectorCode()
        : base(31023, "NoTypesMatchedSelector",
            ResultSeverities.ByName("Error"),
            "Selector matched zero types: {Selector}",
            isRetryable: false)
    {
    }
}
