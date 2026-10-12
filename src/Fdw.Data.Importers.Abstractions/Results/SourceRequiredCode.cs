using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.SchemaImporters.Abstractions.Results;

/// <summary>
/// Source was null or empty.
/// </summary>
[TypeOption(typeof(SchemaImporterResultCodes), "SourceRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceRequiredCode : SchemaImporterResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceRequiredCode"/> class.
    /// </summary>
    public SourceRequiredCode()
        : base(20000, "SourceRequired",
            ResultSeverities.ByName("Error"),
            "Source cannot be null or empty",
            isRetryable: false)
    {
    }
}