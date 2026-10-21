using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The implementing guide document is not valid guide JSON.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "GuideDocumentInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GuideDocumentInvalidCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuideDocumentInvalidCode"/> class.
    /// </summary>
    public GuideDocumentInvalidCode()
        : base(90003, "GuideDocumentInvalid",
            ResultSeverities.ByName("Error"),
            "Guide document could not be parsed: {GuidesPath}",
            isRetryable: false)
    {
    }
}
