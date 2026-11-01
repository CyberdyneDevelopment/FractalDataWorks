using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Document not found.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "DocumentNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentNotFoundCode"/> class.
    /// </summary>
    public DocumentNotFoundCode()
        : base(31001, "DocumentNotFound",
            ResultSeverities.ByName("Error"),
            "Document not found: {FilePath}",
            isRetryable: false)
    {
    }
}
