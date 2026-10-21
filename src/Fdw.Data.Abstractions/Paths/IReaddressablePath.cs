namespace Fdw.Data.Abstractions;

/// <summary>
/// A path that can be made again at another place of the same kind (FDW-1967): the table of a database at another schema or name, a file in
/// another directory or under another name. Each kind of path keeps its address in its own parts, so only the kind knows how its path part
/// and its name part are replaced; what reads a container never has to know which kind it holds.
/// </summary>
public interface IReaddressablePath : IPath
{
    /// <summary>Makes the same kind of path at another place.</summary>
    /// <param name="pathPart">The path part to use (the schema of a table, the directory of a file); none or blank keeps this path's.</param>
    /// <param name="namePart">The name part to use (the table, the file name); none or blank keeps this path's.</param>
    /// <returns>A new path of the same kind with the stated parts replaced; this path is not changed.</returns>
    IPath Readdress(string? pathPart, string? namePart);
}
