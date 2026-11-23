using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;
using Fdw.Services.Scheduling.Abstractions.Results;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A manual trigger needs nothing, but a value it states for AllowConcurrent has to read as true or false.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StoredTriggerChecks), "Manual")]
public sealed class ManualStoredTriggerCheck : StoredTriggerCheckBase
{
    /// <summary>Initializes a new instance of the <see cref="ManualStoredTriggerCheck"/> class.</summary>
    public ManualStoredTriggerCheck()
        : base("Manual")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Check(IReadOnlyDictionary<string, object> configuration)
    {
        // Manual triggers have minimal validation requirements
        if (configuration.TryGetValue(Manual.AllowConcurrentKey, out var allowConcurrentObj) &&
            !TriggerValueShapes.ReadBool(allowConcurrentObj, out _))
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("ManualAllowConcurrentInvalid"));
        }

        return GenericResult.Success();
    }
}
