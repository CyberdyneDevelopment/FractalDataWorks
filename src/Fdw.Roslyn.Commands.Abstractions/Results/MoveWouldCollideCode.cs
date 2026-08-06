using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The move would not bind cleanly — the compiler reports a name collision in an affected project.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "MoveWouldCollide", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MoveWouldCollideCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveWouldCollideCode"/> class.
    /// </summary>
    public MoveWouldCollideCode()
        : base(31029, "MoveWouldCollide",
            ResultSeverities.ByName("Error"),
            "Move would cause {CollisionCount} name collision(s); first: {FirstCollision}. Re-run with DryRun to see them all",
            isRetryable: false)
    {
    }
}
