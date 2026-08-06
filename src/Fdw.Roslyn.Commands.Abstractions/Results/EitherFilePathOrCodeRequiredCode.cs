using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Either FilePath or Code must be provided.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "EitherFilePathOrCodeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EitherFilePathOrCodeRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EitherFilePathOrCodeRequiredCode"/> class.
    /// </summary>
    public EitherFilePathOrCodeRequiredCode()
        : base(21002, "EitherFilePathOrCodeRequired",
            ResultSeverities.ByName("Error"),
            "Either FilePath or Code must be provided",
            isRetryable: false)
    {
    }
}
