using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Either TypeName or ProjectName must be provided.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "EitherTypeNameOrProjectNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EitherTypeNameOrProjectNameRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EitherTypeNameOrProjectNameRequiredCode"/> class.
    /// </summary>
    public EitherTypeNameOrProjectNameRequiredCode()
        : base(21025, "EitherTypeNameOrProjectNameRequired",
            ResultSeverities.ByName("Error"),
            "Either TypeName or ProjectName must be provided",
            isRetryable: false)
    {
    }
}
