using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Connection name picker.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Connection")]
[ExcludeFromCodeCoverage]
public sealed class ConnectionConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="ConnectionConfigurationPropertyType"/>.</summary>
    public ConnectionConfigurationPropertyType() : base(7, "Connection") { }
}
