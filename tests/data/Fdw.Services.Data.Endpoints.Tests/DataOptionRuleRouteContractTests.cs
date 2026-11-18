using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Multitenancy;
using Fdw.Services.Workspaces;
using Fdw.Web.Endpoints.Contracts;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6348 (stage 1 of FDW-6339): every data option rule route (17) is declared with its verb, path and policy. All seventeen are built now (FDW-6489 thirteen, FDW-6342 the tree, FDW-6566
/// the path check, FDW-6571 the history, FDW-6575 the limits); the coded RouteNotBuilt (404, DATAENDPOINTS-31700) stays declared for the next contract-first story.
/// </summary>
public sealed class DataOptionRuleRouteContractTests
{
    private const string B = "/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/option-rules";

    private static readonly Dictionary<string, Probe> Probes = new(StringComparer.Ordinal)
    {
        ["AddDataOptionRule"] = new(() => Factory.Create<AddDataOptionRule>(), ct => Answer<AddDataOptionRule, AddDataOptionRuleRequest, DataOptionRulePayload>(new AddDataOptionRuleRequest(), ct)),
        ["AddDataOptionValues"] = new(() => Factory.Create<AddDataOptionValues>(), ct => Answer<AddDataOptionValues, AddDataOptionValuesRequest, DataOptionRulePayload>(new AddDataOptionValuesRequest(), ct)),
        ["ChangeDataOptionRule"] = new(() => Factory.Create<ChangeDataOptionRule>(), ct => Answer<ChangeDataOptionRule, ChangeDataOptionRuleRequest, DataOptionRulePayload>(new ChangeDataOptionRuleRequest(), ct)),
        ["ChangeDataOptionValue"] = new(() => Factory.Create<ChangeDataOptionValue>(), ct => Answer<ChangeDataOptionValue, ChangeDataOptionValueRequest, DataOptionRulePayload>(new ChangeDataOptionValueRequest(), ct)),
        ["CheckDataOptionPath"] = new(() => Factory.Create<CheckDataOptionPath>(), ct => Answer<CheckDataOptionPath, CheckDataOptionPathRequest, DataOptionPathCheckPayload>(new CheckDataOptionPathRequest(), ct)),
        ["GetDataOptionLimits"] = new(() => Factory.Create<GetDataOptionLimits>(), ct => Answer<GetDataOptionLimits, DataOptionContainerRequest, DataOptionLimitsPayload>(new DataOptionContainerRequest(), ct)),
        ["GetDataOptionRule"] = new(() => Factory.Create<GetDataOptionRule>(), ct => Answer<GetDataOptionRule, DataOptionRuleKeyRequest, DataOptionRulePayload>(new DataOptionRuleKeyRequest(), ct)),
        ["GetDataOptionTree"] = new(() => Factory.Create<GetDataOptionTree>(), ct => Answer<GetDataOptionTree, EmptyRequest, DataOptionTreePayload>(EmptyRequest.Instance, ct)),
        ["ListDataOptionCustodians"] = new(() => Factory.Create<ListDataOptionCustodians>(), ct => Answer<ListDataOptionCustodians, ListDataOptionCustodiansRequest, PaginatedResponse<DataOptionCustodianPayload>>(new ListDataOptionCustodiansRequest(), ct)),
        ["ListDataOptionOwners"] = new(() => Factory.Create<ListDataOptionOwners>(), ct => Answer<ListDataOptionOwners, ListDataOptionOwnersRequest, PaginatedResponse<DataOptionOwnerChoicePayload>>(new ListDataOptionOwnersRequest(), ct)),
        ["ListDataOptionPermissions"] = new(() => Factory.Create<ListDataOptionPermissions>(), ct => Answer<ListDataOptionPermissions, EmptyRequest, List<string>>(EmptyRequest.Instance, ct)),
        ["ListDataOptionRuleHistory"] = new(() => Factory.Create<ListDataOptionRuleHistory>(), ct => Answer<ListDataOptionRuleHistory, ListDataOptionRuleHistoryRequest, PaginatedResponse<DataOptionRuleChangePayload>>(new ListDataOptionRuleHistoryRequest(), ct)),
        ["ListDataOptionRules"] = new(() => Factory.Create<ListDataOptionRules>(), ct => Answer<ListDataOptionRules, ListDataOptionRulesRequest, PaginatedResponse<DataOptionRulePayload>>(new ListDataOptionRulesRequest(), ct)),
        ["RemoveDataOptionRule"] = new(() => Factory.Create<RemoveDataOptionRule>(), ct => Answer<RemoveDataOptionRule, DataOptionRuleKeyRequest, DataOptionRulePayload>(new DataOptionRuleKeyRequest(), ct)),
        ["RemoveDataOptionValues"] = new(() => Factory.Create<RemoveDataOptionValues>(), ct => Answer<RemoveDataOptionValues, RemoveDataOptionValuesRequest, DataOptionRulePayload>(new RemoveDataOptionValuesRequest(), ct)),
        ["RestoreDataOptionRule"] = new(() => Factory.Create<RestoreDataOptionRule>(), ct => Answer<RestoreDataOptionRule, DataOptionRuleKeyRequest, DataOptionRulePayload>(new DataOptionRuleKeyRequest(), ct)),
        ["SwitchDataOptionRule"] = new(() => Factory.Create<SwitchDataOptionRule>(), ct => Answer<SwitchDataOptionRule, SwitchDataOptionRuleRequest, DataOptionRulePayload>(new SwitchDataOptionRuleRequest(), ct)),
    };

    public static TheoryData<string, string, string, string> Routes => new()
    {
        { "AddDataOptionRule", "POST", B, "datastores:write" },
        { "AddDataOptionValues", "POST", B + "/{RuleId}/values", "datastores:write" },
        { "ChangeDataOptionRule", "PATCH", B + "/{RuleId}", "datastores:write" },
        { "ChangeDataOptionValue", "PATCH", B + "/{RuleId}/values/{ValueId}", "datastores:write" },
        { "CheckDataOptionPath", "POST", "/data-option-paths/check", "datastores:read" },
        { "GetDataOptionLimits", "POST", B + "/limits/read", "datastores:read" },
        { "GetDataOptionRule", "POST", B + "/{RuleId}/read", "datastores:read" },
        { "GetDataOptionTree", "POST", "/data-option-tree/read", "datastores:read" },
        { "ListDataOptionCustodians", "POST", "/data-option-custodians/search", "datastores:write" },
        { "ListDataOptionOwners", "POST", "/data-option-owners/search", "datastores:write" },
        { "ListDataOptionPermissions", "POST", "/data-option-permissions/read", "datastores:write" },
        { "ListDataOptionRuleHistory", "POST", B + "/history/search", "datastores:read" },
        { "ListDataOptionRules", "POST", B + "/search", "datastores:read" },
        { "RemoveDataOptionRule", "DELETE", B + "/{RuleId}", "datastores:write" },
        { "RemoveDataOptionValues", "DELETE", B + "/{RuleId}/values", "datastores:write" },
        { "RestoreDataOptionRule", "POST", B + "/{RuleId}/restore", "datastores:write" },
        { "SwitchDataOptionRule", "POST", B + "/{RuleId}/active", "datastores:write" },
    };

    [Theory]
    [MemberData(nameof(Routes))]
    [Trait("Priority", "P1")]
    public void TheRouteIsDeclaredWithItsVerbPathAndPolicy(string route, string verb, string path, string policy)
    {
        // Factory.Create has already run Configure, as the host does once at start: calling it again would add the policy twice.
        var endpoint = Probes[route].Create();
        endpoint.Definition.ShouldNotBeNull();

        endpoint.Definition.Verbs.ShouldNotBeNull();
        endpoint.Definition.Verbs.ShouldBe(new[] { verb });
        endpoint.Definition.Routes.ShouldNotBeNull();
        endpoint.Definition.Routes.ShouldBe(new[] { path });
        endpoint.Definition.PreBuiltUserPolicies.ShouldNotBeNull();
        endpoint.Definition.PreBuiltUserPolicies.ShouldBe(new[] { policy });
    }

    // Every route of the contract is built (FDW-6489, FDW-6342, FDW-6566, FDW-6571, FDW-6575): none answers RouteNotBuilt now. The helper and its code stay declared for the next contract-first story.
    [Fact]
    [Trait("Priority", "P1")]
    public void TheNotBuiltAnswerCarriesItsOwnCodeAndNamesTheRoute()
    {
        var (status, body) = ResultHttpStatusMapper.Map(new GetDataOptionLimits().Refusal(), new DefaultHttpContext());

        status.ShouldBe(404);
        body.Extensions["code"].ShouldBe("DATAENDPOINTS-31700");
        body.Detail.ShouldNotBeNull();
        body.Detail.ShouldContain("GetDataOptionLimits");
    }

    private static async Task<int> Answer<TEndpoint, TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TEndpoint : DataOptionRuleContractEndpointBase<TRequest, TResponse>
        where TRequest : notnull
    {
        var endpoint = Factory.Create<TEndpoint>();
        await endpoint.HandleAsync(request, cancellationToken);
        return endpoint.HttpContext.Response.StatusCode;
    }

    private sealed record Probe(Func<BaseEndpoint> Create, Func<CancellationToken, Task<int>> Answer);

    private sealed class AddDataOptionRule : AddDataOptionRuleEndpointBase
    {
        public AddDataOptionRule()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class AddDataOptionValues : AddDataOptionValuesEndpointBase
    {
        public AddDataOptionValues()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class ChangeDataOptionRule : ChangeDataOptionRuleEndpointBase
    {
        public ChangeDataOptionRule()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class ChangeDataOptionValue : ChangeDataOptionValueEndpointBase
    {
        public ChangeDataOptionValue()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class CheckDataOptionPath : CheckDataOptionPathEndpointBase
    {
        public CheckDataOptionPath()
            : base(NullLogger.Instance)
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class GetDataOptionLimits : GetDataOptionLimitsEndpointBase
    {
        public GetDataOptionLimits()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class GetDataOptionRule : GetDataOptionRuleEndpointBase
    {
        public GetDataOptionRule()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class GetDataOptionTree : GetDataOptionTreeEndpointBase
    {
        public GetDataOptionTree()
            : base(NullLogger.Instance)
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class ListDataOptionCustodians : ListDataOptionCustodiansEndpointBase
    {
        public ListDataOptionCustodians()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class ListDataOptionOwners : ListDataOptionOwnersEndpointBase
    {
        public ListDataOptionOwners()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRuleNames>(), Mock.Of<IOrganizationConfigurationProvider>(), Mock.Of<IWorkspaceConfigurationProvider>(), Mock.Of<IWorkspaceAccessPolicy>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class ListDataOptionPermissions : ListDataOptionPermissionsEndpointBase
    {
        public ListDataOptionPermissions()
            : base(NullLogger.Instance, Mock.Of<IAuthenticationContextAccessor>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class ListDataOptionRuleHistory : ListDataOptionRuleHistoryEndpointBase
    {
        public ListDataOptionRuleHistory()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class ListDataOptionRules : ListDataOptionRulesEndpointBase
    {
        public ListDataOptionRules()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class RemoveDataOptionRule : RemoveDataOptionRuleEndpointBase
    {
        public RemoveDataOptionRule()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class RemoveDataOptionValues : RemoveDataOptionValuesEndpointBase
    {
        public RemoveDataOptionValues()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class RestoreDataOptionRule : RestoreDataOptionRuleEndpointBase
    {
        public RestoreDataOptionRule()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }

    private sealed class SwitchDataOptionRule : SwitchDataOptionRuleEndpointBase
    {
        public SwitchDataOptionRule()
            : base(NullLogger.Instance, Mock.Of<IDataConfigurationRules>(), Mock.Of<IDataConfigurationRuleNames>())
        {
        }

        public IGenericResult Refusal() => NotBuilt();
    }
}
