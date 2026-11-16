using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5664: the option-row routes sit under the container they are about. Reading takes datastores:read and writing takes
/// datastores:write. Each route hands the whole request to the service and answers the service's rows or its refusal as they are.
/// </summary>
public sealed class DataOptionRowEndpointsTests
{
    private const string Options = "/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void EachRouteSitsUnderItsContainerAndReadsOrWritesTheDataStore()
    {
        var rows = new RecordingRows();

        var list = new ListRoute(rows);
        (list.RegisteredRoute, list.RegisteredPolicy).ShouldBe((Options, "datastores:read"));
        var add = new AddRoute(rows);
        (add.RegisteredRoute, add.RegisteredPolicy).ShouldBe((Options, "datastores:write"));
        var change = new ChangeRoute(rows);
        (change.RegisteredRoute, change.RegisteredPolicy).ShouldBe((Options + "/{Id}", "datastores:write"));
        var remove = new RemoveRoute(rows);
        (remove.RegisteredRoute, remove.RegisteredPolicy).ShouldBe((Options, "datastores:write"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnAddHandsEveryRowToTheServiceAndAnswersThemWithTheirIds()
    {
        var stored = Row();
        var rows = new RecordingRows { Added = GenericResult<IReadOnlyList<DataOptionRow>>.Success([stored]) };

        var added = await new AddRoute(rows).Run(new AddDataOptionRowsRequest
        {
            Name = "Places", PathName = "dbo", ContainerName = "Visits",
            Rows = [new NewDataOptionRowPayload { Column = "City", Value = "Bern", OptionType = "SelectList", Option = "Include", Active = null }],
        }, TestContext.Current.CancellationToken);

        var asked = rows.Addition.ShouldNotBeNull();
        (asked.Target.DataStore, asked.Target.Path, asked.Target.Container).ShouldBe(("Places", "dbo", "Visits"));
        asked.Rows.Single().Active.ShouldBeNull();
        added.Value!.Items.Single().Id.ShouldBe(stored.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AChangeStatesOnlyWhatTheBodyStates()
    {
        var stored = Row();
        var rows = new RecordingRows { Changed = GenericResult<DataOptionRow>.Success(stored) };

        await new ChangeRoute(rows).Run(new ChangeDataOptionRowRequest { Name = "Places", PathName = "dbo", ContainerName = "Visits", Id = stored.Id, Option = "Exclude" }, TestContext.Current.CancellationToken);

        var asked = rows.Changing.ShouldNotBeNull();
        (asked.Key.Id, asked.Option, asked.Active, asked.Value).ShouldBe((stored.Id, "Exclude", (bool?)null, (string?)null));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARemoveRefusalIsAnsweredWithItsOwnCode()
    {
        var rows = new RecordingRows { Removed = GenericResult<int>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired")) };
        var id = Guid.CreateVersion7();

        var removed = await new RemoveRoute(rows).Run(new RemoveDataOptionRowsRequest { Name = "Places", PathName = "dbo", ContainerName = "Visits", Id = [id] }, TestContext.Current.CancellationToken);

        removed.IsFailure.ShouldBeTrue();
        removed.Code.ShouldBeSameAs(DataServiceResultCodes.ByName("DataStoreNameRequired"));
        rows.Removal!.Ids.ShouldBe([id]);
    }

    private static DataOptionRow Row() => new()
    {
        Id = Guid.CreateVersion7(), DataStore = "Places", Container = "Visits", Column = "City", Value = "Bern", OptionType = "SelectList", Option = "Include", Active = true,
    };

    private sealed class RecordingRows : IDataOptionRows
    {
        public IGenericResult<IReadOnlyList<DataOptionRow>> Added { get; init; } = GenericResult<IReadOnlyList<DataOptionRow>>.Success([]);

        public IGenericResult<DataOptionRow> Changed { get; init; } = GenericResult<DataOptionRow>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

        public IGenericResult<int> Removed { get; init; } = GenericResult<int>.Success(0);

        public DataOptionRowsAddition? Addition { get; private set; }

        public DataOptionRowChange? Changing { get; private set; }

        public DataOptionRowsRemoval? Removal { get; private set; }

        public Task<IGenericResult<DataOptionRowsPage>> List(DataOptionRowsQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<DataOptionRowsPage>>(GenericResult<DataOptionRowsPage>.Success(new DataOptionRowsPage { Items = [], HasMore = false }));

        public Task<IGenericResult<DataOptionRow?>> Get(DataOptionRowKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<DataOptionRow?>>(GenericResult<DataOptionRow?>.Success(null));

        public Task<IGenericResult<IReadOnlyList<DataOptionRow>>> Add(DataOptionRowsAddition addition, CancellationToken cancellationToken = default)
        {
            Addition = addition;
            return Task.FromResult(Added);
        }

        Task<IGenericResult<DataOptionRow>> IDataOptionRows.Change(DataOptionRowChange change, CancellationToken cancellationToken)
        {
            Changing = change;
            return Task.FromResult(Changed);
        }

        public Task<IGenericResult<int>> Remove(DataOptionRowsRemoval removal, CancellationToken cancellationToken = default)
        {
            Removal = removal;
            return Task.FromResult(Removed);
        }
    }

    private sealed class ListRoute : ListDataOptionRowsEndpointBase
    {
        public ListRoute(IDataOptionRows rows) : base(NullLogger<ListDataOptionRowsEndpointBase>.Instance, rows)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => ReadPolicy;
    }

    private sealed class AddRoute : AddDataOptionRowsEndpointBase
    {
        public AddRoute(IDataOptionRows rows) : base(NullLogger<AddDataOptionRowsEndpointBase>.Instance, rows)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => WritePolicy;

        public Task<IGenericResult<DataOptionRowsPayload>> Run(AddDataOptionRowsRequest request, CancellationToken ct) => Create(request, ct);
    }

    private sealed class ChangeRoute : ChangeDataOptionRowEndpointBase
    {
        public ChangeRoute(IDataOptionRows rows) : base(NullLogger<ChangeDataOptionRowEndpointBase>.Instance, rows)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => WritePolicy;

        public Task<IGenericResult<DataOptionRowPayload>> Run(ChangeDataOptionRowRequest request, CancellationToken ct)
            => Update(request, new DataOptionRowPayload(), ct);
    }

    private sealed class RemoveRoute : RemoveDataOptionRowsEndpointBase
    {
        public RemoveRoute(IDataOptionRows rows) : base(NullLogger<RemoveDataOptionRowsEndpointBase>.Instance, rows)
        {
        }

        public string RegisteredRoute => Route;

        public string RegisteredPolicy => DeletePolicy;

        public Task<IGenericResult> Run(RemoveDataOptionRowsRequest request, CancellationToken ct) => Delete(request, ct);
    }
}
