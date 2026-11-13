using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>An intended source of an abstract Graph data set, reported as not bound yet (FDW-5290). It never blocks a save.</summary>
public sealed class GraphDataSetNotBoundPayload
{
    /// <summary>Gets or sets where: <c>["intendedSources", "0"]</c>.</summary>
    public IList<string> Path { get; set; } = [];

    /// <summary>Gets or sets what the intent names: a <c>GraphIntentKinds</c> option name, DataStore or DataSet.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store or data set the intent names.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether it exists today. A data store may be external and not registered: it is still only not bound.</summary>
    public bool Exists { get; set; }
}
