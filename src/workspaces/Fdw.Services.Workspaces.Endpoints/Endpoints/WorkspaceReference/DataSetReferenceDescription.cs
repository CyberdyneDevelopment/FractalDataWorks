using System;
using System.Linq;
using System.Text;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The description of one data set's query operation in an API reference: what the data set is and
/// the fields a caller may filter and sort on.
/// </summary>
public static class DataSetReferenceDescription
{
    /// <summary>Describes the query operation of a data set a workspace exposes.</summary>
    /// <param name="readable">The data set under the name the workspace knows it by, and the data set its query runs.</param>
    /// <returns>The description, as Markdown.</returns>
    /// <remarks>
    /// A variation is described under its own name, with the fields of the main data set it varies, which are the fields
    /// the data route translates a filter against.
    /// </remarks>
    public static string For(ReadableDataSet readable)
    {
        ArgumentNullException.ThrowIfNull(readable);

        var dataSet = readable.ExecutionTarget;
        var text = new StringBuilder();
        text.Append("Query the **").Append(readable.Name).Append("** data set.");
        if (!string.Equals(readable.Name, dataSet.Name, StringComparison.OrdinalIgnoreCase))
            text.Append(" It is a variation of the **").Append(dataSet.Name).Append("** data set; its stored filter narrows the rows.");

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
