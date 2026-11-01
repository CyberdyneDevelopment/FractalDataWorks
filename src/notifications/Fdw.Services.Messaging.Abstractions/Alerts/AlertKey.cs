using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// Names one alert (FDW-2420): the kind of alert it is and its identifier in that kind.
/// </summary>
public sealed class AlertKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlertKey"/> class.
    /// </summary>
    /// <param name="kind">The kind of alert, the name of an alert source such as <c>UiFeedback</c>.</param>
    /// <param name="id">The identifier of the alert in that kind.</param>
    public AlertKey(string kind, Guid id)
    {
        Kind = kind;
        Id = id;
    }

    /// <summary>Gets the kind of alert.</summary>
    public string Kind { get; }

    /// <summary>Gets the identifier of the alert in its kind.</summary>
    public Guid Id { get; }
}
