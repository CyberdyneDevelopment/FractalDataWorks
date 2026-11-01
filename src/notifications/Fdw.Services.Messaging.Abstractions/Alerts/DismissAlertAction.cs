using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>The alert is taken off this person's screen for good; others still see it</summary>
[TypeOption(typeof(AlertActions), "Dismiss")]
public sealed class DismissAlertAction : AlertActionBase
{
    /// <summary>Initializes a new instance of the <see cref="DismissAlertAction"/> class.</summary>
    public DismissAlertAction() : base("Dismiss", "Dismiss", "The alert is taken off this person's screen for good; others still see it", false, null, true, [])
    {
    }
}
