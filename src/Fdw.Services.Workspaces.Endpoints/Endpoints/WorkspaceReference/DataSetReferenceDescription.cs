using System;
using System.Linq;
using System.Text;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The description of one data set's query operation in an API reference: what the data set is and
/// the fields a caller may filter and sort on.
/// </summary>
public static class DataSetReferenceDescription
{
    /// <summary>Describes a data set's query operation.</summary>
    /// <param name="dataSet">The data set.</param>
    /// <returns>The description, as Markdown.</returns>
    public static string For(IDataSetImplementationConfiguration dataSet)
    {
        ArgumentNullException.ThrowIfNull(dataSet);

        var text = new StringBuilder();
        text.Append("Query the **").Append(dataSet.Name).Append("** data set.");
        if (!string.IsNullOrWhiteSpace(dataSet.Description))
            text.Append(' ').Append(dataSet.Description);

        // The same fields the data route translates a filter against: current and not deleted.
        var fields = dataSet.Fields.Where(f => f.IsCurrent && !f.IsDeleted).OrderBy(f => f.Ordinal).ToList();
        if (fields.Count == 0)
            return text.ToString();

        text.Append("\n\n| Field | Type | Key | Role |\n|---|---|---|---|\n");
        foreach (var field in fields)
        {
            text.Append("| `").Append(field.Name).Append("` | ").Append(field.TypeName)
                .Append(" | ").Append(field.IsKey ? "Yes" : string.Empty)
                .Append(" | ").Append(field.Role).Append(" |\n");
        }

        text.Append("\nFilter and sort on these field names in the request body.");
        return text.ToString();
    }
}
