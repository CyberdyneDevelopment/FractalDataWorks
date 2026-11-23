using System.Text.Json.Nodes;
using Fdw.Collections;

namespace Fdw.Operations.Thresholds;

/// <summary>Base class for the data types a threshold's stored value text can state.</summary>
public abstract class ThresholdDataTypeBase : TypeOptionBase<ThresholdDataTypeBase>, IThresholdDataType
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdDataTypeBase"/> class.</summary>
    /// <param name="name">The option name, the word a stored row names its data type by.</param>
    protected ThresholdDataTypeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract object? Read(string text);

    /// <inheritdoc />
    public virtual JsonValue? Scalar(string text) => JsonValue.Create(text);
}
