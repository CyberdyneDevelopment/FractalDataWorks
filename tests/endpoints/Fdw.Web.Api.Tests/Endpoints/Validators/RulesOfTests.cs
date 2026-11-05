using Fdw.Validation.FastEndpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Validators;

/// <summary>Tests for <c>FdwEndpointValidator.RulesOf</c>: a configuration class that declares no validator is a failure, never a pass (FDW-3884).</summary>
public sealed class RulesOfTests
{
    private sealed class UndeclaredConfiguration;

    private sealed class Request
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class Sut : FdwEndpointValidator<Request>
    {
        public Sut() => RulesOf(_ => new UndeclaredConfiguration());
    }

    [Fact]
    public void AConfigurationClassThatDeclaresNoValidatorIsAFailure() =>
        new Sut().Validate(new Request { Name = "x" }).Errors
            .ShouldContain(e => e.PropertyName == nameof(UndeclaredConfiguration));
}
