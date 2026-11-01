using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Results;

/// <summary>
/// Source code was null or empty.
/// </summary>
[TypeOption(typeof(CodeBuilderCSharpResultCodes), "SourceCodeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceCodeRequiredCode : CodeBuilderCSharpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceCodeRequiredCode"/> class.
    /// </summary>
    public SourceCodeRequiredCode()
        : base(20000, "SourceCodeRequired",
            ResultSeverities.ByName("Error"),
            "Source code cannot be null or empty",
            isRetryable: false)
    {
    }
}
