using System;
using Fdw.UI.Services.Commands;

namespace Fdw.UI.Services.UndoRedo;

/// <summary>
/// Manages undo/redo operations for commands.
/// </summary>
public interface IUndoRedoManager : IDisposable
{
    /// <summary>
    /// Gets whether there are commands to undo.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Gets whether there are commands to redo.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Gets the description of the next undo operation.
    /// </summary>
    string? UndoDescription { get; }

    /// <summary>
    /// Gets the description of the next redo operation.
    /// </summary>
    string? RedoDescription { get; }

    /// <summary>
    /// Gets the number of commands in the undo stack.
    /// </summary>
    int UndoCount { get; }

    /// <summary>
    /// Gets the number of commands in the redo stack.
    /// </summary>
    int RedoCount { get; }

    /// <summary>
    /// Executes a command and adds it to the undo stack.
    /// </summary>
    void Execute(ICommand command);

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    void Redo();

    /// <summary>
    /// Clears both undo and redo stacks.
    /// </summary>
    void Clear();
}
