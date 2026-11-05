using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Schema.Ddl.Results;

/// <summary>
/// A foreign key asks for a referential action the target dialect cannot express.
/// </summary>
/// <remarks>
/// The generator refuses rather than writing a different action: SQL Server has no RESTRICT, and writing
/// NO ACTION in its place changes when the check runs (FDW-3610).
/// </remarks>
[TypeOption(typeof(DdlResultCodes), "ForeignKeyActionNotSupported", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ForeignKeyActionNotSupportedCode : DdlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKeyActionNotSupportedCode"/> class.
    /// </summary>
    public ForeignKeyActionNotSupportedCode()
        : base(20001, "ForeignKeyActionNotSupported",
            ResultSeverities.ByName("Error"),
            "Foreign key '{constraint}' asks for {clause} {action}, which {dialect} cannot express",
            isRetryable: false)
    {
    }
}
