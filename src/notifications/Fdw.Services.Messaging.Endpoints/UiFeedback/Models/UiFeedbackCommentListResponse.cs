using System.Collections.Generic;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// A page of the comments on a UI note, oldest first.
/// </summary>
public class UiFeedbackCommentListResponse
{
    /// <summary>Gets or sets the comments.</summary>
    public IReadOnlyList<UiFeedbackCommentResponse> Items { get; set; } = [];
}
