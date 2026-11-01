using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>What a batch did: the runner's result and the tokens it wrote.</summary>
internal sealed class BatchOutcome
{
    public BatchOutcome(IGenericResult result, ResponseTokens tokens)
    {
        Result = result;
        Tokens = tokens;
    }

    public IGenericResult Result { get; }

    public ResponseTokens Tokens { get; }

    /// <summary>Gets the SQL Server error the batch was refused with, or null when it ran.</summary>
    public SqlServerMessage? Refusal => SqlRefusal.TryGetMessage(Result, out var message) ? message : null;
}
