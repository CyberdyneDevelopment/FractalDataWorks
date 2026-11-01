namespace Fdw.Services.Multitenancy;

/// <summary>
/// Where retention is kept: the store and path the tenant tables are in, the table of retention policies and the table of legal holds (FDW-1575).
/// </summary>
/// <param name="DataStoreName">The data store the tables are in.</param>
/// <param name="PathName">The path (schema) of the tables.</param>
/// <param name="PolicyTable">The table of retention policies.</param>
/// <param name="HoldTable">The table of legal holds.</param>
public sealed record RetentionTables(string DataStoreName, string PathName, string PolicyTable, string HoldTable);
