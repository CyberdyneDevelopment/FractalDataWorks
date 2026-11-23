using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The base of every format choice refusal.</summary>
[ExcludeFromCodeCoverage]
public abstract class FormatChoiceRefusalBase : TypeOptionBase<FormatChoiceRefusalBase>, IFormatChoiceRefusal
{
    /// <summary>Initializes a new instance of the <see cref="FormatChoiceRefusalBase"/> class.</summary>
    /// <param name="name">The refusal's name.</param>
    /// <param name="messageNumber">The number of the row source message this refusal answers.</param>
    protected FormatChoiceRefusalBase(string name, int messageNumber)
        : base(name)
    {
        MessageNumber = messageNumber;
    }

    /// <inheritdoc />
    [TypeLookup("ByMessageNumber")]
    public int MessageNumber { get; }

    /// <inheritdoc />
    public abstract IGenericResult<RowWriterOptions> Refuse(ILogger logger, string resource, string format, string reason);
}
