using System;
using FluentValidation;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Validation;

namespace Fdw.Data.DataSets;

/// <summary>Validator for <see cref="GraphDataSetIntentConfiguration"/> (FDW-5294).</summary>
/// <remarks>
/// What an intent must and must not name comes from its <see cref="GraphIntentKinds"/> option, never from a switch on the kind's name. The
/// rules mirror the row CHECKs on <c>data.GraphDataSetIntent</c> so a wrong intent is refused with a message before the database is reached.
/// An intent is never required to name a container, mapping or filter, and nothing here refuses it for not being bound.
/// </remarks>
public sealed class GraphDataSetIntentConfigurationValidator : FdwConfigurationValidator<GraphDataSetIntentConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetIntentConfigurationValidator"/> class.</summary>
    public GraphDataSetIntentConfigurationValidator()
    {
        RuleFor(x => x.IntentKind)
            .NotEmpty()
            .WithMessage("An intended source must say what it names");

        RuleFor(x => x.IntentKind)
            .Must(kind => !ReferenceEquals(GraphIntentKinds.ByName(kind), GraphIntentKinds.NotFound))
            .When(x => !string.IsNullOrEmpty(x.IntentKind))
            .WithMessage("An intended source must name a GraphIntentKinds option");

        RuleFor(x => x.Ordinal)
            .GreaterThanOrEqualTo(0)
            .WithMessage("An intended source's position cannot be negative");

        RuleFor(x => x.DataStoreName)
            .MaximumLength(200)
            .WithMessage("A data store name must not exceed 200 characters");

        RuleFor(x => x.SourceDataSetName)
            .MaximumLength(200)
            .WithMessage("A data set name must not exceed 200 characters");

        RuleFor(x => x.AuthoredAt)
            .NotNull()
            .When(x => x.AuthorUserId.HasValue)
            .WithMessage("An intended source that names its author says when it was stated");

        RuleFor(x => x.AuthorUserId)
            .NotNull()
            .When(x => x.AuthoredAt.HasValue)
            .WithMessage("An intended source that says when it was stated names its author");

        When(x => GraphIntentKinds.ByName(x.IntentKind).NamesDataStore, () =>
        {
            RuleFor(x => x.DataStoreName)
                .NotEmpty()
                .WithMessage("An intended source that names a data store must give its name");

            RuleFor(x => x.SourceDataSetId)
                .Null()
                .WithMessage("An intended source that names a data store cannot also name a data set");

            RuleFor(x => x.SourceDataSetName)
                .Null()
                .WithMessage("An intended source that names a data store cannot also name a data set");
        });

        When(x => GraphIntentKinds.ByName(x.IntentKind).NamesDataSet, () =>
        {
            RuleFor(x => x.SourceDataSetId)
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage("An intended source that names a data set must give its id");

            RuleFor(x => x.SourceDataSetName)
                .NotEmpty()
                .WithMessage("An intended source that names a data set must give its name");

            RuleFor(x => x.DataStoreName)
                .Null()
                .WithMessage("An intended source that names a data set cannot also name a data store");
        });
    }
}
