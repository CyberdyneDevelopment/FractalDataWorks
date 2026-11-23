using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>An authentication flow's description: what the flow is for, in the words of whoever configured it (FDW-6208). Read by whoever holds <c>settings/server:read</c>. The flow is the thing described, so the type has no owner: its descriptions are removed with it. The provider never fills the text on load: the endpoint that shows it reads it once for its answer.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class AuthenticationFlowDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the provider writes and the endpoint reads its descriptions under.</summary>
    public const string OptionName = "AuthenticationFlow";

    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowDescriptionType"/> class.</summary>
    public AuthenticationFlowDescriptionType()
        : base(OptionName, "settings/server:read", hasOwner: false)
    {
    }
}
