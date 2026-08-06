using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// A method was called on a disposed SQLite transaction.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "TransactionDisposed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TransactionDisposedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionDisposedCode"/> class.
    /// </summary>
    public TransactionDisposedCode()
        : base(21018, "TransactionDisposed",
            ResultSeverities.ByName("Error"),
            "Transaction has been disposed",
            isRetryable: false)
    {
    }
}
