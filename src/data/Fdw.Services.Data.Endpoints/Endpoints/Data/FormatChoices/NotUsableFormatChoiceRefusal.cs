using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Abstractions.Logging;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request states a choice that cannot be used as written.</summary>
[TypeOption(typeof(FormatChoiceRefusals), "NotUsable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NotUsableFormatChoiceRefusal : FormatChoiceRefusalBase
{
    /// <summary>Initializes a new instance of the <see cref="NotUsableFormatChoiceRefusal"/> class.</summary>
    public NotUsableFormatChoiceRefusal()
        : base("NotUsable", RowSourceLog.FormatChoiceNotUsableId)
    {
    }

    /// <inheritdoc />
    public override IGenericResult<RowWriterOptions> Refuse(ILogger logger, string resource, string format, string reason) =>
        GenericResult<RowWriterOptions>.Failure(StreamRecordsEndpointLog.StreamRecordsFormatChoiceNotUsable(logger, resource, format, reason));
}
