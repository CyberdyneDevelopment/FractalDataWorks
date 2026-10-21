using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The implementing guide document was not found under the guides path.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "GuideDocumentNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GuideDocumentNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuideDocumentNotFoundCode"/> class.
    /// </summary>
    public GuideDocumentNotFoundCode()
        : base(31035, "GuideDocumentNotFound",
            ResultSeverities.ByName("Error"),
            "Guide document not found: {GuidesPath}",
            isRetryable: false)
    {
    }
}
