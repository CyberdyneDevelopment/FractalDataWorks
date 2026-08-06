namespace Fdw.Data.Components.DataPreview;

using Fdw.Collections.Attributes;

/// <summary>Preview data from a configured DataSet.</summary>
[TypeOption(typeof(PreviewModes), "DataSet")]
public sealed class DataSetPreviewMode : PreviewModeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetPreviewMode"/> class.</summary>
    public DataSetPreviewMode() : base(3, "DataSet") { }
}
