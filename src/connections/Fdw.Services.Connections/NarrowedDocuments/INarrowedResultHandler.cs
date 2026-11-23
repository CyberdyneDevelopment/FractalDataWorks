using System;
using Fdw.Collections;
using Fdw.Documents.Abstractions;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Makes the paths of one kind of command result relative to a folder again; the collection is keyed by the result value's type name.</summary>
public interface INarrowedResultHandler : ITypeOption<int, INarrowedResultHandler>
{
    /// <summary>Makes the value's paths relative.</summary>
    /// <param name="value">The value, of the type this handler is named for.</param>
    /// <param name="folder">The folder the paths are made relative to.</param>
    /// <param name="unnarrowable">Makes the message for a value that is not of the type this handler narrows; it logs when called.</param>
    /// <returns>The value with relative paths, or the failure of a path outside the folder.</returns>
    IGenericResult<object> Narrow(object value, DocumentScope folder, Func<IGenericMessage> unnarrowable);
}
