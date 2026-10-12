namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Request naming a data view: the body of <c>POST /data-views/get</c>.</summary>
public sealed class DataViewNameRequest
{
    /// <summary>Gets or sets the data view's name.</summary>
    public string Name { get; set; } = string.Empty;
}
