using System;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Builders;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>
/// The one way a command is translated: the operation rule is asked, and only then is the translator called (FDW-6682).
/// </summary>
/// <remarks>
/// Why one method that asks and translates (release manager, 2026-11-17): a route that asked the rule and then called the translator itself needed every
/// route to remember to ask, and a route added later could forget. Here the translation is passed in and is run only when the rule accepts, so there is
/// no way to get a native command from a translator through this type without the ask, and the ask cannot be made on its own. A translator that states no
/// operations (<see cref="IStatesOperations"/>) is not asked, which is how the connection kinds that have not adopted the rule yet behave. The sync form is for the bulk routes,
/// which plan a write and are not async.
/// </remarks>
public static class TranslatorOperations
{
    /// <summary>Translates a command if the container's type states every operation the translator's command performs.</summary>
    /// <typeparam name="TNative">What the translation builds (a native command, or a plan).</typeparam>
    /// <param name="translator">The translator, which may be wrapped (<c>SettingsBoundTranslator</c> states what its inner translator states).</param>
    /// <param name="container">The container the command runs against.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="translate">The translation, run only when the rule accepts.</param>
    /// <returns>The translation, or the coded failure naming the container, its type and the operation it does not state.</returns>
    public static Task<IGenericResult<TNative>> Translate<TNative>(object translator, IStorageContainer container, ILogger logger, Func<Task<IGenericResult<TNative>>> translate)
        => Refusal(translator, container, logger) is { } notStated
            ? Task.FromResult(notStated.ToNewResult<TNative>())
            : translate();

    /// <summary>The sync form of <see cref="Translate{TNative}(object, IStorageContainer, ILogger, Func{Task{IGenericResult{TNative}}})"/>, for a route that plans a write without awaiting.</summary>
    /// <typeparam name="TNative">What the translation builds.</typeparam>
    /// <param name="translator">The translator.</param>
    /// <param name="container">The container the command runs against.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="translate">The translation, run only when the rule accepts.</param>
    /// <returns>The translation, or the coded failure.</returns>
    public static IGenericResult<TNative> Translate<TNative>(object translator, IStorageContainer container, ILogger logger, Func<IGenericResult<TNative>> translate)
        => Refusal(translator, container, logger) is { } notStated
            ? notStated.ToNewResult<TNative>()
            : translate();

    private static IGenericResult? Refusal(object translator, IStorageContainer container, ILogger logger)
    {
        if (translator is not IStatesOperations { Operations.Count: > 0 } states)
            return null;

        var judged = DataOperationRule.Check(container, states.Operations, logger);
        return judged.IsFailure ? judged : null;
    }
}
