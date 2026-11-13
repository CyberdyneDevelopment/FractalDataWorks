using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>What an abstract Graph data set intends to read, as a request states it (FDW-5287, FDW-5294): a data store or another data set, and nothing more.</summary>
public sealed class GraphIntendedSourceRequest
{
    /// <summary>Gets or sets what it names: the name of a <c>GraphIntentKinds</c> option (DataStore or DataSet).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store it names, when its kind names one.</summary>
    public string? DataStore { get; set; }

    /// <summary>Gets or sets the name of the data set it names, when its kind names one; the data set must exist.</summary>
    public string? DataSet { get; set; }

    /// <summary>Gets or sets a note on the intention.</summary>
    public string? Note { get; set; }

    /// <summary>Gets or sets the intended source's stable id (FDW-5452). Answered with every intended source. On an update, one that carries the id of a stored one keeps its id, author and time; one that carries none is new and the server mints its id; an id that matches none is refused.</summary>
    public Guid? Id { get; set; }

    /// <summary>Gets or sets the user id of the person who stated the intention. Set by the server and ignored in a request.</summary>
    public Guid? AuthorUserId { get; set; }

    /// <summary>Gets or sets when the intention was stated. Set by the server and ignored in a request.</summary>
    public DateTimeOffset? CreatedAt { get; set; }
}
