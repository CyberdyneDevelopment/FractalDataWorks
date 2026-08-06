using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.SchemaImporters.Abstractions.Results;

/// <summary>
/// Import operation failed.
/// </summary>
[TypeOption(typeof(SchemaImporterResultCodes), "ImportFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ImportFailedCode : SchemaImporterResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportFailedCode"/> class.
    /// </summary>
    public ImportFailedCode()
        : base(70003, "ImportFailed",
            ResultSeverities.ByName("Error"),
            "Import failed",
            isRetryable: false)
    {
    }
}