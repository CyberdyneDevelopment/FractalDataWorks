using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Audit.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.Audit;

/// <summary>
/// Abstract endpoint that lists audit records with optional filtering by entity type, action, user, and date range.
/// </summary>
public abstract class ListAuditRecordsEndpointBase : Endpoint<ListAuditRecordsRequest, IReadOnlyList<AuditRecord>>
{
    private readonly IAuditService _auditService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListAuditRecordsEndpointBase"/> class.
    /// </summary>
    /// <param name="auditService">The audit service for querying audit records.</param>
    /// <param name="logger">The logger instance.</param>
    protected ListAuditRecordsEndpointBase(
        IAuditService auditService,
        ILogger<ListAuditRecordsEndpointBase>? logger)
    {
        _auditService = auditService;
        _logger = logger ?? NullLogger<ListAuditRecordsEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/audit");
        Policies("datasets:read");
        Summary(s =>
        {
            s.Summary = "List audit records";
            s.Description = "Lists configuration audit trail records with optional filtering.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Lists audit records, optionally filtered by entity type, action, and user.</summary>
    public override async Task HandleAsync(ListAuditRecordsRequest req, CancellationToken ct)
    {
        OperationsEndpointLog.ListingAuditRecords(_logger, req.EntityType, req.Action);

        var result = await _auditService.ListAuditRecords(
            req.EntityType,
            req.EntityId,
            req.Action,
            req.UserId,
            req.Limit,
            ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            // Why: CurrentMessage may be null if the service returned a failure with no messages;
            // use the no-arg overload to avoid a fallback string that masks missing failure detail.
            if (result.CurrentMessage is { } msg)
                OperationsEndpointLog.ListAuditRecordsFailed(_logger, msg);
            else
                OperationsEndpointLog.ListAuditRecordsFailed(_logger);
            AddError("Failed to list audit records");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        OperationsEndpointLog.AuditRecordsFound(_logger, result.Value!.Length);
        await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
    }
}
