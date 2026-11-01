using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No global using in the project matched the requested namespaces.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoGlobalUsingsMatched", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoGlobalUsingsMatchedCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoGlobalUsingsMatchedCode"/> class.
    /// </summary>
    public NoGlobalUsingsMatchedCode()
        : base(31034, "NoGlobalUsingsMatched",
            ResultSeverities.ByName("Error"),
            "No global using matching {Namespaces} found in project '{Project}'",
            isRetryable: false)
    {
    }
}
