using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The named type is not declared in any project of the loaded solution.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TypeNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TypeNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeNotFoundCode"/> class.
    /// </summary>
    public TypeNotFoundCode()
        : base(31036, "TypeNotFound",
            ResultSeverities.ByName("Error"),
            "Type not found in the loaded solution: {TypeName}",
            isRetryable: false)
    {
    }
}
