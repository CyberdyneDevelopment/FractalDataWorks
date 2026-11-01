using System.Collections.Generic;

namespace Fdw.Services.Messaging;

/// <summary>
/// What a tester sends to leave a UI comment (FDW-2143). Who they are and which company they act in come from the caller, never from here.
/// </summary>
public sealed class CreateUiFeedbackRequest
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
    /// Gets or sets the components the comment is about, each the console's description of one element as JSON text, in the order the reviewer chose them; required,
    /// at least one, each a JSON object, and kept as it is (FDW-2343).
    /// </summary>
    public IReadOnlyList<string>? AnchorsJson { get; set; }

    /// <summary>Gets or sets the picture of the screen as a PNG data URL; optional.</summary>
    public string? Screenshot { get; set; }

    /// <summary>Gets or sets the urgency, an option of the UiFeedbackUrgencies type collection; required, with no default.</summary>
    public string? Urgency { get; set; }

    /// <summary>Gets or sets the tags, options of the UiFeedbackTags type collection; any number, each once. An empty list means none; leaving it out is refused.</summary>
    public IReadOnlyList<string>? Tags { get; set; }

    /// <summary>Gets or sets the note; not empty, at most <see cref="UiFeedbackLimits.MaxBodyLength"/> characters.</summary>
    public string? Body { get; set; }
}
