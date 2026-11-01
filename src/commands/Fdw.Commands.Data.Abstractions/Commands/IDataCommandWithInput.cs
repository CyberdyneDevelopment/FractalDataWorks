namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Non-generic interface for commands that carry input data.
/// Provides untyped access to Data for translators that need to inspect command data.
/// </summary>
public interface IDataCommandWithInput : IDataCommand
{
    /// <summary>
    /// Gets the input data for this command as an untyped object.
    /// </summary>
    /// <value>The input data, or null if not set.</value>
    object? InputData { get; }
}