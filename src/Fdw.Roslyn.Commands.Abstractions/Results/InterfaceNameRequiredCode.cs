using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Interface name is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "InterfaceNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InterfaceNameRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InterfaceNameRequiredCode"/> class.
    /// </summary>
    public InterfaceNameRequiredCode()
        : base(21005, "InterfaceNameRequired",
            ResultSeverities.ByName("Error"),
            "Interface name is required",
            isRetryable: false)
    {
    }
}
