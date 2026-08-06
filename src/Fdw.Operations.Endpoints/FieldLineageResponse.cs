using System;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Field-level lineage.
/// </summary>
public class FieldLineageResponse
{
    /// <summary>Gets or sets the logical field name in the DataSet.</summary>
    public string LogicalField { get; set; } = string.Empty;
    /// <summary>Gets or sets the source mappings for this field.</summary>
    public IList<FieldSourceMappingResponse> Sources { get; set; } = [];
    /// <summary>Gets or sets the downstream consumers of this field ("where used").</summary>
    // Why: Lineage UI needs forward-impact to answer "which datasets/pipelines use this field".
    // Server populates by scanning DataSetFieldMapping, PipelineTransformFieldMapping, and
    // CalculationInput for references to this field's RowId.
    public IList<FieldConsumerResponse> Consumers { get; set; } = [];
}