namespace Fdw.Services.Multitenancy;

/// <summary>
/// Where the feature switches are kept: the store and path the tenant tables are in, the table of the features the platform switched on
/// for each tenant, and the table of the features each tenant switched on for its organizations (FDW-1580).
/// </summary>
/// <param name="DataStoreName">The data store the tables are in.</param>
/// <param name="PathName">The path (schema) of the tables.</param>
/// <param name="TenantTable">The table of the features switched on for each tenant.</param>
/// <param name="OrganizationTable">The table of the features switched on for each organization.</param>
public sealed record FeatureTables(string DataStoreName, string PathName, string TenantTable, string OrganizationTable);
