using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Fdw.DevSession.Tests;

/// <summary>A throwaway git repository on disk, deleted when the test finishes.</summary>
/// <remarks>
/// The engine under test exists to drive real git, so these tests drive real git too. A mocked
/// runner would only assert that the engine passes the arguments the test already expects — it
/// could not catch a wrong flag, a git version difference, or a worktree that silently isn't
/// created, which is the entire risk surface here.
/// </remarks>
internal sealed class TemporaryRepository : IDisposable
{
    private TemporaryRepository(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public string Path => System.IO.Path.Combine(Root, "repo");

    public static TemporaryRepository CreateWithInitialCommit()
    {
        var root = Directory.CreateTempSubdirectory("fdw-devsession-tests-").FullName;
        Directory.CreateDirectory(System.IO.Path.Combine(root, "repo"));

        var repository = new TemporaryRepository(root);

        repository.Git("init", "--initial-branch=main");
        repository.Git("config", "user.email", "tests@fdw.local");
        repository.Git("config", "user.name", "FDW Tests");
        repository.WriteFile("README.md", "initial");
        repository.Git("add", "-A");
        repository.Git("commit", "-m", "initial commit");

        return repository;
    }

    public void WriteFile(string relativePath, string contents)
        => File.WriteAllText(System.IO.Path.Combine(Path, relativePath), contents);

    public string Git(params string[] arguments) => RunGit(Path, arguments);

    public static string GitIn(string workingDirectory, params string[] arguments) => RunGit(workingDirectory, arguments);

    private static string RunGit(string workingDirectory, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("git could not be started.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(" ", arguments)} failed ({process.ExitCode}): {error}");
        }

        return output.Trim();
    }

    public void Dispose()
    {
        try
        {
            DeleteRecursive(Root);
        }
        catch (IOException)
        {
        }
    }

    private static void DeleteRecursive(string path)
    {
        if (!Directory.Exists(path)) return;

        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
        Directory.Delete(path, recursive: true);
    }
}
