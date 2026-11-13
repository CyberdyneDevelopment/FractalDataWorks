namespace Fdw.Services.Data;

/// <summary>One stored field of a data set or a container, as the check before save and the describer need it (FDW-5522, FDW-5523): its name and the name of its data type.</summary>
/// <param name="Name">The field's name.</param>
/// <param name="TypeName">The name of the field's data type: empty for a data set field nobody has typed, and none for a container field whose portable type nobody has stated.</param>
public sealed record GraphDataSetFieldFact(string Name, string? TypeName);
