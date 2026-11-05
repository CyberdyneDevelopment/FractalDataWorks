using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.EtlMappers;

/// <summary>
/// Gives a mapper configuration's value its default from the mapper type (FDW-3956): the value the configuration states, else the host's
/// override for the type, else the value the type declares. No value is assumed in code, so one with none of them refuses the mapper.
/// </summary>
public static class EtlRowMapperDefaults
{
    /// <summary>Resolves one value of a mapper configuration.</summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="stated">The value the configuration states, or <see langword="null"/>.</param>
    /// <param name="type">The mapper type that declares the default.</param>
    /// <param name="field">The configuration property's name, the key the type declares it under.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The value, or a failure naming the mapper type and the field.</returns>
    public static IGenericResult<T> Resolve<T>(T? stated, IEtlRowMapperType type, string field, ILogger logger)
        where T : struct =>
        stated is { } value
            ? GenericResult<T>.Success(value)
            : ServiceTypeDefaults.TryResolve(type, field, out var declared) && declared is T typed
                ? GenericResult<T>.Success(typed)
                : GenericResult<T>.Failure(EtlRowMapperLog.MapperValueNotStated(logger, type.Name, field));
}
