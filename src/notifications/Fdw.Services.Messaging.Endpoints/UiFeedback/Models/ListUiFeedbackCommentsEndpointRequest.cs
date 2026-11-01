using System;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Query of the comments on a UI note.
/// </summary>
public class ListUiFeedbackCommentsEndpointRequest
{
    /// <summary>Gets or sets the note's identifier, from the route.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the number of comments to skip.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the number of comments to take, from 1 to 200. The API's page is 50.</summary>
    public int Take { get; set; } = 50;
}
