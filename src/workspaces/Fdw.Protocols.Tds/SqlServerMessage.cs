using System;

namespace Fdw.Protocols.Tds;

/// <summary>
/// A message as SQL Server reports it to a client: its number, state, severity class and text.
/// </summary>
/// <remarks>
/// A class of 10 or below is informational and travels as INFO; above 10 it is an error and travels as
/// ERROR. Clients key behaviour on the number — 18456 is a failed login, 208 an unknown object — so each
/// refusal this server makes carries the number SQL Server would have used for the same refusal.
/// </remarks>
public sealed class SqlServerMessage
{
    /// <summary>The class at and below which a message is informational.</summary>
    public const byte MaxInformationalClass = 10;

    /// <summary>Initializes a new instance of the <see cref="SqlServerMessage"/> class.</summary>
    /// <param name="number">The message number.</param>
    /// <param name="state">The state, which tells apart the places one number is raised from.</param>
    /// <param name="severityClass">The severity class.</param>
    /// <param name="text">The message text.</param>
    /// <param name="lineNumber">The line of the batch or procedure the message refers to.</param>
    public SqlServerMessage(int number, byte state, byte severityClass, string text, int lineNumber)
    {
        Number = number;
        State = state;
        Class = severityClass;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        LineNumber = lineNumber;
    }

    /// <summary>Gets the message number.</summary>
    public int Number { get; }

    /// <summary>Gets the state.</summary>
    public byte State { get; }

    /// <summary>Gets the severity class.</summary>
    public byte Class { get; }

    /// <summary>Gets the message text.</summary>
    public string Text { get; }

    /// <summary>Gets the procedure the message was raised in; empty when it was raised by a batch.</summary>
    public string ProcedureName { get; init; } = string.Empty;

    /// <summary>Gets the line of the batch or procedure the message refers to.</summary>
    public int LineNumber { get; }

    /// <summary>Gets a value indicating whether the message is an error rather than information.</summary>
    public bool IsError => Class > MaxInformationalClass;
}
