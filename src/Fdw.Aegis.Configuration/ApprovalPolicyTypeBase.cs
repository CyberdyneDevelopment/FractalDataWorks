using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// CRTP base class for <see cref="IApprovalPolicyType"/> options.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ApprovalPolicyTypeBase : TypeOptionBase<ApprovalPolicyTypeBase>, IApprovalPolicyType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalPolicyTypeBase"/> class.
    /// </summary>
    /// <param name="name">The policy kind name (matches the AegisCommand domain row's <c>Implementation</c>).</param>
    /// <param name="configurationType">The .NET type of the typed-body configuration for this policy kind.</param>
    protected ApprovalPolicyTypeBase(string name, Type configurationType)
        : base(name)
    {
        ConfigurationType = configurationType;
    }

    /// <inheritdoc />
    public Type ConfigurationType { get; }
}
