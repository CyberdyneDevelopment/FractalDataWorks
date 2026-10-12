using System;
using System.Collections.Generic;

namespace Fdw.UI.Services.Commands;

/// <summary>
/// Command that executes multiple commands as a single unit.
/// </summary>
public sealed class CompositeCommand : ICommand
{
    private readonly List<ICommand> _commands;
    private readonly string _description;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeCommand"/> class.
    /// </summary>
    public CompositeCommand(string description, IEnumerable<ICommand> commands)
    {
        _description = description ?? throw new ArgumentNullException(nameof(description));
        _commands = new List<ICommand>(commands ?? throw new ArgumentNullException(nameof(commands)));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeCommand"/> class.
    /// </summary>
    public CompositeCommand(string description, params ICommand[] commands)
        : this(description, (IEnumerable<ICommand>)commands)
    {
    }

    /// <inheritdoc />
    public string Description => _description;

    /// <inheritdoc />
    public void Execute()
    {
        foreach (var command in _commands)
        {
            command.Execute();
        }
    }

    /// <inheritdoc />
    public void Undo()
    {
        // Undo in reverse order
        for (var i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}
