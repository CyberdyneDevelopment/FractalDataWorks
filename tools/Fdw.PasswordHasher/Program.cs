using System;
using System.Collections.Generic;
using Fdw.Security.Hashing;

namespace Fdw.PasswordHasher;

// Why: Tiny CLI shim around FDW's actual Pbkdf2PasswordHashAlgorithm so seed hashes generated
// by this tool are bytes-for-bytes identical to what the running app produces (and verifies).
// Use this whenever a login fails — verify offline to determine whether the issue is the
// stored hash vs. plaintext or somewhere else in the auth pipeline.
internal static class Program
{
    private const string Algorithm = "Pbkdf2";

    internal static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var verb = args[0].ToLowerInvariant();

        try
        {
            return verb switch
            {
                "hash" => HandleHash(args),
                "verify" => HandleVerify(args),
                "seed" => HandleSeed(args),
                "-h" or "--help" or "help" => HandleHelp(),
                _ => HandleUnknown(verb)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }
    }

    private static int HandleHash(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: fdw-hash hash <plaintext>");
            return 1;
        }

        var algo = ResolveAlgorithm();
        var result = algo.HashPassword(args[1]);

        Console.WriteLine($"Algorithm : {result.AlgorithmName}");
        Console.WriteLine($"Salt      : {result.Salt}");
        Console.WriteLine($"Hash      : {result.Hash}");
        return 0;
    }

    private static int HandleVerify(string[] args)
    {
        if (args.Length < 4)
        {
            Console.Error.WriteLine("usage: fdw-hash verify <plaintext> <storedHashBase64> <storedSaltBase64>");
            return 1;
        }

        var algo = ResolveAlgorithm();
        var ok = algo.VerifyPassword(args[1], args[2], args[3]);

        Console.WriteLine(ok ? "MATCH" : "MISMATCH");
        return ok ? 0 : 3;
    }

    // Why: Convenience verb for emitting seed-style output for one or more user/password pairs.
    // Useful when refreshing dev seeds — pipe to sql, or copy/paste into databases/AuthDb seed files.
    private static int HandleSeed(string[] args)
    {
        if (args.Length < 2 || (args.Length - 1) % 2 != 0)
        {
            Console.Error.WriteLine("usage: fdw-hash seed <user1> <password1> [<user2> <password2> ...]");
            return 1;
        }

        var algo = ResolveAlgorithm();
        var pairs = new List<(string User, string Password)>();
        for (var i = 1; i < args.Length; i += 2)
        {
            pairs.Add((args[i], args[i + 1]));
        }

        Console.WriteLine($"-- FDW seed hashes (Algorithm: {Algorithm})");
        Console.WriteLine();
        foreach (var (user, password) in pairs)
        {
            var result = algo.HashPassword(password);
            Console.WriteLine($"-- {user} / {password}");
            Console.WriteLine($"--   Salt: '{result.Salt}'");
            Console.WriteLine($"--   Hash: '{result.Hash}'");
            Console.WriteLine();
        }

        return 0;
    }

    private static int HandleHelp()
    {
        PrintUsage();
        return 0;
    }

    private static int HandleUnknown(string verb)
    {
        Console.Error.WriteLine($"unknown verb: {verb}");
        PrintUsage();
        return 1;
    }

    private static IPasswordHashAlgorithm ResolveAlgorithm()
    {
        // Why: Resolve through the actual TypeCollection so we exercise the same lookup
        // path the runtime uses — catches any regression in name registration.
        var algo = PasswordHashAlgorithms.ByName(Algorithm);
        if (ReferenceEquals(algo, PasswordHashAlgorithms.NotFound))
        {
            throw new InvalidOperationException(
                $"Algorithm '{Algorithm}' is not registered in PasswordHashAlgorithms TypeCollection.");
        }
        return algo;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("fdw-hash — FDW password hash CLI (uses Pbkdf2PasswordHashAlgorithm)");
        Console.WriteLine();
        Console.WriteLine("usage:");
        Console.WriteLine("  fdw-hash hash <plaintext>");
        Console.WriteLine("      Generate a fresh salt+hash for the given plaintext.");
        Console.WriteLine();
        Console.WriteLine("  fdw-hash verify <plaintext> <storedHashBase64> <storedSaltBase64>");
        Console.WriteLine("      Verify a plaintext against a stored hash+salt. Exits 0 on MATCH, 3 on MISMATCH.");
        Console.WriteLine();
        Console.WriteLine("  fdw-hash seed <user1> <password1> [<user2> <password2> ...]");
        Console.WriteLine("      Emit SQL-comment-style seed lines for one or more user/password pairs.");
        Console.WriteLine();
        Console.WriteLine("notes:");
        Console.WriteLine("  - Algorithm is fixed to Pbkdf2 (PBKDF2-HMAC-SHA512, 100k iter, 16-byte salt, 32-byte hash).");
        Console.WriteLine("  - Salt and hash are emitted/consumed as base64.");
        Console.WriteLine("  - This wraps the same FDW.Security.Hashing.Pbkdf2PasswordHashAlgorithm used at runtime,");
        Console.WriteLine("    so output is byte-identical to what the auth pipeline produces.");
    }
}
