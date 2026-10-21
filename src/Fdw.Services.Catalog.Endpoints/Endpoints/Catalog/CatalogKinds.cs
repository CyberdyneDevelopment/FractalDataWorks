using System;
using System.Collections.Generic;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>The kinds of named thing the catalog search finds (FDW-1681), in the order a browse lists them.</summary>
public static class CatalogKinds
{
    /// <summary>A data set.</summary>
    public const string DataSet = "DataSet";

    /// <summary>A field of a data set.</summary>
    public const string DataSetField = "DataSetField";

    /// <summary>A data store.</summary>
    public const string DataStore = "DataStore";

    /// <summary>A path of a data store.</summary>
    public const string DataPath = "DataPath";

    /// <summary>A container (a table, file or API resource) at a path of a data store.</summary>
    public const string Container = "Container";

    /// <summary>A field of a container.</summary>
    public const string ContainerField = "ContainerField";

    /// <summary>A calculation.</summary>
    public const string Calculation = "Calculation";

    /// <summary>A transformation.</summary>
    public const string Transformation = "Transformation";

    /// <summary>A pipeline.</summary>
    public const string Pipeline = "Pipeline";

    /// <summary>A glossary term.</summary>
    public const string GlossaryTerm = "GlossaryTerm";

    /// <summary>Gets every kind, in the order a browse lists them.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        DataSet, DataSetField, DataStore, DataPath, Container, ContainerField, Calculation, Transformation, Pipeline, GlossaryTerm,
    ];

    /// <summary>Gets the position of a kind in <see cref="All"/>, or -1 when it is not a kind the search finds.</summary>
    /// <param name="kind">The kind to place.</param>
    /// <returns>The position, compared without regard to case.</returns>
    public static int IndexOf(string kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        for (var i = 0; i < All.Count; i++)
        {
            if (string.Equals(All[i], kind, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
