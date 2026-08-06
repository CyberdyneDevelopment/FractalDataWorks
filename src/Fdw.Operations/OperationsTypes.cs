using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Operations;

/// <summary>
/// ServiceTypeCollection for operations domain service types.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(OperationsServiceTypeBase),
    typeof(IOperationsServiceType),
    typeof(OperationsTypes),
    ServiceCategory = "Operations",
    RestrictToCurrentCompilation = true)]
public partial class OperationsTypes : ServiceTypeCollectionBase<OperationsServiceTypeBase, IOperationsServiceType>
{
}
