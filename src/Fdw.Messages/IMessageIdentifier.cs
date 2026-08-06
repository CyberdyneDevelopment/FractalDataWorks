namespace Fdw.Messages;

/// <summary>
/// Interface for message types that have an Id and Name for collection operations.
/// </summary>
public interface IMessageIdentifier
{
    /// <summary>
    /// Gets the unique identifier for this message type.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the name of this message type.
    /// </summary>
    string Name { get; }
}