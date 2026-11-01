using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;

namespace Fdw.Sql.Commands;

/// <summary>Default <see cref="ISqlTranslatorRegistry"/>: dictionary keyed by command type.</summary>
public sealed class SqlTranslatorRegistry : ISqlTranslatorRegistry
{
    private readonly ConcurrentDictionary<Type, ISqlCommandTranslator> _byCommandType = new();

    /// <inheritdoc/>
    public void RegisterAll(IEnumerable<ISqlCommandTranslator> translators)
    {
        foreach (var t in translators)
        {
            _byCommandType[t.CommandType] = t;
        }
    }

    /// <inheritdoc/>
    public IGenericResult<ISqlCommandTranslator> GetTranslator(Type commandType)
    {
        if (_byCommandType.TryGetValue(commandType, out var t))
            return GenericResult<ISqlCommandTranslator>.Success(t);
        return GenericResult<ISqlCommandTranslator>.Failure(
            SqlResultCodes.TranslatorNotFound,
            ResultDetails.Create("CommandType", commandType.Name));
    }
}
