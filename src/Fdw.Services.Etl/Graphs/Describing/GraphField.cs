namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// A field that a row has at a node (FDW-1397): its name and, when it is known, its type.
/// </summary>
/// <param name="Name">The field's name.</param>
/// <param name="Type">The field's type name; <see langword="null"/> when it cannot be known before a run.</param>
/// <param name="Origin">The field of a data set this one came from (FDW-2156); <see langword="null"/> when it came from none or it cannot be followed.</param>
public sealed record GraphField(string Name, string? Type, GraphFieldOrigin? Origin = null);
