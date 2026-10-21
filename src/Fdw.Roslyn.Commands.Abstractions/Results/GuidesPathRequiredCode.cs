using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// GuidesPath must be provided.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "GuidesPathRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GuidesPathRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidesPathRequiredCode"/> class.
    /// </summary>
    public GuidesPathRequiredCode()
        : base(21024, "GuidesPathRequired",
            ResultSeverities.ByName("Error"),
            "GuidesPath is required: the folder holding the implementing guide document",
            isRetryable: false)
    {
    }
}
