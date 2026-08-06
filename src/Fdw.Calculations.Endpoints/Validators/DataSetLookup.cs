using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Calculations.Endpoints.Validators;

/// <summary>
/// Reusable lookup helpers for DataSet-bound request fields. Endpoint code and validators that
/// need to verify a DataSet name resolves can share the same query path.
/// </summary>
public static class DataSetLookup
{
    /// <summary>
    /// Returns true when the supplied name resolves to a registered DataSet in
    /// <c>data.DataSet</c> (IsCurrent + non-deleted). Empty names yield true so callers can
    /// guard with <c>name.Length &gt; 0 &amp;&amp; await Exists(...)</c>.
    /// </summary>
    public static async Task<bool> Exists(
        IConfigurationGateway configGateway,
        string name,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(name)) return true;

        var cmd = DataQuery.From<Dictionary<string, object?>>("ConfigurationDb", "data", "DataSet")
            .Where("Name", name)
            .Build();
        var result = await configGateway.Execute<IEnumerable<Dictionary<string, object?>>>(cmd, ct).ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null && result.Value.Any();
    }
}
