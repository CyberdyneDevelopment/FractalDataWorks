using System.Collections.Generic;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// A page of UI comments.
/// </summary>
public class UiFeedbackListResponse
{
    /// <summary>Gets or sets the comments.</summary>
    public IReadOnlyList<UiFeedbackResponse> Items { get; set; } = [];
}
