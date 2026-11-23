using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Runs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>
/// The answer requests of runs (FDW-6142), the rows of <c>etl.PipelineAnswerRequest</c> and its children in the operational store. Asking, answering and starting are each held by the store, not by memory:
/// a unique key on the asking step refuses a second request for it, one conditional update (the request is Open, is the caller's and has not ended) says by the rows it changed whether this call owns the answer,
/// and the start of the continuation is a claim under the asking run's chain (<see cref="IPipelineStepClaims"/>) that only one call can make.
/// </summary>
/// <remarks>
/// <para>
/// The person is the caller, read by the rule every run's person is read by (<see cref="RunPerson"/>), and the tenant is the caller's own active tenant (<see cref="RunTenant"/>); nothing a request carries names either.
/// What the person is shown and the continuation's arguments and roles are written when the run asks and read from the rows when it starts, so the answer states its text and nothing else.
/// </para>
/// <para>
/// Why the continuation's start is not retried after a failure: an answer is taken once. A start that failed is recorded as failed and refused the same way again, and a start whose host went down mid-start
/// recorded no outcome, so nobody can say whether the continuation began; it is refused and says so (the stance FDW-6139 takes for a step with an outside effect).
/// </para>
/// </remarks>
public sealed class AnswerRequests : IAnswerRequests
{
    /// <summary>The start of the component name the claim of an answered step is made under; a component of a graph cannot be named so, or its claim could be taken for the answered step's.</summary>
    public const string ClaimNamePrefix = "AnswerRequest:";

    private const string PathName = "etl";
    private const string RequestContainerName = "PipelineAnswerRequest";
    private const string ShownContainerName = "PipelineAnswerRequestShown";
    private const string ArgumentContainerName = "PipelineAnswerRequestArgument";
    private const string RoleContainerName = "PipelineAnswerRequestRole";
    private const string TriggerSourcePrefix = "Answer:";

    private readonly IDataGatewayProvider _gateways;
    private readonly IAuthenticationContextAccessor _authentication;
    private readonly IPipelineRuns _runs;
    private readonly IPipelineStepClaims _claims;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="AnswerRequests"/> class.</summary>
    /// <param name="gateways">Gives the data gateway the operational store is reached through.</param>
    /// <param name="authentication">Gives the caller, who is the person that answers.</param>
    /// <param name="runs">Starts the continuation, as the caller.</param>
    /// <param name="claims">Claims the start of the continuation, so it is made once.</param>
    /// <param name="time">The clock a request's end is judged against.</param>
    /// <param name="logger">The logger.</param>
    public AnswerRequests(
        IDataGatewayProvider gateways, IAuthenticationContextAccessor authentication, IPipelineRuns runs, IPipelineStepClaims claims, TimeProvider time, ILogger<AnswerRequests>? logger = null)
    {
        _gateways = gateways;
        _authentication = authentication;
        _runs = runs;
        _claims = claims;
        _time = time;
        _logger = logger ?? NullLogger<AnswerRequests>.Instance;
    }

    private sealed record Caller(Guid Person, Guid Tenant);

    private static string Keeping => AnswerRequestStates.ByName("Keeping").Name;

    private static string Open => AnswerRequestStates.ByName("Open").Name;

    private static string Answered => AnswerRequestStates.ByName("Answered").Name;

    private static string Started => AnswerRequestStates.ByName("Started").Name;

    private static string Expired => AnswerRequestStates.ByName("Expired").Name;

    private IDataGateway Gateway => _gateways.ByName(DataGatewayNames.Main);

