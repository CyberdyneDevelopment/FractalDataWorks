using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Either FilePath or ProjectName must be provided.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "EitherFilePathOrProjectNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EitherFilePathOrProjectNameRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EitherFilePathOrProjectNameRequiredCode"/> class.
    /// </summary>
    public EitherFilePathOrProjectNameRequiredCode()
        : base(21003, "EitherFilePathOrProjectNameRequired",
            ResultSeverities.ByName("Error"),
            "Either FilePath or ProjectName must be provided",
            isRetryable: false)
    {
    }
}
