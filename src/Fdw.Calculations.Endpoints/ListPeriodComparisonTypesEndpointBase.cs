using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;
using Fdw.Web.Calculations.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for listing all available period comparison types.
/// </summary>
public abstract class ListPeriodComparisonTypesEndpointBase : EndpointWithoutRequest<PeriodComparisonTypesResponse>
{
    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/calculations/period-comparisons");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("calculations:read");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        CalculationEndpointLog.ListingPeriodComparisonTypes(EndpointLogger);

        var types = PeriodComparisonTypes.All();
        var dtos = new PeriodComparisonTypePayload[types.Count];

        int i = 0;
        foreach (var t in types)
        {
            dtos[i++] = new PeriodComparisonTypePayload
            {
                Id = t.Id,
                Name = t.Name,
                Description = GetPeriodComparisonDescription(t.Name)
            };
        }

        CalculationEndpointLog.ListedPeriodComparisonTypes(EndpointLogger, dtos.Length);

        return Send.OkAsync(new PeriodComparisonTypesResponse { Types = dtos }, ct);
    }

    /// <summary>
    /// Gets the description for a period comparison type. Override to provide custom descriptions.
    /// </summary>
    protected virtual string GetPeriodComparisonDescription(string typeName) => typeName switch
    {
        "None" => "No period comparison",
        "DayOverDay" => "Compares current day to previous day",
        "WeekOverWeek" => "Compares current week to previous week",
        "MonthOverMonth" => "Compares current month to previous month",
        "MonthToDate" => "Compares current month-to-date to previous month-to-date",
        "QuarterOverQuarter" => "Compares current quarter to previous quarter",
        "QuarterToDate" => "Compares current quarter-to-date to previous quarter-to-date",
        "YearOverYear" => "Compares current year to previous year",
        "YearToDate" => "Compares current year-to-date to previous year-to-date",
        _ => $"Performs {typeName} period comparison"
    };
}
