using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Results;

/// <summary>
/// A statement the client sent was refused, and the refusal is a SQL Server error the client receives.
/// </summary>
/// <remarks>
/// 20001 (InvalidInput): the statement is the client's, and it is not one this server can run as
/// written — a syntax error, an unknown object, an unsupported keyword, a write. The SQL Server error
/// travels in the result's details under <see cref="Sql.SqlRefusal.MessageKey"/>, so the batch runner
/// sends the client exactly the error SQL Server would have.
/// </remarks>
[TypeOption(typeof(TdsEndpointResultCodes), "StatementRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StatementRefusedCode : TdsEndpointResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="StatementRefusedCode"/> class.</summary>
    public StatementRefusedCode()
        : base(20001, "StatementRefused",
            ResultSeverities.ByName("Error"),
            "The statement was refused with SQL Server error {number}: {text}",
            isRetryable: false)
    {
    }
}
