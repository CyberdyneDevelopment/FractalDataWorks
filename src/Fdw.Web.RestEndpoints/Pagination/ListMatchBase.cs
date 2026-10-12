using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>Base class for the ways a list's search text is compared with a name.</summary>
public abstract class ListMatchBase : TypeOptionBase<ListMatchBase>, IListMatch
{
    private readonly string _operatorName;

    /// <summary>Initializes a new instance of the <see cref="ListMatchBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="wire">The word a request names the match by.</param>
    /// <param name="operatorName">The name of the filter operator that states the comparison.</param>
    protected ListMatchBase(string name, string wire, string operatorName)
        : base(name)
    {
        Wire = wire;
        _operatorName = operatorName;
    }

    /// <inheritdoc />
    [TypeLookup("ByWire")]
    public string Wire { get; }

    /// <inheritdoc />
    public IFilterOperator Operator => FilterOperators.ByName(_operatorName);
}
