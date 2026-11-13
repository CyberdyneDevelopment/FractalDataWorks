using FluentValidation;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Validation;

namespace Fdw.Data.DataSets;

/// <summary>Validator for <see cref="GraphDataSetFieldNoteConfiguration"/> (FDW-5294).</summary>
/// <remarks>
/// Whether a note can be answered comes from its <see cref="GraphFieldNoteKinds"/> option, never from a switch on the kind's name. The rules
/// mirror the row CHECK on <c>data.GraphDataSetFieldNote</c> so a wrong note is refused with a message before the database is reached.
/// </remarks>
public sealed class GraphDataSetFieldNoteConfigurationValidator : FdwConfigurationValidator<GraphDataSetFieldNoteConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldNoteConfigurationValidator"/> class.</summary>
    public GraphDataSetFieldNoteConfigurationValidator()
    {
        RuleFor(x => x.NoteKind)
            .NotEmpty()
            .WithMessage("A note must say what it is");

        RuleFor(x => x.NoteKind)
            .Must(kind => !ReferenceEquals(GraphFieldNoteKinds.ByName(kind), GraphFieldNoteKinds.NotFound))
            .When(x => !string.IsNullOrEmpty(x.NoteKind))
            .WithMessage("A note must name a GraphFieldNoteKinds option");

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("A note must say something");

        RuleFor(x => x.Ordinal)
            .GreaterThanOrEqualTo(0)
            .WithMessage("A note's position cannot be negative");

        RuleFor(x => x.AuthoredAt)
            .NotNull()
            .When(x => x.AuthorUserId.HasValue)
            .WithMessage("A note that names its author says when it was written");

        RuleFor(x => x.AuthorUserId)
            .NotNull()
            .When(x => x.AuthoredAt.HasValue)
            .WithMessage("A note that says when it was written names its author");

        RuleFor(x => x.AnsweredAt)
            .NotNull()
            .When(x => x.AnsweredByUserId.HasValue)
            .WithMessage("A note that names who answered it says when");

        RuleFor(x => x.AnsweredByUserId)
            .NotNull()
            .When(x => x.AnsweredAt.HasValue)
            .WithMessage("A note that says when it was answered names who answered it");

        RuleFor(x => x.IsAnswered)
            .Equal(true)
            .When(x => x.AnsweredByUserId.HasValue || x.AnsweredAt.HasValue)
            .WithMessage("Only an answered note says who answered it and when");

        RuleFor(x => x.IsAnswered)
            .Equal(false)
            .When(x => !GraphFieldNoteKinds.ByName(x.NoteKind).CanBeAnswered)
            .WithMessage("Only a note of a kind that can be answered is marked answered");
    }
}
