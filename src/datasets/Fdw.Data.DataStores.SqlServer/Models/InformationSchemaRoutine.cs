using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// A row of INFORMATION_SCHEMA.ROUTINES (FDW-5876: read through the connection, not SqlClient).
/// </summary>
[GenerateMapper]
public sealed class InformationSchemaRoutine
{
    /// <summary>Gets or sets the routine's schema.</summary>
    [Column("ROUTINE_SCHEMA")]
    public string RoutineSchema { get; set; } = string.Empty;

    /// <summary>Gets or sets the routine's name.</summary>
    [Column("ROUTINE_NAME")]
    public string RoutineName { get; set; } = string.Empty;

    /// <summary>Gets or sets the routine type (PROCEDURE or FUNCTION).</summary>
    [Column("ROUTINE_TYPE")]
    public string RoutineType { get; set; } = string.Empty;
}
