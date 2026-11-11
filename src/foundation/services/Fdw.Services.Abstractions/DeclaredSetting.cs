using System;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.ServiceTypes;

/// <summary>
/// Resolves the tuning settings a type declares that are not timeouts (FDW-4959): a whole number of at least one (a batch size) and a
/// true-or-false choice (a table lock). <see cref="DeclaredSeconds"/> does the same for timeouts.
/// </summary>
/// <remarks>
/// The default is declared in the type's <see cref="IDeclaresDefaults.DeclaredDefaults"/>, a host changes it with
/// <see cref="ServiceTypeDefaults.Override"/> before Register, and the value a configuration states wins over both. Each is read once, in the
/// Register phase of the type that declares it, and handed to the factory that type registers.
/// </remarks>
public static class DeclaredSetting
{
    /// <summary>Resolves a type's whole-number default for a configuration field: the host's override, else the value the type declares.</summary>
    /// <param name="option">The type that declares the default.</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="logger">The logger of the type registering.</param>
    /// <returns>The number; a failure naming the type and field when none is declared or it is below one.</returns>
    public static IGenericResult<int> Count(ITypeOption option, string field, ILogger? logger)
    {
        if (option is null) throw new ArgumentNullException(nameof(option));
        var log = logger ?? NullLogger.Instance;
        if (!ServiceTypeDefaults.TryResolve(option, field, out var value) || value is not int number)
            return GenericResult<int>.Failure(ServiceTypeLog.DeclaredCountNotDeclared(log, option.Name, field));

        return number < 1
            ? GenericResult<int>.Failure(ServiceTypeLog.DeclaredCountNotPositive(log, option.Name, field, number))
            : GenericResult<int>.Success(number);
    }

    /// <summary>Resolves a type's true-or-false default for a configuration field: the host's override, else the value the type declares.</summary>
    /// <param name="option">The type that declares the default.</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="logger">The logger of the type registering.</param>
    /// <returns>The choice; a failure naming the type and field when none is declared.</returns>
    public static IGenericResult<bool> Flag(ITypeOption option, string field, ILogger? logger)
    {
        if (option is null) throw new ArgumentNullException(nameof(option));
        return ServiceTypeDefaults.TryResolve(option, field, out var value) && value is bool flag
            ? GenericResult<bool>.Success(flag)
            : GenericResult<bool>.Failure(ServiceTypeLog.DeclaredFlagNotDeclared(logger ?? NullLogger.Instance, option.Name, field));
    }

    /// <summary>Gives the whole number a configuration takes: the number it states, or else the default its type declared.</summary>
    /// <param name="stated">The number the configuration states, or null when it states none. A stated number below one is refused.</param>
    /// <param name="declared">The type's resolved default (<see cref="Count"/>), or null when the factory was built without its type.</param>
    /// <param name="typeName">The type, as its option names it.</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="logger">The logger of the factory building it.</param>
    /// <returns>The number; a failure naming the type and field when the stated number is below one, or none is stated and none was declared.</returns>
    public static IGenericResult<int> EffectiveCount(int? stated, int? declared, string typeName, string field, ILogger? logger)
    {
        var log = logger ?? NullLogger.Instance;
        if (stated is { } own)
            return own < 1
                ? GenericResult<int>.Failure(ServiceTypeLog.StatedCountNotPositive(log, typeName, field, own))
                : GenericResult<int>.Success(own);

        return declared is { } typeDefault
            ? GenericResult<int>.Success(typeDefault)
            : GenericResult<int>.Failure(ServiceTypeLog.DeclaredCountNotDeclared(log, typeName, field));
    }

    /// <summary>Gives the true-or-false choice a configuration takes: the one it states, or else the default its type declared.</summary>
    /// <param name="stated">The choice the configuration states, or null when it states none.</param>
    /// <param name="declared">The type's resolved default (<see cref="Flag"/>), or null when the factory was built without its type.</param>
    /// <param name="typeName">The type, as its option names it.</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="logger">The logger of the factory building it.</param>
    /// <returns>The choice; a failure naming the type and field when none is stated and none was declared.</returns>
    public static IGenericResult<bool> EffectiveFlag(bool? stated, bool? declared, string typeName, string field, ILogger? logger)
    {
        if (stated is { } own)
            return GenericResult<bool>.Success(own);

        return declared is { } typeDefault
            ? GenericResult<bool>.Success(typeDefault)
            : GenericResult<bool>.Failure(ServiceTypeLog.DeclaredFlagNotDeclared(logger ?? NullLogger.Instance, typeName, field));
    }
}
