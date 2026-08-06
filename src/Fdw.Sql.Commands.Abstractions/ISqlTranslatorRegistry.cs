using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Lookup-by-command-type translator registry.</summary>
public interface ISqlTranslatorRegistry
{
    /// <summary>Register every translator instance produced by DI.</summary>
    void RegisterAll(IEnumerable<ISqlCommandTranslator> translators);

    /// <summary>Get the translator registered for a command type.</summary>
    IGenericResult<ISqlCommandTranslator> GetTranslator(Type commandType);
}
