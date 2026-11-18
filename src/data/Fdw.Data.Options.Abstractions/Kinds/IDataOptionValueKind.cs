using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>A kind of value of a data configuration rule (FDW-6454).</summary>
public interface IDataOptionValueKind : ITypeOption<int, DataOptionValueKindBase>
{
    /// <summary>Gets the name a console shows for this option (FDW-6342); every option is a type option, which states one.</summary>
    string DisplayName { get; }

    /// <summary>Gets the member of a value that holds this kind's content.</summary>
    string Member { get; }
}
