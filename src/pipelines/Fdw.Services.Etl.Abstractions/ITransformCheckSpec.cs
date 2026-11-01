namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// One check of a Validate stage, as a request states it.
/// </summary>
public interface ITransformCheckSpec
{
    /// <summary>Gets the check's name, unique within its stage; a rejected record is reported with it.</summary>
    string Name { get; }

    /// <summary>Gets the condition a record must meet, in the pipeline's condition language.</summary>
    string Expression { get; }
}
