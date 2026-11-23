using System.Collections;
using System.Reflection;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution.Answering;

namespace Fdw.Services.Etl.Tests.Execution.Answering;

/// <summary>
/// The operational store's answer tables the way the engine behaves (FDW-6142): a unique key refuses a second request for the same step, a child needs its request, an update changes every row its filter matches
/// and says how many, and each command is taken one at a time (the engine's own atomicity), so a race between two answers is settled by the rows an update changed. It states every answer; nothing is assumed.
/// </summary>
internal sealed class InMemoryOperationalStore
{
    private readonly object _gate = new();

    public InMemoryOperationalStore()
    {
        Gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget target, CancellationToken _) => Write(command, target));
        ReadsOf<EtlPipelineAnswerRequestRecord>();
        ReadsOf<EtlPipelineAnswerRequestShownRecord>();
        ReadsOf<EtlPipelineAnswerRequestArgumentRecord>();
        ReadsOf<EtlPipelineAnswerRequestRoleRecord>();
    }

    /// <summary>Gets the gateway the service is given.</summary>
    public Mock<IDataGateway> Gateway { get; } = new();

    /// <summary>Gets the rows of <c>etl.PipelineAnswerRequest</c>.</summary>
    public List<EtlPipelineAnswerRequestRecord> Requests { get; } = [];

    /// <summary>Gets the rows of <c>etl.PipelineAnswerRequestShown</c>.</summary>
    public List<EtlPipelineAnswerRequestShownRecord> Shown { get; } = [];

    /// <summary>Gets the rows of <c>etl.PipelineAnswerRequestArgument</c>.</summary>
    public List<EtlPipelineAnswerRequestArgumentRecord> Arguments { get; } = [];

    /// <summary>Gets the rows of <c>etl.PipelineAnswerRequestRole</c>.</summary>
    public List<EtlPipelineAnswerRequestRoleRecord> Roles { get; } = [];

    /// <summary>Gets or sets the container whose writes fail, or null for none.</summary>
    public string? FailsWritesTo { get; set; }

    private IList Table(string container) => container switch
    {
        "PipelineAnswerRequest" => Requests,
        "PipelineAnswerRequestShown" => Shown,
        "PipelineAnswerRequestArgument" => Arguments,
        "PipelineAnswerRequestRole" => Roles,
        _ => throw new InvalidOperationException($"No table '{container}' is held here."),
    };

    private void ReadsOf<T>()
        where T : class =>
        Gateway.Setup(g => g.Execute<IEnumerable<T>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget target, CancellationToken _) =>
            {
                lock (_gate)
                {
                    return GenericResult<IEnumerable<T>>.Success(Table(target.Container).Cast<object>().Where(row => Matches(row, FilterOf(command))).Cast<T>().ToList());
                }
            });

    private IGenericResult<int> Write(IDataCommand command, DataStoreTarget target)
    {
        lock (_gate)
        {
            if (string.Equals(FailsWritesTo, target.Container, StringComparison.Ordinal))
                return GenericResult<int>.Failure(new GenericMessage("the store is down"));

            var table = Table(target.Container);
            var kind = command.GetType().Name;
            if (kind.StartsWith("InsertCommand", StringComparison.Ordinal))
                return Insert(target.Container, table, (IEnumerable)Read(command, "Data"));

            var matched = table.Cast<object>().Where(row => Matches(row, FilterOf(command))).ToList();
            if (kind.StartsWith("DeleteCommand", StringComparison.Ordinal))
            {
                foreach (var row in matched)
                    table.Remove(row);

                return GenericResult<int>.Success(matched.Count);
            }

            var data = Read(command, "Data");
            foreach (var row in matched)
            {
                foreach (var property in data.GetType().GetProperties())
                    Property(row, property.Name).SetValue(row, property.GetValue(data));
            }

            return GenericResult<int>.Success(matched.Count);
        }
    }

    private IGenericResult<int> Insert(string container, IList table, IEnumerable rows)
    {
        var count = 0;
        foreach (var row in rows)
        {
            if (table.Cast<object>().Any(existing => SameKey(container, existing, row)))
                return GenericResult<int>.Failure(new GenericMessage("a unique key refused the row"));

            if (!string.Equals(container, "PipelineAnswerRequest", StringComparison.Ordinal) && !Requests.Any(request => request.Id == (Guid)Read(row, "RequestId")))
                return GenericResult<int>.Failure(new GenericMessage("the foreign key to the request refused the row"));

            table.Add(row);
            count++;
        }

        return GenericResult<int>.Success(count);
    }

    private static bool SameKey(string container, object left, object right)
    {
        bool Same(params string[] names) => names.All(name => Equals(Read(left, name), Read(right, name)));
        return Same("Id") || container switch
        {
            "PipelineAnswerRequest" => Same("TenantId", "ClaimRootId", "ComponentName", "Iteration"),
            "PipelineAnswerRequestShown" => Same("RequestId", "Ordinal"),
            "PipelineAnswerRequestArgument" => Same("RequestId", "Name"),
            _ => Same("RequestId", "RoleName"),
        };
    }

    private static PropertyInfo Property(object row, string name) => row.GetType().GetProperty(name) ?? throw new InvalidOperationException($"{row.GetType().Name} has no property '{name}'.");

    private static object Read(object row, string name) => Property(row, name).GetValue(row) ?? throw new InvalidOperationException($"{row.GetType().Name}.{name} holds no value.");

    /// <summary>Gets the rows a command's filter matches, the way the store answers a read: a row that is not stated by the filter is not returned.</summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="rows">The rows the table holds.</param>
    /// <param name="command">The read, whose filter decides.</param>
    /// <returns>The matching rows.</returns>
    internal static List<T> RowsMatching<T>(IEnumerable<T> rows, IDataCommand command)
        where T : class =>
        [.. rows.Where(row => Matches(row, FilterOf(command)))];

    private static IFilterExpression? FilterOf(IDataCommand command) => command.GetType().GetProperty("Filter")?.GetValue(command) as IFilterExpression;

    private static bool Matches(object row, IFilterExpression? filter) => filter?.Root is not { } root || Holds(row, root);

    private static bool Holds(object row, IFilterNode node) => node switch
    {
        FilterGroup group => group.Operator == LogicalOperator.And ? group.Nodes.All(inner => Holds(row, inner)) : group.Nodes.Any(inner => Holds(row, inner)),
        FilterCondition condition => Compare(Read(row, condition.PropertyName), condition.Value, condition.Operator.Name),
        _ => throw new InvalidOperationException("A filter node this store does not read."),
    };

    private static bool Compare(object? held, object? wanted, string @operator)
    {
        if (@operator == "Equal")
            return Equals(held, wanted);

        var order = ((IComparable)(held ?? throw new InvalidOperationException("A value to compare is missing."))).CompareTo(wanted);
        return @operator switch
        {
            "GreaterThan" => order > 0,
            "LessThanOrEqual" => order <= 0,
            _ => throw new InvalidOperationException($"An operator '{@operator}' this store does not read."),
        };
    }
}
