using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// CRTP base class for <see cref="IApprovalPolicyType"/> options.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ApprovalPolicyTypeBase : TypeOptionBase<int, ApprovalPolicyTypeBase>, IApprovalPolicyType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalPolicyTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The policy kind name (matches <c>AegisCommandConfiguration.ServiceOptionType</c>).</param>
    /// <param name="configurationType">The .NET type of the typed-body configuration for this policy kind.</param>
    protected ApprovalPolicyTypeBase(int id, string name, Type configurationType)
        : base(id, name)
    {
        ConfigurationType = configurationType;
    }

    /// <inheritdoc />
    public Type ConfigurationType { get; }
}
