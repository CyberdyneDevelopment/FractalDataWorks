using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Method name is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "MethodNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MethodNameRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MethodNameRequiredCode"/> class.
    /// </summary>
    public MethodNameRequiredCode()
        : base(21006, "MethodNameRequired",
            ResultSeverities.ByName("Error"),
            "Method name is required",
            isRetryable: false)
    {
    }
}
