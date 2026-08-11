using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.UiServiceTypeOptions;

/// <summary>
/// The UI domains a skin serves.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(UiServiceTypeBase),
    typeof(IUiServiceType),
    typeof(UiServiceTypes),
    ServiceCategory = "UiService")]
public partial class UiServiceTypes : ServiceTypeCollectionBase<UiServiceTypeBase, IUiServiceType>
{
}
