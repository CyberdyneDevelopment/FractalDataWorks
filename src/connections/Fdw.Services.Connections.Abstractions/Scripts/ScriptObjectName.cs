namespace Fdw.Services.Connections.Abstractions;

/// <summary>An object a script names (FDW-5457), in the terms of its store's catalogue.</summary>
/// <param name="Path">The path the script names the object in (a schema, for a SQL store); null when it names none and the store decides.</param>
/// <param name="Name">The object's name: the container it is in the catalogue.</param>
public sealed record ScriptObjectName(string? Path, string Name)
{
    /// <summary>Gets the name as the script gives it, with its path when it gives one.</summary>
    public string Display => Path is null ? Name : string.Concat(Path, ".", Name);
}
