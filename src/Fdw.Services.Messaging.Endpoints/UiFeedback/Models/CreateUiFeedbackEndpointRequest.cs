using System.Collections.Generic;
using System.Text.Json;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Request to leave a UI comment: what the console sends when a tester saves a post-it.
/// </summary>
public class CreateUiFeedbackEndpointRequest
{
    /// <summary>Gets or sets the address of the screen.</summary>
    public string? Screen { get; set; }

    /// <summary>Gets or sets the route pattern of the screen.</summary>
    public string? Route { get; set; }

    /// <summary>Gets or sets the area marked, in window coordinates.</summary>
    public UiFeedbackArea? Area { get; set; }

    /// <summary>Gets or sets the size of the window.</summary>
    public UiFeedbackSize? Viewport { get; set; }

    /// <summary>Gets or sets the theme the screen was in; optional.</summary>
    public string? Theme { get; set; }

    /// <summary>
    /// Gets or sets the components the comment is about, in the order the reviewer chose them; required, at least one and at most 20. Each is the console's description
    /// of one element, a JSON object, kept as it is.
    /// </summary>
    public IReadOnlyList<JsonElement>? Anchors { get; set; }

    /// <summary>Gets or sets the picture of the screen as a PNG data URL; optional.</summary>
    public string? Screenshot { get; set; }

    /// <summary>Gets or sets the urgency, an option of the UiFeedbackUrgencies type collection; required.</summary>
    public string? Urgency { get; set; }

    /// <summary>Gets or sets the tags, options of the UiFeedbackTags type collection; send an empty list for none.</summary>
    public IReadOnlyList<string>? Tags { get; set; }

    /// <summary>Gets or sets the note.</summary>
    public string? Body { get; set; }
}
