using Fdw.Collections;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>How a stream answers one kind of refusal of a format's choices, found by the number of the message that refused them.</summary>
public interface IFormatChoiceRefusal : ITypeOption<int, FormatChoiceRefusalBase>
{
    /// <summary>Gets the number of the row source message this refusal answers.</summary>
    int MessageNumber { get; }

    /// <summary>Answers the refusal.</summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="resource">The resource being streamed.</param>
    /// <param name="format">The format whose choices were refused.</param>
    /// <param name="reason">Why the choices were refused.</param>
    /// <returns>The failed result.</returns>
    IGenericResult<RowWriterOptions> Refuse(ILogger logger, string resource, string format, string reason);
}
