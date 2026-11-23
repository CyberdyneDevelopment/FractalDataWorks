using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;
using Fdw.Services.Scheduling.Abstractions.Results;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A once trigger states the one time it fires, as a UTC DateTime.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StoredTriggerChecks), "Once")]
public sealed class OnceStoredTriggerCheck : StoredTriggerCheckBase
{
    /// <summary>Initializes a new instance of the <see cref="OnceStoredTriggerCheck"/> class.</summary>
    public OnceStoredTriggerCheck()
        : base("Once")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Check(IReadOnlyDictionary<string, object> configuration)
    {
        if (!configuration.TryGetValue(Once.StartTimeKey, out var executeObj) ||
            executeObj is not System.DateTime executeAtUtc)
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("OnceExecuteAtRequired"));
        }

        if (executeAtUtc.Kind != System.DateTimeKind.Utc)
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("OnceExecuteAtMustBeUtc"));
        }

        return GenericResult.Success();
    }
}
