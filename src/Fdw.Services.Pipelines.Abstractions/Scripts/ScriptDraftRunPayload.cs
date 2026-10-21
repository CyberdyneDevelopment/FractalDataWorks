namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A script that is not saved, run once with the values given.</summary>
public class ScriptDraftRunPayload : ScriptRunPayload
{
    /// <summary>Gets or sets the script to run.</summary>
    public ScriptPayload Script { get; set; } = new();
}
