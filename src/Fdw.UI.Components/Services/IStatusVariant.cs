using Fdw.Collections;

namespace Fdw.UI.Components.Services;

/// <summary>
/// Interface for semantic status variants.
/// </summary>
public interface IStatusVariant : ITypeOption<int, StatusVariantBase>
{
    /// <summary>
    /// Gets the css class that colours a badge of this variant, e.g. <c>b-ok</c>. It is the
    /// modifier alone; the <c>badge</c> class that shapes the pill comes from the component.
    /// </summary>
    string BadgeClass { get; }
}
