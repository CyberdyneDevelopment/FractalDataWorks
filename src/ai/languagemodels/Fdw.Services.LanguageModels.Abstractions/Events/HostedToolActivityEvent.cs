
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>Something a tool the provider runs itself did, or found.</summary>
public sealed class HostedToolActivityEvent : LanguageModelStreamEvent
{
    /// <summary>Initializes a new instance of the <see cref="HostedToolActivityEvent"/> class.</summary>
    /// <param name="activity">The activity.</param>
    public HostedToolActivityEvent(HostedToolActivity activity)
    {
        Activity = activity;
    }

    /// <inheritdoc />
    public override IStreamEventKind Kind => StreamEventKinds.ByName("HostedToolActivity");

    /// <summary>Gets the activity.</summary>
    public HostedToolActivity Activity { get; }
}
