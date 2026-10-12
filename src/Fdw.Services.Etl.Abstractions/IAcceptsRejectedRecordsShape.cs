namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// An option that can say whether it writes the records a pipeline's stages rejected (FDW-1148).
/// </summary>
/// <remarks>
/// The fact is data: it is read from the option, not written into the validator or the console, and it is listed in
/// <c>/type-collections/LoadStepKinds/values</c> as <c>acceptsRejectedRecords</c> so a form can offer only the kinds a reject
/// step may use.
/// </remarks>
public interface IAcceptsRejectedRecordsShape
{
    /// <summary>Gets whether the option can write the records a pipeline's stages rejected, with their reason and stage.</summary>
    bool AcceptsRejectedRecords { get; }
}
