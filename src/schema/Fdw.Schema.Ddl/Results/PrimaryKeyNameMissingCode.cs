using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Schema.Ddl.Results;

/// <summary>
/// A CREATE TABLE command names primary-key columns but no constraint name. The name is the command's to give;
/// the generator does not invent one (FDW-3580: PK_{table} used to stand in for it).
/// </summary>
[TypeOption(typeof(DdlResultCodes), "PrimaryKeyNameMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PrimaryKeyNameMissingCode : DdlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrimaryKeyNameMissingCode"/> class.
    /// </summary>
    public PrimaryKeyNameMissingCode()
        : base(90001, "PrimaryKeyNameMissing",
            ResultSeverities.ByName("Error"),
            "Table '{tableName}' names primary-key columns but no primary-key constraint name",
            isRetryable: false)
    {
    }
}
