using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Tests that sign-in admission decides from the methods a federated step's authority permits, read when asked (FDW-4697): the step declares none of its own and keeps nothing, so
/// the methods cannot come from a field a login left behind.
/// </summary>
[Trait("Category", "Security")]
public sealed class AuthorityAssertingStepAdmissionTests
{
    private sealed class FederatedStep(Func<IGenericResult<IReadOnlyList<string>>> authority) : IAuthorityAssertingAuthenticationStep
    {
        public string Name => "Federated";

        public IReadOnlyList<IContextElement> Requires => [];

        public IReadOnlyList<IContextElement> Contributes => [ContextElements.Subject];

        public IReadOnlyList<string> AuthenticationMethods => [];

        public Task<IGenericResult<IReadOnlyList<string>>> ProvableMethods(CancellationToken cancellationToken = default) => Task.FromResult(authority());

        public Task<IGenericResult<StepOutcome>> Execute(AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static IGenericResult<IReadOnlyList<string>> Permits(params string[] methods) => GenericResult<IReadOnlyList<string>>.Success(methods);

    private sealed class Proving(params string[] methods) : IAuthenticationStep
    {
        public string Name => "Proving";

        public IReadOnlyList<IContextElement> Requires => [];

        public IReadOnlyList<IContextElement> Contributes => [ContextElements.Subject];

        public IReadOnlyList<string> AuthenticationMethods => methods;

        public Task<IGenericResult<StepOutcome>> Execute(AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static SignInMethodAdmission Admission(IAuthorityAssertingAuthenticationStep step, IAuthenticationStep? alongside = null)
    {
        var flow = new AuthenticationFlow
        {
            Name = "federated-flow",
            Steps = alongside is null ? FlowSteps.Named("federated") : FlowSteps.Named("federated", "alongside"),
            RequiredPermissions = [],
            Audience = "test-audience",
            ExecutionLifetime = TimeSpan.FromMinutes(5),
        };
        var steps = new NamedSteps().Add("federated", step);
        if (alongside is not null)
            steps.Add("alongside", alongside);

        return new SignInMethodAdmission(
            new StaticFlowProvider(flow), Mock.Of<ITenantResolver>(), Mock.Of<IUserPermissions>(), new AuthenticationContextAccessor(), steps.Lookup);
    }

    [Fact]
    public async Task AMethodTheAuthorityPermitsIsAdmitted()
    {
        var result = await Admission(new FederatedStep(() => Permits("pwd", "mfa"))).MayUse(Guid.NewGuid(), "mfa", TestContext.Current.CancellationToken);

        result.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task AMethodTheAuthorityDoesNotPermitIsRefused()
    {
        var result = await Admission(new FederatedStep(() => Permits("pwd"))).MayUse(Guid.NewGuid(), "mfa", TestContext.Current.CancellationToken);

        result.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task AnAuthorityThatPermitsNothingAdmitsNothing()
    {
        var result = await Admission(new FederatedStep(() => Permits())).MayUse(Guid.NewGuid(), "pwd", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task AnAuthorityThatCannotBeReadRefusesWithItsOwnReason()
    {
        var unreadable = new FederatedStep(() => GenericResult<IReadOnlyList<string>>.Failure(new GenericMessage("The foreign authority 'corp' could not be read.")));

        var result = await Admission(unreadable).MayUse(Guid.NewGuid(), "pwd", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage!.ShouldContain("could not be read");
    }

    [Fact]
    public async Task AnUnreadableAuthorityDoesNotRefuseAMethodAnotherStepOfTheFlowProves()
    {
        var unreadable = new FederatedStep(() => GenericResult<IReadOnlyList<string>>.Failure(new GenericMessage("The foreign authority 'corp' could not be read.")));

        var result = await Admission(unreadable, alongside: new Proving("pwd")).MayUse(Guid.NewGuid(), "pwd", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task TheAnswerFollowsTheAuthorityEachTimeItIsAsked()
    {
        var permitted = new List<string> { "mfa" };
        var admission = Admission(new FederatedStep(() => Permits([.. permitted])));

        (await admission.MayUse(Guid.NewGuid(), "mfa", TestContext.Current.CancellationToken)).Value.ShouldBeTrue();
        permitted.Clear();
        (await admission.MayUse(Guid.NewGuid(), "mfa", TestContext.Current.CancellationToken)).Value.ShouldBeFalse();
    }
}
