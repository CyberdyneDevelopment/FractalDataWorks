using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Abstractions.Logging;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request leaves out a choice the format needs.</summary>
[TypeOption(typeof(FormatChoiceRefusals), "NotStated", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NotStatedFormatChoiceRefusal : FormatChoiceRefusalBase
{
    /// <summary>Initializes a new instance of the <see cref="NotStatedFormatChoiceRefusal"/> class.</summary>
    public NotStatedFormatChoiceRefusal()
        : base("NotStated", RowSourceLog.FormatChoiceNotStatedId)
    {
    }

    /// <inheritdoc />
    public override IGenericResult<RowWriterOptions> Refuse(ILogger logger, string resource, string format, string reason) =>
        GenericResult<RowWriterOptions>.Failure(StreamRecordsEndpointLog.StreamRecordsFormatChoiceNotStated(logger, resource, format, reason));
}
