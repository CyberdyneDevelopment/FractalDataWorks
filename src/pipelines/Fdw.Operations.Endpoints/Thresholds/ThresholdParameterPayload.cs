using System.Text.Json;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>A parameter of a threshold (FDW-3948): how its type's measure is taken, such as the field a quality rule reads or the limits of a range.</summary>
public sealed class ThresholdParameterPayload
{
    /// <summary>Gets or sets the parameter name, one the threshold type declares.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value: one scalar, or an array of scalars for a parameter the type declares as a list. Its type is the type the threshold type declares.</summary>
    public JsonElement Value { get; set; }
}
