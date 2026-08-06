namespace Fdw.Data.Components.DataPreview;

using Fdw.Collections;

/// <summary>Base class for data preview mode options.</summary>
public abstract class PreviewModeBase : TypeOptionBase<int, PreviewModeBase>, IPreviewMode
{
    /// <summary>Initializes a new instance of the <see cref="PreviewModeBase"/> class.</summary>
    protected PreviewModeBase(int id, string name) : base(id, name) { }
}
