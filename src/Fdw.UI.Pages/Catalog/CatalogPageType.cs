using Fdw.Collections.Attributes;
using Fdw.UI.Registration;

namespace Fdw.Services.Catalog.UI.Pages;

/// <summary>
/// Contributes this package's Catalog pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Catalog")]
public sealed class CatalogPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogPageType"/> class.
    /// </summary>
    public CatalogPageType()
        : base(9, "Catalog",
        [
            new Page("Catalog", typeof(global::Fdw.UI.Pages.Catalog.Pages.CatalogPage), new NavItem("Catalog", "book", NavSections.Catalog, 60), PageAccess.Authenticated),
            new Page("Index", typeof(global::Fdw.UI.Pages.Catalog.Pages.Glossary.GlossaryIndexPage), new NavItem("Glossary", "bookmark", NavSections.Catalog, 60), PageAccess.Authenticated),
        ])
    { }
}
