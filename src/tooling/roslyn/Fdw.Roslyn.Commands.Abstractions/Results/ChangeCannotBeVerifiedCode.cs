using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The change could not be verified because an affected project's compilation cannot bind.
/// </summary>
/// <remarks>
/// Distinct from ChangeWouldNotCompile on purpose. That one means "your change is wrong"; this means
/// "nobody can tell whether your change is wrong". Reporting the second as the first sends the caller
/// hunting for a defect in their own edit that may not exist.
/// </remarks>
[TypeOption(typeof(RoslynResultCodes), "ChangeCannotBeVerified", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ChangeCannotBeVerifiedCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeCannotBeVerifiedCode"/> class.
    /// </summary>
    public ChangeCannotBeVerifiedCode()
        : base(31031, "ChangeCannotBeVerified",
            ResultSeverities.ByName("Error"),
            "{ProjectCount} affected project(s) cannot be verified: {Detail}. Restore/build the solution and reload, then re-run",
            isRetryable: true)
    {
    }
}
