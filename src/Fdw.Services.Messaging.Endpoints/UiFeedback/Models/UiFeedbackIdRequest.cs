using System;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// A request that names one UI comment in the route.
/// </summary>
public class UiFeedbackIdRequest
{
    /// <summary>Gets or sets the comment's identifier.</summary>
    public Guid Id { get; set; }
}
