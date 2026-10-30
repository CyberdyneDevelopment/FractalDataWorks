namespace Fdw.Data.Abstractions;

/// <summary>
/// A path that names one file (FDW-3193): the file name and the extension of that name. A destination that writes files named by a value of a field
/// makes each name from the value and the extension of the declared file, so it needs a path that can say what its name is and a connection
/// type never has to be named.
/// </summary>
public interface IFilePath : IReaddressablePath
{
    /// <summary>Gets the file name, with its extension.</summary>
    string FileName { get; }

    /// <summary>Gets the extension of the file name, with its dot; empty when the name has none.</summary>
    string Extension { get; }
}
