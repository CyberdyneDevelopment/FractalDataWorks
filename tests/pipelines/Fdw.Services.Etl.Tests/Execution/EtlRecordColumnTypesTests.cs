using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fdw.Data;
using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// FDW-7403: every record of the Etl assembly that is read from an OpsDb table through a generated mapper states each of its members as the type of the column of the same name in the
/// databases repository's DDL. A record is found by what it is, a class with <c>[GenerateMapper]</c> named <c>Etl{Table}Record</c>, and its table is <c>etl.{Table}</c>, so a record added later
/// is checked without anyone remembering it. The fault this catches: <c>EtlPipelineExecutionRecord</c> stated its two times as <c>DateTime</c> over <c>DATETIMEOFFSET</c> columns, and its mapper
/// failed on every row that had started, which no test noticed because none read a row through the mapper.
/// </summary>
/// <remarks>
/// The DDL is read from the checkout <c>FDW_TEST_DATABASES_PATH</c> names; with it not set the test reports a visible skip, never a pass. A member with no column of its name is left alone (the
/// mapper reads it as null); a record may state fewer members than its table has columns. A nullable column read into a non-nullable value member is a mismatch (a NULL cannot be read into it).
/// </remarks>
public sealed class EtlRecordColumnTypesTests
{
    private const string DatabasesPathVariable = "FDW_TEST_DATABASES_PATH";

    private static readonly Regex RecordName = new("^Etl(?<table>.+)Record$", RegexOptions.CultureInvariant);

    private static readonly Regex ColumnLine = new(
        @"^,?\s*\[?(?<name>[A-Za-z_][A-Za-z0-9_]*)\]?\s+(?<type>[A-Za-z0-9]+)(\s*\([^)]*\))?(?<rest>.*)$", RegexOptions.CultureInvariant);

    private static readonly HashSet<string> NotColumns = new(StringComparer.OrdinalIgnoreCase) { "CONSTRAINT", "PRIMARY", "UNIQUE", "INDEX", "CHECK", "FOREIGN" };

    private static readonly Dictionary<string, Type> ClrTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BIGINT"] = typeof(long),
        ["INT"] = typeof(int),
        ["SMALLINT"] = typeof(short),
        ["TINYINT"] = typeof(byte),
        ["BIT"] = typeof(bool),
        ["UNIQUEIDENTIFIER"] = typeof(Guid),
        ["DATETIMEOFFSET"] = typeof(DateTimeOffset),
        ["DATETIME2"] = typeof(DateTime),
        ["DATETIME"] = typeof(DateTime),
        ["VARCHAR"] = typeof(string),
        ["NVARCHAR"] = typeof(string),
        ["CHAR"] = typeof(string),
        ["NCHAR"] = typeof(string),
        ["DECIMAL"] = typeof(decimal),
        ["FLOAT"] = typeof(double),
        ["REAL"] = typeof(float),
        ["VARBINARY"] = typeof(byte[]),
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryEtlRecordStatesEachOfItsMembersAsTheTypeOfItsColumn()
    {
        var root = Environment.GetEnvironmentVariable(DatabasesPathVariable);
        if (string.IsNullOrWhiteSpace(root))
            Assert.Skip($"Set {DatabasesPathVariable} to a checkout of the databases repository to check the Etl records against the DDL of their tables.");

        var records = typeof(EtlPipelineExecutionRecord).Assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<GenerateMapperAttribute>() is not null && RecordName.IsMatch(type.Name))
            .ToList();
        records.Count.ShouldBeGreaterThan(5, "the records were not found by what they are");

        var problems = new List<string>();
        foreach (var record in records)
        {
            var table = RecordName.Match(record.Name).Groups["table"].Value;
            var file = Path.Combine(root, "OpsDb", "etl", "Tables", $"etl.{table}.sql");
            if (!File.Exists(file))
            {
                problems.Add($"{record.Name}: the table file {file} does not exist");
                continue;
            }

            var columns = Columns(File.ReadAllText(file));
            foreach (var member in record.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetCustomAttribute<NotStoredAttribute>() is null))
            {
                // Why a member with no column is not a mismatch: the generated mapper reads such a member as null and the read succeeds (the migration that retires a column leaves the member for one release,
                // FDW-6672), so only a member whose column exists is judged.
                var problem = columns.TryGetValue(member.Name, out var column) ? Mismatch(member, column) : null;
                if (problem is not null)
                    problems.Add($"{record.Name}.{member.Name} ({member.PropertyType.Name}) over etl.{table}: {problem}");
            }
        }

        problems.ShouldBeEmpty(string.Join(Environment.NewLine, problems));
    }

    private static string? Mismatch(PropertyInfo member, (string Type, bool Nullable) column)
    {
        if (!ClrTypes.TryGetValue(column.Type, out var expected))
            return $"the column type {column.Type} is not one this test knows";

        var stated = Nullable.GetUnderlyingType(member.PropertyType) ?? member.PropertyType;
        if (stated != expected)
            return $"the column is {column.Type} ({expected.Name}), the member is {stated.Name}";

        return column.Nullable && member.PropertyType.IsValueType && Nullable.GetUnderlyingType(member.PropertyType) is null
            ? $"the column {column.Type} is NULL-able and the member cannot hold a NULL"
            : null;
    }

    private static Dictionary<string, (string Type, bool Nullable)> Columns(string ddl)
    {
        var columns = new Dictionary<string, (string Type, bool Nullable)>(StringComparer.OrdinalIgnoreCase);
        var inTable = false;
        foreach (var raw in ddl.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                inTable = true;
                continue;
            }

            if (!inTable)
                continue;

            if (line.StartsWith(");", StringComparison.Ordinal))
                break;

            var match = ColumnLine.Match(line);
            if (match.Success && !NotColumns.Contains(match.Groups["name"].Value))
            {
                var rest = match.Groups["rest"].Value;
                var nullable = !rest.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase) && !rest.Contains("IDENTITY", StringComparison.OrdinalIgnoreCase);
                columns[match.Groups["name"].Value] = (match.Groups["type"].Value, nullable);
            }
        }

        return columns;
    }
}
