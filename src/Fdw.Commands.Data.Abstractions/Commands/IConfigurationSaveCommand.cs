using System;
using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A version-on-write save of one configuration row: the current version the container's Logical key
/// matches is retired and the new version is inserted, and the key values of both are returned.
/// </summary>
/// <remarks>
/// The row is the record's mapped properties plus <see cref="FieldValues"/>, intersected with the fields
/// its container declares. Nothing is looked up: a Foreign key field -- to the row's owner or to a row it
/// cites -- is written from the value it arrives with, like any other field.
/// </remarks>
public interface IConfigurationSaveCommand : IDataCommand, IDataCommandWithInput
{
    /// <summary>Gets the CLR type of the configuration record being saved.</summary>
    Type ConfigurationType { get; }

    /// <summary>
    /// Gets field values written beyond the record's mapped properties, keyed by field name -- the
    /// Foreign key fields an owner fills with its own key values. Empty when there are none.
    /// </summary>
    IReadOnlyDictionary<string, object?> FieldValues { get; }

    /// <summary>
    /// Gets whether the save replaces a current version of the row -- retiring it before the insert -- or
    /// only inserts. A child row is only inserted: it is written under its owner's new row, from which
    /// nothing current hangs yet, and the children of the version replaced stay with that version.
    /// </summary>
    bool ReplacesCurrent { get; }
}
