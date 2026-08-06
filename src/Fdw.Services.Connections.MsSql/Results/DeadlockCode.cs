using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// SQL deadlock victim (error 1205). The transaction was chosen as a deadlock victim.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "Deadlock", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeadlockCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeadlockCode"/> class.
    /// </summary>
    public DeadlockCode()
        : base(
            81000,
            "Deadlock",
            ResultSeverities.ByName("Warning"),
            "Transaction was chosen as a deadlock victim on '{CommandText}'. The operation can be retried.",
            isRetryable: true)
    {
    }
}
