using System;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// How the shared query translator's refusals reach a SQL Server client: as error 245, with the
/// translator's own reason, since every one is a value that does not fit the column it is compared with.
/// </summary>
/// <remarks>
/// The translator checks each value against the type its column declares before any query runs — the same
/// check the HTTP query route applies — so a WHERE that compares a date column with text that is not a date
/// is refused here, not by the store.
/// </remarks>
internal sealed class SqlQueryRefusals : IQueryRefusals
{
    private readonly SqlScope _scope;
    private readonly int _line;

    /// <summary>Initializes a new instance of the <see cref="SqlQueryRefusals"/> class.</summary>
    /// <param name="scope">The query's scope, for the logger.</param>
    /// <param name="line">The line of the statement translated.</param>
    public SqlQueryRefusals(SqlScope scope, int line)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _line = line;
    }

    /// <inheritdoc/>
    public IGenericResult<T> Invalid<T>(string reason)
        => SqlRefusal.Refuse<T>(new SqlServerMessage(245, 2, 16, $"Conversion failed: {reason}.", _line), _scope.Context.Logger);
}
