namespace Fdw.Collections;

/// <summary>
/// A type option whose use must be acknowledged by the people told of it (FDW-2142): an alert about something of this kind stays until a
/// person says they have seen it.
/// </summary>
/// <remarks>The type-collection value listing gives it as <c>needsAcknowledgement</c>, so a screen decides on the fact and never on a name.</remarks>
public interface IAcknowledgementShape
{
    /// <summary>Gets whether an alert about something of this kind stays until a person acknowledges it.</summary>
    bool NeedsAcknowledgement { get; }
}
