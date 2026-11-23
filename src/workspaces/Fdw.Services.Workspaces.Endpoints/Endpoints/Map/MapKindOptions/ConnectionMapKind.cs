using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A connection, counted from its domain table by the summary.</summary>
[TypeOption(typeof(MapKindOptions), "Connection")]
[ExcludeFromCodeCoverage]
public sealed class ConnectionMapKind : MapKindOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionMapKind"/> class.</summary>
    public ConnectionMapKind()
        : base("Connection")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IDomainRecordProvider<DomainConfiguration>> Records(MapCountProviders providers, ILogger logger)
        => MapSources.Records(providers.Connections as IDomainRecordProvider<DomainConfiguration>, Name, logger);
}
