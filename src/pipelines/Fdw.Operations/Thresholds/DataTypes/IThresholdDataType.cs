using System.Text.Json.Nodes;
using Fdw.Collections;

namespace Fdw.Operations.Thresholds;

/// <summary>A data type a threshold's stored value text can state.</summary>
public interface IThresholdDataType : ITypeOption<int, ThresholdDataTypeBase>
{
    /// <summary>Reads the text as a value of this type.</summary>
    /// <param name="text">The stored text.</param>
    /// <returns>The value, or <c>null</c> when the text does not read as this type.</returns>
    object? Read(string text);

    /// <summary>Shows the text as the JSON scalar of this type.</summary>
    /// <param name="text">The stored text.</param>
    /// <returns>The scalar; the text itself when it does not read as this type, so the stored value is shown as it is.</returns>
    JsonValue? Scalar(string text);
}
