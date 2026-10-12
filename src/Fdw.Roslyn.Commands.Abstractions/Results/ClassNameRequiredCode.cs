using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Class name is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "ClassNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ClassNameRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassNameRequiredCode"/> class.
    /// </summary>
    public ClassNameRequiredCode()
        : base(20000, "ClassNameRequired",
            ResultSeverities.ByName("Error"),
            "Class name is required",
            isRetryable: false)
    {
    }
}
