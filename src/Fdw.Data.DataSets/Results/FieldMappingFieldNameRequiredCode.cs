using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A field mapping on a data set source names no data set field. The field a mapping fills is its
/// identity within the source, so a mapping without one cannot be stored. Caller-input validation
/// failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "FieldMappingFieldNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldMappingFieldNameRequiredCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldMappingFieldNameRequiredCode"/> class.
    /// </summary>
    public FieldMappingFieldNameRequiredCode()
        : base(20011, "FieldMappingFieldNameRequired",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: a field mapping on source '{sourceName}' is missing dataSetFieldName",
            isRetryable: false)
    {
    }
}
