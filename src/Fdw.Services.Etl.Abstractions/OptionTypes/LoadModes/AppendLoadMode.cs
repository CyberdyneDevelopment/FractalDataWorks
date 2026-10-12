using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Rows are added to the destination with a bulk insert.</summary>
[TypeOption(typeof(LoadModes), "Append")]
public sealed class AppendLoadMode : LoadModeBase
{
    /// <summary>Initializes a new instance of the <see cref="AppendLoadMode"/> class.</summary>
    public AppendLoadMode() : base("Append", usesKeyColumns: false, usesCompareColumns: false, usesWriteColumns: false, allowsVersioning: false, allowsSoftDelete: false, truncatesFirst: false)
    {
    }
}
