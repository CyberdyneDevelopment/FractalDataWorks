namespace Fdw.Data.Components.DataPreview;

using Fdw.Collections.Attributes;

/// <summary>Preview data from a DataStore container.</summary>
[TypeOption(typeof(PreviewModes), "DataStore")]
public sealed class DataStorePreviewMode : PreviewModeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataStorePreviewMode"/> class.</summary>
    public DataStorePreviewMode() : base(1, "DataStore") { }
}
