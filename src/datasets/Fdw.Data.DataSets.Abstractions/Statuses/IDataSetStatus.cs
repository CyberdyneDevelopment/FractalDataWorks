using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A status a data set can have in a list (FDW-4488).</summary>
public interface IDataSetStatus : ITypeOption<int, DataSetStatusBase>
{
    /// <summary>Gets how a status ranks among the others: the highest-ranked status whose rule applies is the data set's.</summary>
    int Rank { get; }

    /// <summary>Says whether this status describes the data set whose writers the input holds.</summary>
    /// <param name="input">What the rule reads: the data set's writers and their latest runs, the window and the time now.</param>
    /// <returns><see langword="true"/> when the status applies.</returns>
    bool Applies(DataSetStatusInput input);
}
