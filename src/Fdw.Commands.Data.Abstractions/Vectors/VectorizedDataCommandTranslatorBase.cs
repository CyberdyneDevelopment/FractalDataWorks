using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions.Logging;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Base class for vectorized command translators: a vectorized connection's translators collection holds them, named
/// for the command each translates.
/// </summary>
/// <typeparam name="TCommand">The connection's command type.</typeparam>
public abstract class VectorizedDataCommandTranslatorBase<TCommand> : IVectorizedDataCommandTranslator<TCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorizedDataCommandTranslatorBase{TCommand}"/> class.
    /// </summary>
    /// <param name="name">The command the translator translates: its <c>CommandType</c>.</param>
    protected VectorizedDataCommandTranslatorBase(string name)
    {
        Id = OptionId.DeriveInt(GetType().FullName!);
        Name = name;

        VectorizedDataCommandTranslatorBaseLog.TranslatorInitializing(
            NullLogger<VectorizedDataCommandTranslatorBase<TCommand>>.Instance, name);
    }

    /// <inheritdoc/>
    public int Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Category => "Vectorized";

    /// <inheritdoc/>
    public abstract Task<IGenericResult<TCommand>> Translate(
        IVectorizedDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default);
}
