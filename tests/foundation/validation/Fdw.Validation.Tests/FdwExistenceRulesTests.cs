using Fdw.Collections;

namespace Fdw.Validation.Tests;

public sealed class ExistenceRulesModel
{
    public string Kind { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public Guid ParentId { get; set; }
}

public sealed class ExistenceRulesValidator : AbstractValidator<ExistenceRulesModel>
{
    public ExistenceRulesValidator(IReadOnlyCollection<ITypeOption> options, Func<string, object?> parentByName, Func<Guid, object?> parentById)
    {
        RuleFor(x => x.Kind).MustExistIn(() => options);
        RuleFor(x => x.ParentName).ParentMustExist(parentByName);
        RuleFor(x => x.ParentId).ParentMustExist(parentById);
    }
}

public sealed class OptionalExistenceRulesValidator : AbstractValidator<ExistenceRulesModel>
{
    public OptionalExistenceRulesValidator(IReadOnlyCollection<ITypeOption> options, Func<string, object?> parentByName, Func<Guid, object?> parentById)
    {
        RuleFor(x => x.Kind).MustExistIn(() => options).When(x => !string.IsNullOrEmpty(x.Kind));
        RuleFor(x => x.ParentName).ParentMustExist(parentByName).When(x => !string.IsNullOrEmpty(x.ParentName));
        RuleFor(x => x.ParentId).ParentMustExist(parentById).When(x => x.ParentId != Guid.Empty);
    }
}

/// <summary>
/// The existence rules fail on an empty value (FDW-3540): a value that names nothing does not name something that exists. A property that
/// may be left empty says so at its call site with <c>.When</c>.
/// </summary>
public sealed class FdwExistenceRulesTests
{
    private static readonly Guid KnownParent = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static IReadOnlyCollection<ITypeOption> Options()
    {
        var known = new Mock<ITypeOption>();
        known.Setup(option => option.Name).Returns("Known");
        known.Setup(option => option.Category).Returns("Test");
        return [known.Object];
    }

    private static ExistenceRulesValidator Strict() =>
        new(Options(), name => string.Equals(name, "Parent", StringComparison.Ordinal) ? new object() : null, id => id == KnownParent ? new object() : null);

    private static OptionalExistenceRulesValidator Optional() =>
        new(Options(), name => string.Equals(name, "Parent", StringComparison.Ordinal) ? new object() : null, id => id == KnownParent ? new object() : null);

    private static ExistenceRulesModel Valid() => new() { Kind = "Known", ParentName = "Parent", ParentId = KnownParent };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AValueThatExistsPassesEveryRule() => Strict().Validate(Valid()).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MustExistInFailsOnAnEmptyValue(string? empty)
    {
        var model = Valid();
        model.Kind = empty!;

        var result = Strict().Validate(model);

        result.IsValid.ShouldBeFalse();
        result.Errors.Single().PropertyName.ShouldBe(nameof(ExistenceRulesModel.Kind));
        result.Errors.Single().ErrorMessage.ShouldContain("A value is required");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MustExistInFailsOnAValueThatIsNotAnOption()
    {
        var model = Valid();
        model.Kind = "Missing";

        var result = Strict().Validate(model);

        result.Errors.Single().ErrorMessage.ShouldContain("'Missing' was not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ParentMustExistFailsOnAnEmptyName(string? empty)
    {
        var model = Valid();
        model.ParentName = empty!;

        var result = Strict().Validate(model);

        result.IsValid.ShouldBeFalse();
        result.Errors.Single().PropertyName.ShouldBe(nameof(ExistenceRulesModel.ParentName));
        result.Errors.Single().ErrorMessage.ShouldContain("A value is required");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ParentMustExistFailsOnANameWithNoParent()
    {
        var model = Valid();
        model.ParentName = "Missing";

        var result = Strict().Validate(model);

        result.Errors.Single().ErrorMessage.ShouldContain("'Missing' which does not exist");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ParentMustExistFailsOnAnEmptyId()
    {
        var model = Valid();
        model.ParentId = Guid.Empty;

        var result = Strict().Validate(model);

        result.IsValid.ShouldBeFalse();
        result.Errors.Single().PropertyName.ShouldBe(nameof(ExistenceRulesModel.ParentId));
        result.Errors.Single().ErrorMessage.ShouldContain("A value is required");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ParentMustExistFailsOnAnIdWithNoParent()
    {
        var model = Valid();
        model.ParentId = Guid.NewGuid();

        var result = Strict().Validate(model);

        result.Errors.Single().ErrorMessage.ShouldContain("ID that does not exist");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AnOptionalPropertyPassesWhenEmptyBecauseTheCallSiteSaysSo()
    {
        var model = new ExistenceRulesModel();

        Optional().Validate(model).IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AnOptionalPropertyThatIsFilledInIsStillChecked()
    {
        var model = new ExistenceRulesModel { Kind = "Missing", ParentName = "Missing", ParentId = Guid.NewGuid() };

        Optional().Validate(model).Errors.Count.ShouldBe(3);
    }
}
