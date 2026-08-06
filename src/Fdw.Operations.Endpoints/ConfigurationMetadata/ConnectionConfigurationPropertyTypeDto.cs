using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>Connection name picker.</summary>
[TypeOption(typeof(ConfigurationPropertyTypeDtos), "Connection")]
[ExcludeFromCodeCoverage]
public sealed class ConnectionConfigurationPropertyTypeDto : ConfigurationPropertyTypeDtoBase
{
    /// <summary>Initializes a new instance of <see cref="ConnectionConfigurationPropertyTypeDto"/>.</summary>
    public ConnectionConfigurationPropertyTypeDto() : base(7, "Connection") { }
}
