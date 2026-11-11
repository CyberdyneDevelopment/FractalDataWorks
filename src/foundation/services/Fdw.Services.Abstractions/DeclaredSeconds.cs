using System;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.ServiceTypes;

/// <summary>
/// Resolves and checks the timeouts a type declares (FDW-4904): a connect, command or request timeout held in whole seconds. Each is read once,
/// in the Register phase of the type that declares it, and its value goes to the factory that type registers.
/// </summary>
/// <remarks>
/// Mike, 2026-11-10: "connection and command timeouts should ALL be done in configuration. the default values should be set in the connection type
/// option (so that it can be over written in the program.cs PlatformServices.... convention) and it should be configurable on the connection
/// instead of being hard coded". So a timeout a configuration may leave out is declared in the type's <see cref="IDeclaresDefaults.DeclaredDefaults"/>,
/// a host changes it with <see cref="ServiceTypeDefaults.Override"/> (or <see cref="ServiceTypeDefaults.OverrideForCategory"/> for every type of a
/// category) before Register, and the value a configuration states wins over both. The configuration property carries
/// <see cref="Fdw.Conventions.ResolvedAtUseAttribute"/>, so a create that leaves it out stores nothing and the default reaches it at use.
/// Why whole seconds of at least one: the drivers take their timeouts in whole seconds, so a fraction would be rounded, and zero means "no timeout".
/// </remarks>
public static class DeclaredSeconds
{
    /// <summary>Resolves a type's default, in whole seconds, for a configuration field: the host's override, else the value the type declares.</summary>
    /// <param name="option">The type that declares the default.</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="logger">The logger of the type registering.</param>
    /// <returns>The seconds; a failure naming the type and field when none is declared or it is below one.</returns>
    public static IGenericResult<int> Resolve(ITypeOption option, string field, ILogger? logger)
    {
        if (option is null) throw new ArgumentNullException(nameof(option));
        var log = logger ?? NullLogger.Instance;
        if (!ServiceTypeDefaults.TryResolve(option, field, out var value) || value is not int seconds)
            return GenericResult<int>.Failure(ServiceTypeLog.DeclaredSecondsNotDeclared(log, option.Name, field));

        return seconds < 1
            ? GenericResult<int>.Failure(ServiceTypeLog.DeclaredSecondsNotPositive(log, option.Name, field, seconds))
            : GenericResult<int>.Success(seconds);
    }

    /// <summary>
    /// Gives the timeout a configuration takes, in whole seconds: the number it states, or else the default its type declared, which the factory was
    /// handed when the type registered it.
    /// </summary>
    /// <param name="stated">The number the configuration states, or null when it states none. A stated number below one is refused.</param>
    /// <param name="declared">The type's resolved default (<see cref="Resolve"/>), or null when the factory was built without its type.</param>
    /// <param name="typeName">The type, as its option names it.</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="logger">The logger of the factory building it.</param>
    /// <returns>
    /// The seconds; a failure naming the type and field when the configuration states a number below one, or states none and the factory was
    /// handed no default.
    /// </returns>
    public static IGenericResult<int> Effective(int? stated, int? declared, string typeName, string field, ILogger? logger)
    {
        var log = logger ?? NullLogger.Instance;
        if (stated is { } own)
            return own < 1
                ? GenericResult<int>.Failure(ServiceTypeLog.StatedSecondsNotPositive(log, typeName, field, own))
                : GenericResult<int>.Success(own);

        return declared is { } typeDefault
            ? GenericResult<int>.Success(typeDefault)
            : GenericResult<int>.Failure(ServiceTypeLog.DeclaredSecondsNotDeclared(log, typeName, field));
    }
}
