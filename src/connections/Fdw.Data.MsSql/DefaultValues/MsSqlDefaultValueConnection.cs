using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using System.Collections.Generic;

namespace Fdw.Data.MsSql;

/// <summary>
/// MsSql's entry point into the <see cref="DefaultValues"/> root collection.
/// </summary>
[TypeOption(typeof(DefaultValues), "MsSql")]
public sealed class MsSqlDefaultValueConnection : DefaultValueConnectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDefaultValueConnection"/> class.
    /// </summary>
    public MsSqlDefaultValueConnection()
        : base("MsSql")
    {
    }

    /// <inheritdoc />
    public override IDefaultValuePolicy ByName(string policyName) => MsSqlDefaults.ByName(policyName);

    /// <inheritdoc />
    public override IReadOnlyList<IDefaultValuePolicy> Policies => [.. MsSqlDefaults.All()];
}
