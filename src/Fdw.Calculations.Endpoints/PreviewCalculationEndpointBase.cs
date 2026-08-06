using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Calculations.Abstractions.CalculationTypeOptions;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Web.Calculations.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// Base endpoint for previewing a calculation with sample data.
/// </summary>
public abstract class PreviewCalculationEndpointBase : Endpoint<PreviewCalculationRequest, PreviewCalculationResponse>
{
    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/calculations/preview");
#if DEVELOP
        AllowAnonymous();
#else
        // Why: preview computes a calculation result for an ad-hoc spec without persisting it.
        // It's a read-shaped operation (no state change), so Viewer with calculations:read is allowed.
        Policies("calculations:read");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(PreviewCalculationRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var calculationType = CalculationTypes.ByName(req.CalculationType);
        if (calculationType.Id == 0)
        {
            CalculationEndpointLog.UnknownCalculationType(EndpointLogger, req.CalculationType);
            AddError($"Unknown calculation type: {req.CalculationType}");
            await Send.ErrorsAsync(cancellation: ct).ConfigureAwait(false);
            return;
        }

        CalculationEndpointLog.PreviewingCalculation(EndpointLogger, req.CalculationType);

        var sampleSize = req.SampleSize > 0 ? req.SampleSize : 10;
        var sampleValues = GenerateSampleData(sampleSize);

        try
        {
            var sampleRows = new List<IDataRow>(sampleValues.Length);
            foreach (var v in sampleValues)
                sampleRows.Add(DataRow.SingleField("value", v));
            var result = calculationType.Calculate(sampleRows, "value");
            CalculationEndpointLog.CalculationPreviewCompleted(EndpointLogger, req.CalculationType);

            await Send.OkAsync(new PreviewCalculationResponse
            {
                CalculationType = req.CalculationType,
                SampleData = sampleValues,
                Result = result,
                Description = GetCalculationDescription(req.CalculationType)
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            CalculationEndpointLog.CalculationPreviewFailed(EndpointLogger, ex, req.CalculationType);
            AddError("An internal error occurred during calculation execution.");
            await Send.ErrorsAsync(cancellation: ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Generates sample data for the preview. Override to provide custom sample data generation.
    /// </summary>
    protected virtual decimal[] GenerateSampleData(int count)
    {
        var values = new decimal[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = Math.Round((decimal)RandomNumberGenerator.GetInt32(0, 10001) / 100m, 2);
        }
        return values;
    }

    /// <summary>
    /// Gets the description for a calculation type. Override to provide custom descriptions.
    /// </summary>
    protected virtual string GetCalculationDescription(string typeName) => typeName switch
    {
        "Sum" => "Calculates the total sum of all values",
        "Average" => "Calculates the arithmetic mean of all values",
        "Count" => "Returns the number of values",
        "Min" => "Returns the minimum value",
        "Max" => "Returns the maximum value",
        _ => $"Performs {typeName} calculation"
    };
}
