using System;
using System.Collections.Generic;
using Fdw.UI.Services.Commands;

namespace Fdw.UI.Services.UndoRedo;

/// <summary>
/// Manages undo/redo operations for commands.
/// </summary>
public sealed class UndoRedoManager : IUndoRedoManager
{
    private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
    private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
    private readonly int _maxUndoLevels;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UndoRedoManager"/> class.
    /// </summary>
    public UndoRedoManager(int maxUndoLevels = 50)
    {
        if (maxUndoLevels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUndoLevels), "Must be greater than 0.");
        }
        _maxUndoLevels = maxUndoLevels;
    }

    /// <inheritdoc />
    public bool CanUndo => _undoStack.Count > 0;

    /// <inheritdoc />
    public bool CanRedo => _redoStack.Count > 0;

    /// <inheritdoc />
    public string? UndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    /// <inheritdoc />
    public string? RedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <inheritdoc />
    public int UndoCount => _undoStack.Count;

    /// <inheritdoc />
    public int RedoCount => _redoStack.Count;

    /// <inheritdoc />
    public void Execute(ICommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ThrowIfDisposed();

        command.Execute();
        _undoStack.Push(command);

        // Dispose redo commands (they're no longer valid after a new action)
        DisposeCommands(_redoStack);
        _redoStack.Clear();

        // Enforce max undo levels
        if (_undoStack.Count > _maxUndoLevels)
        {
            TrimUndoStack();
        }
    }

    /// <inheritdoc />
    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
    }

    /// <inheritdoc />
    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
    }

    /// <inheritdoc />
    public void Clear()
    {
        DisposeCommands(_undoStack);
        DisposeCommands(_redoStack);
        _undoStack.Clear();
        _redoStack.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Clear();
    }

    private void TrimUndoStack()
    {
        var commands = _undoStack.ToArray();
        _undoStack.Clear();

        for (var i = _maxUndoLevels; i < commands.Length; i++)
        {
            DisposeCommand(commands[i]);
        }

        for (var i = _maxUndoLevels - 1; i >= 0; i--)
        {
            _undoStack.Push(commands[i]);
        }
    }

    private static void DisposeCommands(Stack<ICommand> stack)
    {
        foreach (var command in stack)
        {
            DisposeCommand(command);
        }
    }

    private static void DisposeCommand(ICommand command)
    {
        if (command is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UndoRedoManager));
        }
    }
}
