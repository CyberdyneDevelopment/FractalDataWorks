using Fdw.Collections;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>Base class for page actions.</summary>
public abstract class PageActionBase : TypeOptionBase<PageActionBase>, IPageActionType
{
    /// <summary>Initializes a new instance of <see cref="PageActionBase"/>.</summary>
    protected PageActionBase(string name) : base(name) { }
}