    /// <inheritdoc />
    public async Task<IGenericResult<AnswerRequestKept>> Ask(AnswerRequestAsk ask, CancellationToken cancellationToken = default)
    {
        if (PipelineTypes.OperationalConnection is not { Length: > 0 } store)
            return GenericResult<AnswerRequestKept>.Failure(AnswerRequestLog.OperationalStoreNotNamed(_logger, "Ask"));

        if (Problem(ask) is { } problem)
            return problem.ToNewResult<AnswerRequestKept>();

        var row = RowOf(ask, _time.GetUtcNow());
        var inserted = await Gateway.Execute<int>(Insert.Into<EtlPipelineAnswerRequestRecord>(RequestContainerName).DataStore(store).Path(PathName).Value(row), cancellationToken).ConfigureAwait(false);
        if (inserted.IsFailure)
        {
            // Why the refusal is read back and not read from its text: the unique key on the asking step is what refuses a second request, so the row that is there says whether this step asked before.
            var earlier = await ReadByStep(store, ask, inserted.CurrentMessage, cancellationToken).ConfigureAwait(false);
            if (earlier.IsFailure || earlier.Value is not { } existing)
                return earlier.ToNewResult<AnswerRequestKept>();

            row = existing;
        }

        if (string.Equals(row.StateName, Keeping, StringComparison.Ordinal))
        {
            var kept = await KeepParts(store, ask, row, cancellationToken).ConfigureAwait(false);
            if (kept.IsFailure)
                return kept.ToNewResult<AnswerRequestKept>();

            row.StateName = Open;
        }

        var request = await Present(store, row, cancellationToken).ConfigureAwait(false);
        if (request.IsFailure || request.Value is not { } read)
            return request.ToNewResult<AnswerRequestKept>();

        _ = inserted.IsSuccess
            ? AnswerRequestLog.Kept(_logger, row.Id, row.PersonId, ask.ComponentName, ask.ExecutionId, row.ExpiresAt)
            : AnswerRequestLog.AlreadyKept(_logger, ask.ComponentName, ask.ExecutionId, row.Id);
        return GenericResult<AnswerRequestKept>.Success(new AnswerRequestKept(read, inserted.IsSuccess));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Announced(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
    {
        if (PipelineTypes.OperationalConnection is not { Length: > 0 } store)
            return GenericResult.Failure(AnswerRequestLog.OperationalStoreNotNamed(_logger, "Announced"));

        var written = await Gateway.Execute<int>(
            Update.In<EtlPipelineAnswerRequestAnnounced>(RequestContainerName).DataStore(store).Path(PathName).Where("Id", requestId).Where("TenantId", tenantId)
                .Value(new EtlPipelineAnswerRequestAnnounced { AnnouncedAt = _time.GetUtcNow() }),
            cancellationToken).ConfigureAwait(false);
        return written.IsSuccess && written.Value > 0
            ? GenericResult.Success()
            : GenericResult.Failure(AnswerRequestLog.NotAnnouncedRecorded(_logger, requestId, written.IsSuccess ? "no request has that id in the tenant" : written.CurrentMessage));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AnswerRequestAnswered>> Answer(Guid requestId, string answer, CancellationToken cancellationToken = default)
    {
        var caller = CallerOf("Answer");
        if (caller.IsFailure || caller.Value is not { } who)
            return caller.ToNewResult<AnswerRequestAnswered>();

        if (PipelineTypes.OperationalConnection is not { Length: > 0 } store)
            return GenericResult<AnswerRequestAnswered>.Failure(AnswerRequestLog.OperationalStoreNotNamed(_logger, "Answer"));

        if (string.IsNullOrWhiteSpace(answer))
            return GenericResult<AnswerRequestAnswered>.Failure(AnswerRequestLog.AnswerEmpty(_logger, requestId));

        var now = _time.GetUtcNow();
        var due = await ExpireDue(store, who, now, cancellationToken).ConfigureAwait(false);
        if (due.IsFailure)
            return due.ToNewResult<AnswerRequestAnswered>();

        var found = await ReadOwn(store, who, requestId, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { } row)
            return found.ToNewResult<AnswerRequestAnswered>();

        var taken = await TakeTheAnswer(store, who, row, answer, now, cancellationToken).ConfigureAwait(false);
        if (taken.IsFailure || taken.Value is not { } answered)
            return taken.ToNewResult<AnswerRequestAnswered>();

        return await StartContinuation(store, answered, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<AnswerRequest>>> List(CancellationToken cancellationToken = default)
    {
        var caller = CallerOf("List");
        if (caller.IsFailure || caller.Value is not { } who)
            return caller.ToNewResult<IReadOnlyList<AnswerRequest>>();

        if (PipelineTypes.OperationalConnection is not { Length: > 0 } store)
            return GenericResult<IReadOnlyList<AnswerRequest>>.Failure(AnswerRequestLog.OperationalStoreNotNamed(_logger, "List"));

        var due = await ExpireDue(store, who, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        if (due.IsFailure)
            return due.ToNewResult<IReadOnlyList<AnswerRequest>>();

        var rows = await Gateway.Execute<IEnumerable<EtlPipelineAnswerRequestRecord>>(
            Query.From<EtlPipelineAnswerRequestRecord>(store, PathName, RequestContainerName)
                .Where("TenantId", who.Tenant).Where("PersonId", who.Person).Where("StateName", Open).Build(),
            cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
            return GenericResult<IReadOnlyList<AnswerRequest>>.Failure(AnswerRequestLog.NotRead(_logger, "the open requests", rows.CurrentMessage));

        List<AnswerRequest> requests = [];
        foreach (var row in rows.Value.OrderBy(row => row.AskedAt))
        {
            var presented = await Present(store, row, cancellationToken).ConfigureAwait(false);
            if (presented.IsFailure || presented.Value is not { } request)
                return presented.ToNewResult<IReadOnlyList<AnswerRequest>>();

            requests.Add(request);
        }

        return GenericResult<IReadOnlyList<AnswerRequest>>.Success(requests);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AnswerRequest>> Read(Guid requestId, CancellationToken cancellationToken = default)
    {
        var caller = CallerOf("Read");
        if (caller.IsFailure || caller.Value is not { } who)
            return caller.ToNewResult<AnswerRequest>();

        if (PipelineTypes.OperationalConnection is not { Length: > 0 } store)
            return GenericResult<AnswerRequest>.Failure(AnswerRequestLog.OperationalStoreNotNamed(_logger, "Read"));

        var due = await ExpireDue(store, who, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        if (due.IsFailure)
            return due.ToNewResult<AnswerRequest>();

        var found = await ReadOwn(store, who, requestId, cancellationToken).ConfigureAwait(false);
        return found.IsFailure || found.Value is not { } row
            ? found.ToNewResult<AnswerRequest>()
            : await Present(store, row, cancellationToken).ConfigureAwait(false);
    }

    // What an ask must state to be kept: the first thing wrong, as a failure with a message of its own, or null when nothing is. The checks are failure results themselves, so they chain.
    private IGenericResult? Problem(AnswerRequestAsk ask)
    {
        if (ReferenceEquals(AnswerRequestKinds.ByName(ask.KindName), AnswerRequestKinds.NotFound))
            return GenericResult.Failure(AnswerRequestLog.KindUnknown(_logger, ask.ComponentName, ask.KindName));

        if (ask.ExpiresAfterSeconds <= 0)
            return GenericResult.Failure(AnswerRequestLog.ExpiryNotPositive(_logger, ask.ComponentName, ask.PolicyName));

        if (string.IsNullOrWhiteSpace(ask.ComponentName) || string.IsNullOrWhiteSpace(ask.PolicyName) || string.IsNullOrWhiteSpace(ask.CallName) || string.IsNullOrWhiteSpace(ask.AnswerParameterName))
            return GenericResult.Failure(AnswerRequestLog.AskNamesMissing(_logger, ask.ExecutionId));

        return ShownProblem(ask) ?? ArgumentProblem(ask);
    }

    private IGenericResult? ShownProblem(AnswerRequestAsk ask)
    {
        if (!ask.Shown.Any(shown => string.Equals(shown.Key, AnswerRequestShown.Title, StringComparison.Ordinal)))
            return GenericResult.Failure(AnswerRequestLog.NoTitleShown(_logger, ask.ComponentName));

        var options = ask.Shown.Count(shown => string.Equals(shown.Key, AnswerRequestShown.Option, StringComparison.Ordinal));
        var mustBeAnOption = AnswerRequestKinds.ByName(ask.KindName).AnswerMustBeAnOption;
        if (mustBeAnOption && options == 0)
            return GenericResult.Failure(AnswerRequestLog.ChoiceNeedsOptions(_logger, ask.ComponentName));

        return !mustBeAnOption && options > 0 ? GenericResult.Failure(AnswerRequestLog.QuestionTakesNoOptions(_logger, ask.ComponentName)) : null;
    }

    private IGenericResult? ArgumentProblem(AnswerRequestAsk ask)
    {
        if (ask.Arguments.Any(argument => string.Equals(argument.Key, ask.AnswerParameterName, StringComparison.OrdinalIgnoreCase)))
            return GenericResult.Failure(AnswerRequestLog.ArgumentNamedLikeTheAnswer(_logger, ask.ComponentName, ask.AnswerParameterName));

        return ask.Arguments.Select(argument => argument.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() == ask.Arguments.Count
            ? null
            : GenericResult.Failure(AnswerRequestLog.ArgumentsRepeated(_logger, ask.ComponentName));
    }

    private static EtlPipelineAnswerRequestRecord RowOf(AnswerRequestAsk ask, DateTimeOffset now) => new()
    {
        TenantId = ask.TenantId,
        PersonId = ask.PersonId,
        ExecutionId = ask.ExecutionId,
        ClaimRootId = ask.ClaimRootId,
        ComponentName = ask.ComponentName,
        Iteration = ask.Iteration,
        KindName = ask.KindName,
        PolicyName = ask.PolicyName,
        CallName = ask.CallName,
        ContinuationPipelineId = ask.ContinuationPipelineId,
        AnswerParameterName = ask.AnswerParameterName,
        GrantId = ask.GrantId,
        StateName = Keeping,
        AskedAt = now,
        ExpiresAt = now.AddSeconds(ask.ExpiresAfterSeconds)
    };

    private async Task<IGenericResult<EtlPipelineAnswerRequestRecord>> ReadByStep(string store, AnswerRequestAsk ask, string? insertRefusal, CancellationToken cancellationToken)
    {
        var read = await Gateway.Execute<IEnumerable<EtlPipelineAnswerRequestRecord>>(
            Query.From<EtlPipelineAnswerRequestRecord>(store, PathName, RequestContainerName)
                .Where("TenantId", ask.TenantId).Where("ClaimRootId", ask.ClaimRootId).Where("ComponentName", ask.ComponentName).Where("Iteration", ask.Iteration).Build(),
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
            return GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.NotKept(_logger, ask.ComponentName, read.CurrentMessage));

        return read.Value.FirstOrDefault() is { } found
            ? GenericResult<EtlPipelineAnswerRequestRecord>.Success(found)
            : GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.NotKept(_logger, ask.ComponentName, insertRefusal));
    }

    // The parts are written again from the ask each time a request is found still being kept, so a host that went down between the row and its parts leaves nothing half written that anyone could read or answer;
    // the request opens only when every part is there.
    private async Task<IGenericResult> KeepParts(string store, AnswerRequestAsk ask, EtlPipelineAnswerRequestRecord row, CancellationToken cancellationToken)
    {
        foreach (var container in new[] { ShownContainerName, ArgumentContainerName, RoleContainerName })
        {
            var deleted = await Gateway.Execute<int>(Delete.From(container).DataStore(store).Path(PathName).Where("RequestId", row.Id).Build(), cancellationToken).ConfigureAwait(false);
            if (deleted.IsFailure)
                return GenericResult.Failure(AnswerRequestLog.NotKept(_logger, ask.ComponentName, deleted.CurrentMessage));
        }

        var shown = await InsertAll(store, ShownContainerName, ask.ComponentName, ask.Shown.Select((item, ordinal) =>
            new EtlPipelineAnswerRequestShownRecord { RequestId = row.Id, Ordinal = ordinal, Name = item.Key, ValueText = item.Value }), cancellationToken).ConfigureAwait(false);
        var arguments = shown.IsFailure ? shown : await InsertAll(store, ArgumentContainerName, ask.ComponentName, ask.Arguments.Select(item =>
            new EtlPipelineAnswerRequestArgumentRecord { RequestId = row.Id, Name = item.Key, ValueText = item.Value }), cancellationToken).ConfigureAwait(false);
        var roles = arguments.IsFailure ? arguments : await InsertAll(store, RoleContainerName, ask.ComponentName, ask.RunRoles.Select(role =>
            new EtlPipelineAnswerRequestRoleRecord { RequestId = row.Id, RoleName = role }), cancellationToken).ConfigureAwait(false);
        if (roles.IsFailure)
            return roles;

        var opened = await Gateway.Execute<int>(
            Update.In<EtlPipelineAnswerRequestStateChange>(RequestContainerName).DataStore(store).Path(PathName)
                .Where("Id", row.Id).Where("TenantId", row.TenantId).Where("StateName", Keeping)
                .Value(new EtlPipelineAnswerRequestStateChange { StateName = Open }),
            cancellationToken).ConfigureAwait(false);
        return opened.IsSuccess && opened.Value > 0
            ? GenericResult.Success()
            : GenericResult.Failure(AnswerRequestLog.NotKept(_logger, ask.ComponentName, opened.IsSuccess ? "the request was no longer being kept" : opened.CurrentMessage));
    }

    private async Task<IGenericResult> InsertAll<T>(string store, string container, string component, IEnumerable<T> rows, CancellationToken cancellationToken)
        where T : class
    {
        foreach (var row in rows)
        {
            var inserted = await Gateway.Execute<int>(Insert.Into<T>(container).DataStore(store).Path(PathName).Value(row), cancellationToken).ConfigureAwait(false);
            if (inserted.IsFailure)
                return GenericResult.Failure(AnswerRequestLog.NotKept(_logger, component, inserted.CurrentMessage));
        }

        return GenericResult.Success();
    }

    private IGenericResult<Caller> CallerOf(string operation)
    {
        if (RunPerson.Of(_authentication.Current) is not { } person)
            return GenericResult<Caller>.Failure(AnswerRequestLog.CallerIsNoPerson(_logger, operation));

        return RunTenant.Of(_authentication.Current) is { } tenant
            ? GenericResult<Caller>.Success(new Caller(person, tenant))
            : GenericResult<Caller>.Failure(AnswerRequestLog.CallerHasNoTenant(_logger, operation));
    }

    // The requests of the caller whose end has passed become Expired, so what is read is true. It is one update, judged by the end the policy gave each request when it was asked.
    private async Task<IGenericResult> ExpireDue(string store, Caller caller, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var expired = await Gateway.Execute<int>(
            Update.In<EtlPipelineAnswerRequestStateChange>(RequestContainerName).DataStore(store).Path(PathName)
                .Where("TenantId", caller.Tenant).Where("PersonId", caller.Person).Where("StateName", Open).Where("ExpiresAt", FilterOperators.ByName("LessThanOrEqual"), now)
                .Value(new EtlPipelineAnswerRequestStateChange { StateName = Expired }),
            cancellationToken).ConfigureAwait(false);
        if (expired.IsFailure)
            return GenericResult.Failure(AnswerRequestLog.NotRead(_logger, "the requests that passed their end", expired.CurrentMessage));

        if (expired.Value > 0)
            _ = AnswerRequestLog.ExpiredNow(_logger, expired.Value, caller.Person);

        return GenericResult.Success();
    }

    // One request of the caller. A request that is not in the caller's tenant, or is still being kept, is not found; one kept for another person is not theirs.
    private async Task<IGenericResult<EtlPipelineAnswerRequestRecord>> ReadOwn(string store, Caller caller, Guid requestId, CancellationToken cancellationToken)
    {
        var read = await Gateway.Execute<IEnumerable<EtlPipelineAnswerRequestRecord>>(
            Query.From<EtlPipelineAnswerRequestRecord>(store, PathName, RequestContainerName).Where("Id", requestId).Where("TenantId", caller.Tenant).Build(),
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
            return GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.NotRead(_logger, "the request", read.CurrentMessage));

        if (read.Value.FirstOrDefault() is not { } row)
            return GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.NotFound(_logger, requestId));

        if (!AnswerRequestStates.ByName(row.StateName).IsReadable)
            return GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.NotFound(_logger, requestId));

        return row.PersonId == caller.Person
            ? GenericResult<EtlPipelineAnswerRequestRecord>.Success(row)
            : GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.NotYours(_logger, requestId));
    }

    private async Task<IGenericResult<IReadOnlyList<KeyValuePair<string, string>>>> ReadShown(string store, Guid requestId, CancellationToken cancellationToken)
    {
        var read = await Gateway.Execute<IEnumerable<EtlPipelineAnswerRequestShownRecord>>(
            Query.From<EtlPipelineAnswerRequestShownRecord>(store, PathName, ShownContainerName).Where("RequestId", requestId).Build(), cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is null
            ? GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Failure(AnswerRequestLog.NotRead(_logger, "what the person is shown", read.CurrentMessage))
            : GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Success([.. read.Value.OrderBy(row => row.Ordinal).Select(row => new KeyValuePair<string, string>(row.Name, row.ValueText))]);
    }

    private async Task<IGenericResult<AnswerRequest>> Present(string store, EtlPipelineAnswerRequestRecord row, CancellationToken cancellationToken)
    {
        var shown = await ReadShown(store, row.Id, cancellationToken).ConfigureAwait(false);
        if (shown.IsFailure || shown.Value is not { } items)
            return shown.ToNewResult<AnswerRequest>();

        var kind = AnswerRequestKinds.ByName(row.KindName);
        var state = AnswerRequestStates.ByName(row.StateName);
        if (ReferenceEquals(kind, AnswerRequestKinds.NotFound) || ReferenceEquals(state, AnswerRequestStates.NotFound))
            return GenericResult<AnswerRequest>.Failure(AnswerRequestLog.NotRead(_logger, "the request", $"its kind '{row.KindName}' or state '{row.StateName}' is none of the options"));

        return GenericResult<AnswerRequest>.Success(new AnswerRequest
        {
            Id = row.Id,
            TenantId = row.TenantId,
            PersonId = row.PersonId,
            ExecutionId = row.ExecutionId,
            Kind = kind,
            PolicyName = row.PolicyName,
            CallName = row.CallName,
            State = state,
            AskedAt = row.AskedAt,
            ExpiresAt = row.ExpiresAt,
            AnnouncedAt = row.AnnouncedAt,
            AnsweredAt = row.AnsweredAt,
            ContinuationExecutionId = row.ContinuationExecutionId,
            Shown = items
        });
    }

    // The answer is taken once: an Open request that has not ended takes it by the one conditional update; a request that was answered and whose start is owed takes only the same answer again (so the start can be finished);
    // any other request takes none.
    private async Task<IGenericResult<EtlPipelineAnswerRequestRecord>> TakeTheAnswer(
        string store, Caller caller, EtlPipelineAnswerRequestRecord row, string answer, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var state = AnswerRequestStates.ByName(row.StateName);
        if (state.OwesTheStart)
        {
            return string.Equals(answer, row.AnswerText, StringComparison.Ordinal)
                ? GenericResult<EtlPipelineAnswerRequestRecord>.Success(row)
                : GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.AnswerDiffers(_logger, row.Id));
        }

        if (!state.AcceptsAnAnswer)
        {
            return string.Equals(row.StateName, Expired, StringComparison.Ordinal)
                ? GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.Expired(_logger, row.Id, row.ExpiresAt))
                : GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.AlreadyAnswered(_logger, row.Id, row.StateName));
        }

        var option = await OptionJudgment(store, row, answer, cancellationToken).ConfigureAwait(false);
        if (option.IsFailure)
            return option.ToNewResult<EtlPipelineAnswerRequestRecord>();

        var changed = await Gateway.Execute<int>(
            Update.In<EtlPipelineAnswerRequestAnswer>(RequestContainerName).DataStore(store).Path(PathName)
                .Where("Id", row.Id).Where("TenantId", caller.Tenant).Where("PersonId", caller.Person).Where("StateName", Open).Where("ExpiresAt", FilterOperators.ByName("GreaterThan"), now)
                .Value(new EtlPipelineAnswerRequestAnswer { StateName = Answered, AnsweredAt = now, AnswerText = answer }),
            cancellationToken).ConfigureAwait(false);
        if (changed.IsFailure)
            return GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.AnswerNotRecorded(_logger, row.Id, changed.CurrentMessage));

        if (changed.Value == 0)
            return await WhyNotTaken(store, caller, row.Id, cancellationToken).ConfigureAwait(false);

        _ = AnswerRequestLog.Answered(_logger, row.Id, caller.Person);
        row.StateName = Answered;
        row.AnsweredAt = now;
        row.AnswerText = answer;
        return GenericResult<EtlPipelineAnswerRequestRecord>.Success(row);
    }

    // A choice takes one of its options and nothing else.
    private async Task<IGenericResult> OptionJudgment(string store, EtlPipelineAnswerRequestRecord row, string answer, CancellationToken cancellationToken)
    {
        if (!AnswerRequestKinds.ByName(row.KindName).AnswerMustBeAnOption)
            return GenericResult.Success();

        var shown = await ReadShown(store, row.Id, cancellationToken).ConfigureAwait(false);
        if (shown.IsFailure || shown.Value is not { } items)
            return shown;

        return items.Any(item => string.Equals(item.Key, AnswerRequestShown.Option, StringComparison.Ordinal) && string.Equals(item.Value, answer, StringComparison.Ordinal))
            ? GenericResult.Success()
            : GenericResult.Failure(AnswerRequestLog.AnswerNotAnOption(_logger, row.Id));
    }

    // The conditional update changed no row, so the request is read again to say why: another call answered first, its end passed, or nothing is known.
    private async Task<IGenericResult<EtlPipelineAnswerRequestRecord>> WhyNotTaken(string store, Caller caller, Guid requestId, CancellationToken cancellationToken)
    {
        var now = await ReadOwn(store, caller, requestId, cancellationToken).ConfigureAwait(false);
        if (now.IsFailure || now.Value is not { } row)
            return now;

        if (string.Equals(row.StateName, Expired, StringComparison.Ordinal) || (AnswerRequestStates.ByName(row.StateName).AcceptsAnAnswer && row.ExpiresAt <= _time.GetUtcNow()))
            return GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.Expired(_logger, requestId, row.ExpiresAt));

        return AnswerRequestStates.ByName(row.StateName).AcceptsAnAnswer
            ? GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.AnswerNotRecorded(_logger, requestId, "no row changed although the request is open"))
            : GenericResult<EtlPipelineAnswerRequestRecord>.Failure(AnswerRequestLog.AlreadyAnswered(_logger, requestId, row.StateName));
    }

    // The start: what the continuation is given is read from the request's rows, the start is claimed under the asking run's chain (only one call makes the claim), the continuation is started as the caller,
    // the request moves to Started and the claim records how it ended.
    private async Task<IGenericResult<AnswerRequestAnswered>> StartContinuation(string store, EtlPipelineAnswerRequestRecord row, CancellationToken cancellationToken)
    {
        if (row.AnswerText is not { } answerText)
            return GenericResult<AnswerRequestAnswered>.Failure(AnswerRequestLog.NotRead(_logger, "the answer", "the request is answered and holds no answer"));

        var arguments = await ReadArguments(store, row.Id, cancellationToken).ConfigureAwait(false);
        if (arguments.IsFailure || arguments.Value is not { } given)
            return arguments.ToNewResult<AnswerRequestAnswered>();

        var roles = await ReadRoles(store, row.Id, cancellationToken).ConfigureAwait(false);
        if (roles.IsFailure || roles.Value is not { } actsWith)
            return roles.ToNewResult<AnswerRequestAnswered>();

        var claim = await _claims.Claim(row.TenantId, row.ClaimRootId, ClaimNamePrefix + row.Id.ToString("D"), 0, row.ExecutionId, cancellationToken).ConfigureAwait(false);
        if (claim.IsFailure || claim.Value is not { } made)
            return GenericResult<AnswerRequestAnswered>.Failure(AnswerRequestLog.StartNotClaimed(_logger, row.Id, claim.CurrentMessage));

        if (!made.IsNew)
        {
            return string.Equals(made.Outcome, StepClaimOutcomes.Failed, StringComparison.Ordinal)
                ? GenericResult<AnswerRequestAnswered>.Failure(AnswerRequestLog.StartFailedBefore(_logger, row.Id, made.ExecutionId))
                : GenericResult<AnswerRequestAnswered>.Failure(AnswerRequestLog.StartOutcomeUnknown(_logger, row.Id, made.ExecutionId));
        }

        var started = await _runs.Start(new PipelineStart
        {
            PipelineId = row.ContinuationPipelineId,
            TriggerSource = TriggerSourcePrefix + row.Id.ToString("D"),
            TenantId = row.TenantId,
            RunRoles = actsWith,
            Parameters = [.. given, new KeyValuePair<string, string>(row.AnswerParameterName, answerText)],
            ParentExecutionId = row.ExecutionId,
            AwaitCompletion = false
        }, cancellationToken).ConfigureAwait(false);
        if (started.IsFailure)
        {
            await RecordOutcome(row, made, StepClaimOutcomes.Failed, cancellationToken).ConfigureAwait(false);
            return started.ToNewResult<AnswerRequestAnswered>();
        }

        return await Finish(store, row, made, started.Value, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<AnswerRequestAnswered>> Finish(string store, EtlPipelineAnswerRequestRecord row, StepClaim claim, Guid continuationId, CancellationToken cancellationToken)
    {
        var moved = await Gateway.Execute<int>(
            Update.In<EtlPipelineAnswerRequestStart>(RequestContainerName).DataStore(store).Path(PathName).Where("Id", row.Id).Where("TenantId", row.TenantId).Where("StateName", Answered)
                .Value(new EtlPipelineAnswerRequestStart { StateName = Started, ContinuationExecutionId = continuationId }),
            cancellationToken).ConfigureAwait(false);
        await RecordOutcome(row, claim, StepClaimOutcomes.Succeeded, cancellationToken).ConfigureAwait(false);
        if (moved.IsFailure || moved.Value == 0)
            return GenericResult<AnswerRequestAnswered>.Failure(AnswerRequestLog.StartNotRecorded(_logger, row.Id, continuationId, moved.IsSuccess ? "the request was no longer Answered" : moved.CurrentMessage));

        _ = AnswerRequestLog.Started(_logger, row.Id, continuationId);
        row.StateName = Started;
        row.ContinuationExecutionId = continuationId;
        var request = await Present(store, row, cancellationToken).ConfigureAwait(false);
        return request.IsFailure || request.Value is not { } read
            ? request.ToNewResult<AnswerRequestAnswered>()
            : GenericResult<AnswerRequestAnswered>.Success(new AnswerRequestAnswered(read, continuationId));
    }

    // How the start ended is written on the claim. A claim whose outcome cannot be written is logged, not returned: the continuation already began, and the request says so.
    private async Task RecordOutcome(EtlPipelineAnswerRequestRecord row, StepClaim claim, string outcome, CancellationToken cancellationToken)
    {
        var recorded = await _claims.Record(row.TenantId, claim.Id, outcome, cancellationToken).ConfigureAwait(false);
        if (recorded.IsFailure)
            _ = AnswerRequestLog.StartNotClaimed(_logger, row.Id, recorded.CurrentMessage);
    }

    private async Task<IGenericResult<IReadOnlyList<KeyValuePair<string, string>>>> ReadArguments(string store, Guid requestId, CancellationToken cancellationToken)
    {
        var read = await Gateway.Execute<IEnumerable<EtlPipelineAnswerRequestArgumentRecord>>(
            Query.From<EtlPipelineAnswerRequestArgumentRecord>(store, PathName, ArgumentContainerName).Where("RequestId", requestId).Build(), cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is null
            ? GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Failure(AnswerRequestLog.NotRead(_logger, "the arguments of the continuation", read.CurrentMessage))
            : GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Success([.. read.Value.Select(row => new KeyValuePair<string, string>(row.Name, row.ValueText))]);
    }

    private async Task<IGenericResult<IReadOnlyList<string>>> ReadRoles(string store, Guid requestId, CancellationToken cancellationToken)
    {
        var read = await Gateway.Execute<IEnumerable<EtlPipelineAnswerRequestRoleRecord>>(
            Query.From<EtlPipelineAnswerRequestRoleRecord>(store, PathName, RoleContainerName).Where("RequestId", requestId).Build(), cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is null
            ? GenericResult<IReadOnlyList<string>>.Failure(AnswerRequestLog.NotRead(_logger, "the roles of the continuation", read.CurrentMessage))
            : GenericResult<IReadOnlyList<string>>.Success([.. read.Value.Select(row => row.RoleName)]);
    }
}
