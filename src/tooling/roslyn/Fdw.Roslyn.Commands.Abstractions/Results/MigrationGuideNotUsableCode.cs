using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The published migration guide named by GuidePath does not exist, or records no assembly moves.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "MigrationGuideNotUsable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MigrationGuideNotUsableCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationGuideNotUsableCode"/> class.
    /// </summary>
    public MigrationGuideNotUsableCode()
        : base(31028, "MigrationGuideNotUsable",
            ResultSeverities.ByName("Error"),
            "Migration guide '{GuidePath}' {Problem}",
            isRetryable: false)
    {
    }
}
