namespace Fdw.Services.Data.Endpoints;

/// <summary>An intended source to add to a Graph data set (FDW-5452): what it names, and nothing more. Who stated it and when are the caller and now.</summary>
public sealed class AddGraphIntendedSourceRequest
{
    /// <summary>Gets or sets the name of the Graph data set.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what it names: the name of a <c>GraphIntentKinds</c> option (DataStore or DataSet).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store it names, when its kind names one.</summary>
    public string? DataStore { get; set; }

    /// <summary>Gets or sets the name of the data set it names, when its kind names one; the data set must exist.</summary>
    public string? DataSet { get; set; }

    /// <summary>Gets or sets a note on the intention.</summary>
    public string? Note { get; set; }
}
