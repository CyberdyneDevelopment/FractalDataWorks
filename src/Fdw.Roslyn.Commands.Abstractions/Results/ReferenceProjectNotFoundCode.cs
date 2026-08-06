using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Reference project not found.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "ReferenceProjectNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ReferenceProjectNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceProjectNotFoundCode"/> class.
    /// </summary>
    public ReferenceProjectNotFoundCode()
        : base(31016, "ReferenceProjectNotFound",
            ResultSeverities.ByName("Error"),
            "Reference project not found: {ReferenceName}",
            isRetryable: false)
    {
    }
}
