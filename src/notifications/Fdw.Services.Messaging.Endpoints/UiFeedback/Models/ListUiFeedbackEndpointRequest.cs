namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Query of a list of UI comments.
/// </summary>
public class ListUiFeedbackEndpointRequest
{
    /// <summary>Gets or sets the state to list: Open or Resolved; leave it out to list both.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the route pattern to list the comments of; leave it out to list every screen.</summary>
    public string? Route { get; set; }

    /// <summary>Gets or sets the urgency to list; leave it out to list every urgency.</summary>
    public string? Urgency { get; set; }

    /// <summary>Gets or sets the tag to list; leave it out to list every tag.</summary>
    public string? Tag { get; set; }

    /// <summary>Gets or sets the order: <c>urgency</c> for the most urgent first; leave it out for the newest first.</summary>
    public string? Sort { get; set; }

    /// <summary>Gets or sets the number of comments to skip.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the number of comments to take, from 1 to 200. The API's page is 50.</summary>
    public int Take { get; set; } = 50;
}
