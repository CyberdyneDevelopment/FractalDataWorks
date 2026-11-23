using System;

namespace Fdw.Services.Abstractions;

/// <summary>
/// What a record that points at another domain's member says about that member: where to ask whether it exists, or why nothing asks.
/// </summary>
/// <remarks>
/// A threshold link, a tag link and a variation hold the durable id of a member of another domain. A foreign key cannot check that id when the
/// domain row's id is unique among current rows only (a filtered index cannot be referenced), so the owning domain asks
/// <see cref="ICitedConfigurations"/>. Each option of <c>ThresholdSubjectKinds</c> and <c>TaggableKinds</c> states its citation as a required member, one of three statements, so an option that
/// forgets to state one does not compile and none is checked by accident.
/// </remarks>
public sealed class Citation
{
    private Citation(string? container, string? reason)
    {
        Container = container;
        Reason = reason;
    }

    /// <summary>Gets the container of the cited domain's provider (<c>path.table</c>, as <see cref="IDomainConfigurationProvider.Container"/> names it); null when nothing is asked.</summary>
    public string? Container { get; }

    /// <summary>Gets why no check is made when <see cref="Container"/> is null: the key the database enforces, or why the subject is not a configuration row.</summary>
    public string? Reason { get; }

    /// <summary>States that the cited member is looked up in the domain whose provider names <paramref name="container"/>.</summary>
    /// <param name="container">The container, <c>path.table</c>: for the data set domain, <c>data.DataSet</c>.</param>
    /// <returns>The citation.</returns>
    /// <exception cref="ArgumentException">The container is empty.</exception>
    public static Citation Of(string container)
    {
        if (string.IsNullOrWhiteSpace(container))
            throw new ArgumentException("A citation names the container it cites.", nameof(container));

        return new Citation(container, null);
    }

    /// <summary>States that the database enforces the citation with a foreign key, so nothing is asked.</summary>
    /// <param name="constraint">The foreign key that enforces it.</param>
    /// <returns>The citation.</returns>
    /// <exception cref="ArgumentException">The constraint is empty.</exception>
    public static Citation EnforcedByDatabase(string constraint)
    {
        if (string.IsNullOrWhiteSpace(constraint))
            throw new ArgumentException("A citation the database enforces names the key that does.", nameof(constraint));

        return new Citation(null, constraint);
    }

    /// <summary>States that the subject is not the member of a domain a check can ask, or that no check exists yet, and says why.</summary>
    /// <param name="reason">Why nothing is asked.</param>
    /// <returns>The citation.</returns>
    /// <exception cref="ArgumentException">The reason is empty.</exception>
    public static Citation Uncited(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A citation that is not checked says why.", nameof(reason));

        return new Citation(null, reason);
    }
}
