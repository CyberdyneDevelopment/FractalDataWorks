using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Schema.Ddl.Results;

/// <summary>
/// No DDL commands were generated from the schema.
/// </summary>
[TypeOption(typeof(DdlResultCodes), "NoCommandsGenerated", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoCommandsGeneratedCode : DdlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoCommandsGeneratedCode"/> class.
    /// </summary>
    public NoCommandsGeneratedCode()
        : base(30000, "NoCommandsGenerated",
            ResultSeverities.ByName("Warning"),
            "No DDL commands were generated",
            isRetryable: false)
    {
    }
}