#pragma warning disable CS1591
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Fdw.Types;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Schema.Ddl.Tasks;

/// <summary>
/// MSBuild task that scans assemblies for TypeCollections and generates DDL files.
/// </summary>
public sealed class GenerateDdlFilesTask : Task
{
    /// <summary>
    /// Gets or sets the path to the assembly to scan.
    /// </summary>
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output directory for generated DDL files.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target database system (MsSql, PostgreSql, MySql).
    /// </summary>
    public string TargetDatabase { get; set; } = "MsSql";

    /// <summary>
    /// Executes the DDL generation task.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    // MA0051: Method length acceptable - MSBuild task orchestration (load assembly, discover types, generate files)
#pragma warning disable MA0051 // Method is too long
    public override bool Execute()
#pragma warning restore MA0051
    {
        try
        {
            Log.LogMessage(MessageImportance.High, "Scanning assembly: {0}", AssemblyPath);

            if (!File.Exists(AssemblyPath))
            {
                Log.LogError("Assembly not found: {0}", AssemblyPath);
                return false;
            }

            // Load the assembly
            var assembly = Assembly.LoadFrom(AssemblyPath);

            // Find all TypeCollections by looking for types with GetMetadata() method
            var typeCollections = DiscoverTypeCollections(assembly);

            Log.LogMessage(MessageImportance.High, "Found {0} TypeCollections", typeCollections.Count);

            if (typeCollections.Count == 0)
            {
                Log.LogMessage(MessageImportance.Normal, "No TypeCollections found in assembly");
                return true;
            }

            // Generate DDL files
            Directory.CreateDirectory(OutputPath);

            var generatedFiles = new List<string>();

            foreach (var typeCollection in typeCollections)
            {
                var metadata = GetMetadata(typeCollection);
                if (metadata == null)
                    continue;

                var ddlCommands = GetDdlCommands(typeCollection);
                if (ddlCommands == null || ddlCommands.Count == 0)
                {
                    Log.LogMessage(MessageImportance.Low, "No DDL commands for {0}", metadata.Name);
                    continue;
                }

                // Generate SQL file for this TypeCollection
                var fileName = $"{metadata.Name}.sql";
                var filePath = Path.Combine(OutputPath, "lookups", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                var sql = GenerateSql(ddlCommands, metadata);
                File.WriteAllText(filePath, sql);

                generatedFiles.Add(filePath);
                Log.LogMessage(MessageImportance.Normal, "Generated: {0}", filePath);
            }

            Log.LogMessage(MessageImportance.High, "Generated {0} DDL files", generatedFiles.Count);

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

    private static List<Type> DiscoverTypeCollections(Assembly assembly)
    {
        var collections = new List<Type>();

        foreach (var type in assembly.GetTypes())
        {
            // Look for types with public static GetMetadata() method
            var method = type.GetMethod(
                "GetMetadata",
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            if (method != null && method.ReturnType == typeof(TypeCollectionMetadata))
            {
                collections.Add(type);
            }
        }

        return collections;
    }

    private TypeCollectionMetadata? GetMetadata(Type typeCollection)
    {
        try
        {
            var method = typeCollection.GetMethod(
                "GetMetadata",
                BindingFlags.Public | BindingFlags.Static);

            if (method == null)
                return null;

            return method.Invoke(null, null) as TypeCollectionMetadata;
        }
        catch (Exception ex)
        {
            Log.LogWarning("Failed to get metadata for {0}: {1}", typeCollection.Name, ex.Message);
            return null;
        }
    }

    private IReadOnlyList<IDdlCommand>? GetDdlCommands(Type typeCollection)
    {
        try
        {
            // Look for DdlCommands property
            var property = typeCollection.GetProperty(
                "DdlCommands",
                BindingFlags.Public | BindingFlags.Static);

            if (property == null)
                return null;

            return property.GetValue(null) as IReadOnlyList<IDdlCommand>;
        }
        catch (Exception ex)
        {
            Log.LogWarning("Failed to get DDL commands for {0}: {1}", typeCollection.Name, ex.Message);
            return null;
        }
    }

    private string GenerateSql(IReadOnlyList<IDdlCommand> commands, TypeCollectionMetadata metadata)
    {
        var lines = new List<string>
        {
            "-- Generated DDL for TypeCollection: " + metadata.Name,
            "-- Full name: " + metadata.FullName,
            "-- Generated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " UTC",
            "",
            "-- Insert TypeCollection metadata",
            ""
        };

        foreach (var command in commands)
        {
            if (command is InsertDataCommand insertCmd)
            {
                var sql = GenerateInsertSql(insertCmd);
                lines.Add(sql);
                lines.Add("GO");
                lines.Add("");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string GenerateInsertSql(InsertDataCommand command)
    {
        var schema = string.IsNullOrEmpty(command.SchemaName) ? "dbo" : command.SchemaName;
        var lines = new List<string>();

        if (command.IdentityInsert)
        {
            lines.Add($"SET IDENTITY_INSERT [{schema}].[{command.TableName}] ON;");
        }

        // Generate INSERT statement
        var columnList = string.Join(", ", command.Columns.Select(c => $"[{c}]"));
        lines.Add($"INSERT INTO [{schema}].[{command.TableName}] ({columnList})");
        lines.Add("VALUES");

        var valueRows = new List<string>();
        foreach (var row in command.Values)
        {
            var values = row.Select(FormatValue);
            valueRows.Add($"    ({string.Join(", ", values)})");
        }

        lines.Add(string.Join("," + Environment.NewLine, valueRows));
        lines.Add(";");

        if (command.IdentityInsert)
        {
            lines.Add($"SET IDENTITY_INSERT [{schema}].[{command.TableName}] OFF;");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string FormatValue(object? value)
    {
        if (value == null)
            return "NULL";

        if (value is string str)
            return $"N'{str.Replace("'", "''")}'";

        if (value is bool b)
            return b ? "1" : "0";

        if (value is DateTime dt)
            return $"'{dt:yyyy-MM-dd HH:mm:ss}'";

        if (value is Guid guid)
            return $"'{guid}'";

        return value.ToString() ?? "NULL";
    }
}
