using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fdw.Commands.Data.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// The base of the scripted command family (FDW-1435): the text and the named parameters every scripted command carries.
/// </summary>
public abstract class ScriptedDataCommandBase : IScriptedDataCommand
{
    // Why a Regex instance and not a generated one: this project targets netstandard2.0, which has no source-generated regex.
    private static readonly Regex ParameterName = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.None, TimeSpan.FromSeconds(1));

    /// <summary>Initializes a new instance for the Empty sentinel of <c>ScriptedDataCommands</c>: a command with no text.</summary>
    protected ScriptedDataCommandBase()
        : this("NotFound", string.Empty, [])
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ScriptedDataCommandBase"/> class.</summary>
    /// <param name="commandType">The command's name in <c>ScriptedDataCommands</c>.</param>
    /// <param name="text">The text to run.</param>
    /// <param name="parameters">The values of the script's named parameters.</param>
    protected ScriptedDataCommandBase(string commandType, string text, IReadOnlyList<ScriptParameter> parameters)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        CommandId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CommandType = commandType;

        ScriptedDataCommandBaseLog.CommandCreated(NullLogger<ScriptedDataCommandBase>.Instance, CommandId, CommandType);
    }

    /// <inheritdoc />
    public Guid CommandId { get; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; }

    /// <inheritdoc />
    public string CommandType { get; }

    /// <inheritdoc />
    public string Category => "Scripted";

    /// <inheritdoc />
    public string Text { get; }

    /// <inheritdoc />
    public IReadOnlyList<ScriptParameter> Parameters { get; }

    /// <inheritdoc />
    public virtual string? FirstProblem()
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return "the script has no text";
        }

        var unsafeName = Parameters.FirstOrDefault(parameter => !ParameterName.IsMatch(parameter.Name));
        if (unsafeName is not null)
        {
            return $"'{unsafeName.Name}' is not a parameter name (a letter or underscore, then letters, digits and underscores)";
        }

        var twice = Parameters.GroupBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        return twice is null ? null : $"the parameter '{twice.Key}' is given twice";
    }
}

/// <summary>A scripted command that answers with a stated type.</summary>
/// <typeparam name="TResult">What the command answers with.</typeparam>
public abstract class ScriptedDataCommandBase<TResult> : ScriptedDataCommandBase, IScriptedDataCommand<TResult>
{
    /// <summary>Initializes a new instance of the <see cref="ScriptedDataCommandBase{TResult}"/> class.</summary>
    /// <param name="commandType">The command's name in <c>ScriptedDataCommands</c>.</param>
    /// <param name="text">The text to run.</param>
    /// <param name="parameters">The values of the script's named parameters.</param>
    protected ScriptedDataCommandBase(string commandType, string text, IReadOnlyList<ScriptParameter> parameters)
        : base(commandType, text, parameters)
    {
    }
}
