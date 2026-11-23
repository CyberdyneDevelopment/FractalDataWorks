using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;
using Fdw.Services.Scheduling.Abstractions.Results;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A cron trigger states a cron expression of at least five fields.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StoredTriggerChecks), "Cron")]
public sealed class CronStoredTriggerCheck : StoredTriggerCheckBase
{
    /// <summary>Initializes a new instance of the <see cref="CronStoredTriggerCheck"/> class.</summary>
    public CronStoredTriggerCheck()
        : base("Cron")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Check(IReadOnlyDictionary<string, object> configuration)
    {
        if (!configuration.TryGetValue(Cron.CronExpressionKey, out var cronObj) ||
            cronObj is not string cronExpression ||
            string.IsNullOrWhiteSpace(cronExpression))
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("CronExpressionRequired"));
        }

        // Basic cron expression validation - detailed validation would be in TriggerType
        if (cronExpression.Split(' ').Length < 5)
        {
            return GenericResult.Failure(SchedulingResultCodes.ByName("CronExpressionInvalidFieldCount"));
        }

        return GenericResult.Success();
    }
}
