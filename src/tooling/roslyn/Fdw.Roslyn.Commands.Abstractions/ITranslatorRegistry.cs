using System;
using Fdw.Results;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Registry for looking up command translators.
/// </summary>
public interface ITranslatorRegistry
{
    /// <summary>
    /// Gets a translator for the specified command and result types.
    /// </summary>
    /// <typeparam name="TCommand">The type of command.</typeparam>
    /// <typeparam name="TResult">The type of result.</typeparam>
    /// <returns>A result containing the translator or an error if not found.</returns>
    IGenericResult<IRoslynCommandTranslator<TCommand, TResult>> GetTranslator<TCommand, TResult>()
        where TCommand : IRoslynCommand
        where TResult : IRoslynCommandResult;

    /// <summary>
    /// Gets a translator for the specified command type.
    /// </summary>
    /// <param name="commandType">The type of command.</param>
    /// <returns>A result containing the translator or an error if not found.</returns>
    IGenericResult<IRoslynCommandTranslator> GetTranslator(Type commandType);

    /// <summary>
    /// Registers a translator.
    /// </summary>
    /// <param name="translator">The translator to register.</param>
    void Register(IRoslynCommandTranslator translator);

    /// <summary>
    /// Registers a strongly-typed translator.
    /// </summary>
    /// <typeparam name="TCommand">The type of command.</typeparam>
    /// <typeparam name="TResult">The type of result.</typeparam>
    /// <param name="translator">The translator to register.</param>
    void Register<TCommand, TResult>(IRoslynCommandTranslator<TCommand, TResult> translator)
        where TCommand : IRoslynCommand
        where TResult : IRoslynCommandResult;
}
