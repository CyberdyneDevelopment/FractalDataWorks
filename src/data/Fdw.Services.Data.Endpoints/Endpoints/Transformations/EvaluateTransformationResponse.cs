namespace Fdw.Services.Data.Endpoints;

/// <summary>What a Built transformation evaluates to, and the kind of value it is.</summary>
public class EvaluateTransformationResponse
{
    /// <summary>Gets or sets the value: a JSON number for a number, an ISO 8601 text for a date-time.</summary>
    public object Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of value: <c>Number</c> or <c>DateTime</c>.</summary>
    public string ValueType { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type name a data set field of that kind states: <c>Decimal</c> or <c>DateTimeOffset</c>.</summary>
    public string DataType { get; set; } = string.Empty;
}
