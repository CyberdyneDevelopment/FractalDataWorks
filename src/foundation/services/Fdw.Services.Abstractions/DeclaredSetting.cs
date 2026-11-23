using System;
using System.Globalization;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.Hosting;
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
        var log = logger ?? NullLogger.Instance;
        if (!ServiceTypeDefaults.TryResolve(option, field, out var value) || value is not int number)
            return GenericResult<int>.Failure(ServiceTypeLog.DeclaredCountNotDeclared(log, option.Name, field));

        return number < 1
            ? GenericResult<int>.Failure(ServiceTypeLog.DeclaredCountNotPositive(log, option.Name, field, number))
            : GenericResult<int>.Success(number);
    }

    /// <summary>
    /// Resolves a host-wide whole-number limit a type declares (FDW-7103): the value the host's configuration states at <c>{section}:{field}</c>,
    /// else the host's override, else the value the type declares. Read once, in the Register phase of the type that declares it.
    /// </summary>
    /// <remarks>
    /// <see cref="EffectiveCount"/> takes the value a configuration row states; this takes the value a host states in its own configuration (appsettings,
    /// environment), for a limit that belongs to the host and not to a row: how many rows one read may hold, how large a page may be.
    /// </remarks>
    /// <param name="builder">The host being built, whose configuration is read.</param>
    /// <param name="section">The configuration section the host states the limit in.</param>
    /// <param name="option">The type that declares the default.</param>
    /// <param name="field">The limit's name: the key under <paramref name="section"/> and the key of the type's declared default.</param>
    /// <param name="logger">The logger of the type registering.</param>
    /// <returns>The number; a failure naming the key when the stated text is not a whole number or is below one, or naming the field when none is
    /// stated and none is declared or the default is below one.</returns>
    public static IGenericResult<int> ConfiguredCount(IHostApplicationBuilder builder, string section, ITypeOption option, string field, ILogger? logger)
    {
        var log = logger ?? NullLogger.Instance;
        var key = section + ":" + field;
        if (builder.Configuration[key] is not { Length: > 0 } text)
            return Count(option, field, log);

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stated))
            return GenericResult<int>.Failure(ServiceTypeLog.StatedCountNotANumber(log, option.Name, key, text));

        return stated < 1
            ? GenericResult<int>.Failure(ServiceTypeLog.StatedCountNotPositive(log, option.Name, key, stated))
            : GenericResult<int>.Success(stated);
    }

    /// <summary>
    /// Resolves a host-wide true-or-false setting a type declares (FDW-6901): the value the host's configuration states at <c>{section}:{field}</c>,
    /// else the host's override, else the value the type declares. Read once, in the Register phase that uses it.
    /// </summary>
    /// <param name="builder">The host being built, whose configuration is read.</param>
    /// <param name="section">The configuration section the host states the setting in.</param>
    /// <param name="option">The type that declares the default.</param>
    /// <param name="field">The setting's name: the key under <paramref name="section"/> and the key of the type's declared default.</param>
    /// <param name="logger">The logger of the type registering.</param>
    /// <returns>The choice; a failure naming the key when the stated text is neither true nor false, or naming the field when none is stated and
    /// none is declared.</returns>
    public static IGenericResult<bool> ConfiguredFlag(IHostApplicationBuilder builder, string section, ITypeOption option, string field, ILogger? logger)
    {
        var log = logger ?? NullLogger.Instance;
        var key = section + ":" + field;
        if (builder.Configuration[key] is not { Length: > 0 } text)
            return Flag(option, field, log);

        return bool.TryParse(text, out var stated)
            ? GenericResult<bool>.Success(stated)
            : GenericResult<bool>.Failure(ServiceTypeLog.StatedFlagNotTrueOrFalse(log, option.Name, key, text));
    }

    /// <summary>Resolves a type's name default for a configuration field (which option of a collection applies): the host's override, else the
    /// value the type declares (FDW-6901).</summary>
    /// <param name="option">The type that declares the default.</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="logger">The logger of the type registering.</param>
    /// <returns>The name; a failure naming the type and field when none is declared or the declared one is blank.</returns>
    public static IGenericResult<string> Name(ITypeOption option, string field, ILogger? logger)
    {
        return ServiceTypeDefaults.TryResolve(option, field, out var value) && value is string { Length: > 0 } name
            ? GenericResult<string>.Success(name)
            : GenericResult<string>.Failure(ServiceTypeLog.DeclaredNameNotDeclared(logger ?? NullLogger.Instance, option.Name, field));
    }

    /// <summary>
    /// Resolves a host-wide name setting a type declares (FDW-6901): the value the host's configuration states at <c>{section}:{field}</c>, else
    /// the host's override, else the value the type declares. The caller checks the name is one of the options it chooses among.
    /// </summary>
    /// <param name="builder">The host being built, whose configuration is read.</param>
    /// <param name="section">The configuration section the host states the setting in.</param>
    /// <param name="option">The type that declares the default.</param>
    /// <param name="field">The setting's name: the key under <paramref name="section"/> and the key of the type's declared default.</param>
    /// <param name="logger">The logger of the type registering.</param>
    /// <returns>The name; a failure naming the field when none is stated and none is declared.</returns>
    public static IGenericResult<string> ConfiguredName(IHostApplicationBuilder builder, string section, ITypeOption option, string field, ILogger? logger)
    {
        return builder.Configuration[section + ":" + field] is { Length: > 0 } stated
            ? GenericResult<string>.Success(stated)
            : Name(option, field, logger);
    }

    /// <summary>Resolves a type's true-or-false default for a configuration field: the host's override, else the value the type declares.</summary>
    /// <param name="option">The type that declares the default.</param>
    /// <param name="field">The configuration property's name.</param>
    /// <param name="logger">The logger of the type registering.</param>
    /// <returns>The choice; a failure naming the type and field when none is declared.</returns>
    public static IGenericResult<bool> Flag(ITypeOption option, string field, ILogger? logger)
    {
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
