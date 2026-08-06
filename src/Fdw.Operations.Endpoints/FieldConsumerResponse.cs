namespace Fdw.Operations.Endpoints;

/// <summary>
/// A downstream consumer of a field: another dataset's field, a pipeline step,
/// or a calculation that reads this field's value. Populated by the server
/// scanning DataSetFieldMapping, PipelineTransformFieldMapping, and CalculationInput
/// for references to the source field's RowId.
/// </summary>
public class FieldConsumerResponse
{
    /// <summary>
    /// The kind of consumer: <c>"DataSet"</c>, <c>"Pipeline"</c>, or <c>"Calculation"</c>.
    /// Drives which columns are meaningful on the consumer side.
    /// </summary>
    public string ConsumerKind { get; set; } = string.Empty;

    /// <summary>
    /// Name of the consuming dataset, pipeline, or calculation entity.
    /// </summary>
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the specific consumer field/column, if applicable (a DataSet field
    /// name, a pipeline transform output column, or a calculation output column).
    /// </summary>
    public string ConsumerField { get; set; } = string.Empty;
}
