using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Documents.Abstractions;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>The type the narrowed result handler collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class NarrowedResultHandlerBase : TypeOptionBase<NarrowedResultHandlerBase>, INarrowedResultHandler
{
    /// <summary>Initializes the handler with the name of the value type it narrows.</summary>
    /// <param name="name">The value type's name.</param>
    protected NarrowedResultHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract IGenericResult<object> Narrow(object value, DocumentScope folder, Func<IGenericMessage> unnarrowable);
}

/// <summary>A narrowed result handler for one value class.</summary>
/// <typeparam name="TValue">The value class the handler narrows.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class NarrowedResultHandlerBase<TValue> : NarrowedResultHandlerBase
    where TValue : class
{
    /// <summary>Initializes the handler with the name of the value type it narrows.</summary>
    /// <param name="name">The value type's name.</param>
    protected NarrowedResultHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public sealed override IGenericResult<object> Narrow(object value, DocumentScope folder, Func<IGenericMessage> unnarrowable)
    {
        if (value is not TValue typed)
        {
            return GenericResult<object>.Failure(unnarrowable());
        }

        var narrowed = Narrow(typed, folder);
        return narrowed.IsSuccess && narrowed.Value is { } narrowedValue ? GenericResult<object>.Success(narrowedValue) : narrowed.ToNewResult<object>();
    }

    /// <summary>Makes the value's paths relative.</summary>
    /// <param name="value">The value.</param>
    /// <param name="folder">The folder the paths are made relative to.</param>
    /// <returns>The value with relative paths, or the failure of a path outside the folder.</returns>
    protected abstract IGenericResult<TValue> Narrow(TValue value, DocumentScope folder);
}
