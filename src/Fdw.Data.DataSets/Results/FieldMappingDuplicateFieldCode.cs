using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set source carries two field mappings for the same data set field. A mapping is
/// identified within its source by the field it fills, so two of them would be one identity with
/// two meanings. Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "FieldMappingDuplicateField", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldMappingDuplicateFieldCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldMappingDuplicateFieldCode"/> class.
    /// </summary>
    public FieldMappingDuplicateFieldCode()
        : base(20012, "FieldMappingDuplicateField",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: source '{sourceName}' maps field '{fieldName}' more than once",
            isRetryable: false)
    {
    }
}
