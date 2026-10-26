using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Retires the current configuration rows that match and returns the key values of every row it retired.
/// When <see cref="MarkDeleted"/> is set the rows are marked deleted and stay current; otherwise they
/// are set non-current.
/// </summary>
/// <remarks>
/// The rows are matched either on field values -- the Foreign key fields an owner's key values fill --
/// or on the values of one key the container declares, named by its type. A save retires the children
/// of the version it replaces without deleting them; a delete marks them deleted.
/// </remarks>
public interface IConfigurationRetireCommand : IDataCommand, IDataCommandWithInput
{
    /// <summary>Gets the field values to match on, keyed by field name, or null when a key is matched instead.</summary>
    IReadOnlyDictionary<string, object?>? FieldValues { get; }

    /// <summary>Gets the type of the declared key to match on, or null when field values are matched instead.</summary>
    KeyTypeBase? KeyType { get; }

    /// <summary>Gets the values of <see cref="KeyType"/>'s fields, in key-field order.</summary>
    IReadOnlyList<object?> KeyValues { get; }

    /// <summary>Gets whether the retired rows are marked deleted and left current, instead of set non-current.</summary>
    bool MarkDeleted { get; }
}
