using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A quality rule, counted from its domain table by the summary.</summary>
[TypeOption(typeof(MapKindOptions), "QualityRule")]
[ExcludeFromCodeCoverage]
public sealed class QualityRuleMapKind : MapKindOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="QualityRuleMapKind"/> class.</summary>
    public QualityRuleMapKind()
        : base("QualityRule")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IDomainRecordProvider<DomainConfiguration>> Records(MapCountProviders providers, ILogger logger)
        => MapSources.Records(providers.QualityRules as IDomainRecordProvider<DomainConfiguration>, Name, logger);
}
