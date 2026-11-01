using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The rewrite would leave affected projects unable to compile.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "ChangeWouldNotCompile", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ChangeWouldNotCompileCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeWouldNotCompileCode"/> class.
    /// </summary>
    public ChangeWouldNotCompileCode()
        : base(31030, "ChangeWouldNotCompile",
            ResultSeverities.ByName("Error"),
            "Change would leave {CollisionCount} collision(s) and {UnresolvedCount} unresolved reference(s); first: {First}. Re-run with DryRun to see them all",
            isRetryable: false)
    {
    }
}
