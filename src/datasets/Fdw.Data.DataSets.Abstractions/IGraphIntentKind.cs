using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A kind of intended source of a Graph data set: what the intent names.</summary>
public interface IGraphIntentKind : ITypeOption<int, GraphIntentKindBase>
{
    /// <summary>Gets a value indicating whether an intent of this kind names a data store.</summary>
    bool NamesDataStore { get; }

    /// <summary>Gets a value indicating whether an intent of this kind names another data set.</summary>
    bool NamesDataSet { get; }
}
