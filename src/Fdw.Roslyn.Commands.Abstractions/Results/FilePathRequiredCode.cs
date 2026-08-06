using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// File path is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "FilePathRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FilePathRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilePathRequiredCode"/> class.
    /// </summary>
    public FilePathRequiredCode()
        : base(21004, "FilePathRequired",
            ResultSeverities.ByName("Error"),
            "File path is required",
            isRetryable: false)
    {
    }
}
