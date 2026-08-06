namespace Fdw.UI.Services.Commands;

/// <summary>
/// Represents a command that can be executed and undone.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets a description of what this command does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    void Execute();

    /// <summary>
    /// Undoes the command.
    /// </summary>
    void Undo();
}
