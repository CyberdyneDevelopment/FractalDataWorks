using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A connection's description: what the connection reaches, in the words of whoever configured it (FDW-6208). Read by whoever holds <c>connections:read</c>. The connection is the thing described, so the type has no owner: a connection is removed with its own descriptions. One type serves every connection type, because the connection type stays invisible above the connection layer.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class ConnectionDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the connection provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Connection";

    /// <summary>Initializes a new instance of the <see cref="ConnectionDescriptionType"/> class.</summary>
    public ConnectionDescriptionType()
        : base(OptionName, "connections:read", hasOwner: false)
    {
    }
}
