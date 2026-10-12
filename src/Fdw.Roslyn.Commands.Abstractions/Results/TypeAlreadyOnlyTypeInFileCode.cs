using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Type is already the only type in its file.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TypeAlreadyOnlyTypeInFile", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TypeAlreadyOnlyTypeInFileCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeAlreadyOnlyTypeInFileCode"/> class.
    /// </summary>
    public TypeAlreadyOnlyTypeInFileCode()
        : base(40001, "TypeAlreadyOnlyTypeInFile",
            ResultSeverities.ByName("Error"),
            "Type is already the only type in its file",
            isRetryable: false)
    {
    }
}
