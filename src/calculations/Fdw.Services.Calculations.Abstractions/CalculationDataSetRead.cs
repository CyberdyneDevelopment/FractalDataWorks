namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Names what a host states in the calculation context's parameters so that a calculation may read a data set's rows.
/// </summary>
public static class CalculationDataSetRead
{
    /// <summary>
    /// The parameter that holds the most rows a calculation may read from one data set input, as an <see cref="int"/>. A host that
    /// runs a calculation for a caller states it; a run that states none has no caller to read as, and a DataSet input refuses.
    /// </summary>
    public const string RowCapParameterName = "DataSetRowCap";
}
