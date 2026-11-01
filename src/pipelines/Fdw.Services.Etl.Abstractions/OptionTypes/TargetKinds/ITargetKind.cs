using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What a source reads or a destination writes: the declared container, one file of it, a list of files read as one stream (FDW-3089), the container
/// of a data set, or files split by a field (FDW-3193).
/// </summary>
public interface ITargetKind : ITypeOption<int, ITargetKind>
{
    /// <summary>Gets the settings, by their names in a component's body, that a source of this kind must state.</summary>
    IReadOnlyList<string> Needs { get; }

    /// <summary>Gets the settings, by their names in a component's body, that a source of this kind must not state.</summary>
    IReadOnlyList<string> Refuses { get; }

    /// <summary>Gets a value indicating whether a source can be of this kind.</summary>
    bool ForSources { get; }

    /// <summary>Gets a value indicating whether a destination can be of this kind.</summary>
    bool ForDestinations { get; }

    /// <summary>Gets the settings, by their names in a component's body, that a destination of this kind must state.</summary>
    IReadOnlyList<string> WritesNeed { get; }

    /// <summary>Gets the settings, by their names in a component's body, that a destination of this kind must not state.</summary>
    IReadOnlyList<string> WritesRefuse { get; }
}
