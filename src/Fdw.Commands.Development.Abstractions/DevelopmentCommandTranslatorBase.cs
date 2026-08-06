using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Base class for development command translators.
/// </summary>
public abstract class DevelopmentCommandTranslatorBase : TypeOptionBase<int, DevelopmentCommandTranslatorBase>, IDevelopmentCommandTranslator
{
    /// <summary>
    /// Gets the type of command this translator handles.
    /// </summary>
    public abstract Type CommandType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevelopmentCommandTranslatorBase"/> class.
    /// </summary>
    /// <param name="name">The name of the translator.</param>
    /// <param name="description">The description of the translator.</param>
    protected DevelopmentCommandTranslatorBase(string name, string description)
        : base(GenerateIdFromName(name), name, name, name, description, "DevelopmentCommandTranslator")
    {
    }

    /// <summary>
    /// Generates a deterministic ID from a translator name using FNV-1a hash.
    /// </summary>
    private static int GenerateIdFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

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
}

/// <summary>
/// Strongly-typed base class for development command translators.
/// </summary>
/// <typeparam name="TCommand">The type of command.</typeparam>
/// <typeparam name="TContext">The type of context.</typeparam>
/// <typeparam name="TResult">The type of result.</typeparam>
public abstract class DevelopmentCommandTranslatorBase<TCommand, TContext, TResult>
    : DevelopmentCommandTranslatorBase, IDevelopmentCommandTranslator<TCommand, TContext, TResult>
    where TCommand : IDevelopmentCommand
    where TResult : IDevelopmentCommandResult
{
    /// <inheritdoc/>
    public override Type CommandType => typeof(TCommand);

    /// <summary>
    /// Initializes a new instance of the translator.
    /// </summary>
    /// <param name="name">The name of the translator.</param>
    /// <param name="description">The description of the translator.</param>
    protected DevelopmentCommandTranslatorBase(string name, string description)
        : base(name, description)
    {
    }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<TResult>> Execute(
        TCommand command,
        TContext context,
        CancellationToken cancellationToken = default);
}
