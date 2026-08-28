using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions.Logging;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Abstract base class for data command translators.
/// This base class is used by TypeCollection source generators for compile-time discovery.
/// </summary>
/// <typeparam name="TCommand">The command type this translator produces (SqlCommand, HttpRequestMessage, etc.).</typeparam>
/// <remarks>
/// <para>
/// This base class is primarily for compile-time translator discovery via [TypeOption].
/// Most translators will be registered at runtime by connections via DataCommandTranslators.Register().
/// </para>
/// <para>
/// Properties must be set in constructor for TypeCollection source generators to read them.
/// Implements ITypeOption for TypeCollection compatibility.
/// </para>
/// </remarks>
public abstract class DataCommandTranslatorBase<TCommand> : IDataCommandTranslator<TCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataCommandTranslatorBase{TCommand}"/> class.
    /// </summary>
    /// <param name="name">Name of the translator (must match TypeOption attribute if used).</param>
    /// <param name="domainName">Domain name this translator targets.</param>
    protected DataCommandTranslatorBase(string name, string domainName)
    {
        Id = GenerateIdFromName(name);
        Name = name;
        DomainName = domainName;

        DataCommandTranslatorBaseLog.TranslatorInitializing(
            NullLogger<DataCommandTranslatorBase<TCommand>>.Instance, name, domainName);
    }

    /// <summary>
    /// Generates a deterministic ID from a translator name using FNV-1a hash.
    /// </summary>
    private static int GenerateIdFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            DataCommandTranslatorBaseLog.TranslatorNameMissing(NullLogger<DataCommandTranslatorBase<TCommand>>.Instance);
            throw new ArgumentNullException(nameof(name));
        }

        unchecked
        {
            const int FnvPrime = 0x01000193;
            const int FnvOffsetBasis = (int)0x811C9DC5;

            int hash = FnvOffsetBasis;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= FnvPrime;
            }
            return hash & 0x7FFFFFFF;
        }
    }

    /// <summary>
    /// Gets the unique identifier for this translator type.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the unique identifier as object for ITypeOption compatibility.
    /// </summary>
    object ITypeOption.Id => Id;

    /// <summary>
    /// Gets the name of this translator type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the category of this translator type.
    /// </summary>
    public string Category => DomainName;

    /// <summary>
    /// Gets the domain name this translator targets.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Translates a data command to a connection-specific command of type TCommand.
    /// Uses container schema for intelligent query building (field roles, types, converters).
    /// </summary>
    /// <param name="command">The data command to translate.</param>
    /// <param name="container">The container with schema metadata (fields, roles, converters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the translated command of type TCommand.</returns>
    public abstract Task<IGenericResult<TCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default);
}
