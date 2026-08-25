using Fdw.Collections.Attributes;
using Fdw.UI.Navigation;

namespace Fdw.Data.UI.Pages;

/// <summary>
/// Contributes this package's Data pages to <see cref="PageTypes"/>.
/// </summary>
[TypeOption(typeof(PageTypes), "Data")]
public sealed class DataPageType : PageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPageType"/> class.
    /// </summary>
    public DataPageType()
        : base(4, "Data",
        [
            new Page("DataPreview", typeof(global::Fdw.UI.Pages.Data.Pages.DataPreviewPage), new NavItem("Data Preview", "eye", NavSections.DataSources, 10), PageAccess.Authenticated),
            new Page("DataSetDetail", typeof(global::Fdw.UI.Pages.Data.Pages.DataSetDetailPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("DataSetWizard", typeof(global::Fdw.UI.Pages.Data.Pages.DataSetWizardPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("DataSets", typeof(global::Fdw.UI.Pages.Data.Pages.DataSetsPage), new NavItem("Data Sets", "table", NavSections.DataSources, 10), PageAccess.RequiringPermission("datasets:read")),
            new Page("DataStoreDetail", typeof(global::Fdw.UI.Pages.Data.Pages.DataStoreDetailPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("DataStoreEditor", typeof(global::Fdw.UI.Pages.Data.Pages.DataStoreEditorPage), NavItem.Empty, PageAccess.Authenticated),
            new Page("DataStores", typeof(global::Fdw.UI.Pages.Data.Pages.DataStoresPage), new NavItem("Data Stores", "database", NavSections.DataSources, 10), PageAccess.RequiringPermission("datastores:read")),
            new Page("Mapper", typeof(global::Fdw.UI.Pages.Data.Pages.MapperPage), new NavItem("Field Mapper", "shuffle", NavSections.DataSources, 10), PageAccess.RequiringPermission("field-mapping-transforms:read")),
            new Page("Visualize", typeof(global::Fdw.UI.Pages.Data.Pages.VisualizePage), new NavItem("Visualize", "bar-chart-2", NavSections.DataSources, 10), PageAccess.Authenticated),
        ])
    { }
}
