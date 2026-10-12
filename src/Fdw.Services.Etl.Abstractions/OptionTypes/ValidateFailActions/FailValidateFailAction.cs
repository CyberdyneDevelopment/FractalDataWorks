using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The run fails at the first record that fails a check.</summary>
[TypeOption(typeof(ValidateFailActions), "Fail")]
public sealed class FailValidateFailAction : ValidateFailActionBase
{
    /// <summary>Initializes a new instance of the <see cref="FailValidateFailAction"/> class.</summary>
    public FailValidateFailAction() : base("Fail", "Fail the run", "The run stops at the first record that fails a check", stopsRun: true)
    {
    }
}
