using Fdw.Collections;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>Base class for page actions.</summary>
public abstract class PageActionBase : TypeOptionBase<int, PageActionBase>, IPageAction
{
    /// <summary>Initializes a new instance of <see cref="PageActionBase"/>.</summary>
    protected PageActionBase(int id, string name) : base(id, name) { }
}
