using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>
/// Whether the custodian of a data configuration rule still holds <c>datastores:write</c> (FDW-6346): a custodian who no longer does is shown as such,
/// never hidden. The console takes the names from the tree route and holds none of them itself.
/// </summary>
[TypeCollection(typeof(DataOptionCustodianStateBase), typeof(IDataOptionCustodianState), typeof(DataOptionCustodianStates))]
public abstract partial class DataOptionCustodianStates : TypeCollectionBase<DataOptionCustodianStateBase, IDataOptionCustodianState>
{
}
