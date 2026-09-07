using System.CommandLine;
using System.Reflection;

namespace OsuFilesUtility;

internal static class CommandLine
{
    internal sealed class Args
    {
        public const string DefaultOutPath = "./YOU-CAN-RENAME-THIS-AND-MOVE-THIS-ON-SAME-DRIVE";

        public required string LazerPath { get; init; }
        public required string? OutPath { get; init; }
        public required bool IsCopy { get; init; }
        public required bool IsQuiet { get; init; }
        public required bool IsVerbose { get; init; }
        public required Operation Command { get; init; }
        public string? Target { get; init; }
        public string? MD5Hash { get; init; }
        public string? ReplayPath { get; init; }
        public long? OnlineID { get; init; }
        public bool Pretty { get; init; }

        public enum Operation
        {
            ExportJson,
            ExportBinary,
            LinkAll,
            LinkMD5,
            LinkReplay,
            LinkID,
            Diff,
            Validate,
        }
    }

    public static int Run(string[] args)
    {
        if (TryRunDroppedDirectory(args, out var droppedDirectoryExitCode))
        {
            return droppedDirectoryExitCode;
        }

        var root = BuildRootCommand();
        return root.Parse(args).Invoke();
    }

    private static RootCommand BuildRootCommand()
    {
        var root = new RootCommand("Create links from osu!lazer files and export beatmap data")
        {
            HelpName = "ofu",
        };

        var lazerPath = new Option<DirectoryInfo>("--dir", "-d")
        {
            DefaultValueFactory = _ => new DirectoryInfo(Api.GetDefaultLazerPath()),
            Description = "Path to the osu!lazer directory",
            Recursive = true,
        };
        var isVerbose = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed information",
            Recursive = true,
        };
        var isQuiet = new Option<bool>("--quiet", "-q")
        {
            Description = "Suppress output",
            Recursive = true,
        };

        root.Options.Add(lazerPath);
        root.Options.Add(isVerbose);
        root.Options.Add(isQuiet);

        var exportCommand = new Command("export", "Export osu!lazer beatmap data");
        var exportJsonCommand = new Command("json", "Export beatmap data as JSON");
        var prettyJson = new Option<bool>("--pretty")
        {
            Description = "Format the JSON with indentation",
        };
        var jsonOutput = CreateOutputArgument();
        exportJsonCommand.Options.Add(prettyJson);
        exportJsonCommand.Arguments.Add(jsonOutput);
        exportJsonCommand.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.ExportJson,
            parsed.GetValue(jsonOutput),
            pretty: parsed.GetValue(prettyJson))));

        var exportBinaryCommand = new Command("binary", "Export beatmap data in binary format");
        // Keep the common typo working while exposing the correctly-spelled command in help.
        exportBinaryCommand.Aliases.Add("binrary");
        var binaryOutput = CreateOutputArgument();
        exportBinaryCommand.Arguments.Add(binaryOutput);
        exportBinaryCommand.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.ExportBinary,
            parsed.GetValue(binaryOutput))));

        exportCommand.Subcommands.Add(exportJsonCommand);
        exportCommand.Subcommands.Add(exportBinaryCommand);
        root.Subcommands.Add(exportCommand);

        var linkCommand = new Command("link", "Create links or copies for beatmaps");
        var linkAllCommand = new Command("all", "Link all beatmaps");
        var linkAllOutput = CreateOutputArgument(useDefaultValue: true);
        var linkAllCopy = CreateCopyOption();
        linkAllCommand.Arguments.Add(linkAllOutput);
        linkAllCommand.Options.Add(linkAllCopy);
        linkAllCommand.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.LinkAll,
            parsed.GetValue(linkAllOutput),
            isCopy: parsed.GetValue(linkAllCopy))));

        var linkMd5Command = new Command("md5", "Link the beatmap with the specified MD5 hash");
        var md5 = new Argument<string>("md5")
        {
            Description = "Beatmap MD5 hash",
            Arity = ArgumentArity.ExactlyOne,
        };
        var linkMd5Output = CreateOutputArgument(useDefaultValue: true);
        var linkMd5Copy = CreateCopyOption();
        linkMd5Command.Arguments.Add(md5);
        linkMd5Command.Arguments.Add(linkMd5Output);
        linkMd5Command.Options.Add(linkMd5Copy);
        linkMd5Command.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.LinkMD5,
            parsed.GetValue(linkMd5Output),
            isCopy: parsed.GetValue(linkMd5Copy),
            md5Hash: parsed.GetValue(md5))));

        var linkReplayCommand = new Command("replay", "Link the beatmap used in a replay");
        var replay = new Argument<FileInfo>("replay-file")
        {
            Description = "Path to the replay file",
            HelpName = "replay file",
            Arity = ArgumentArity.ExactlyOne,
        };
        var linkReplayOutput = CreateOutputArgument(useDefaultValue: true);
        var linkReplayCopy = CreateCopyOption();
        linkReplayCommand.Arguments.Add(replay);
        linkReplayCommand.Arguments.Add(linkReplayOutput);
        linkReplayCommand.Options.Add(linkReplayCopy);
        linkReplayCommand.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.LinkReplay,
            parsed.GetValue(linkReplayOutput),
            isCopy: parsed.GetValue(linkReplayCopy),
            replayPath: parsed.GetValue(replay)?.FullName)));

        var linkIdCommand = new Command("id", "Link the beatmap or beatmap set with the specified online ID");
        var onlineId = new Argument<long>("id")
        {
            Description = "Beatmap or beatmap set online ID",
            Arity = ArgumentArity.ExactlyOne,
        };
        var linkIdOutput = CreateOutputArgument(useDefaultValue: true);
        var linkIdCopy = CreateCopyOption();
        linkIdCommand.Arguments.Add(onlineId);
        linkIdCommand.Arguments.Add(linkIdOutput);
        linkIdCommand.Options.Add(linkIdCopy);
        linkIdCommand.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.LinkID,
            parsed.GetValue(linkIdOutput),
            isCopy: parsed.GetValue(linkIdCopy),
            onlineId: parsed.GetValue(onlineId))));

        linkCommand.Subcommands.Add(linkAllCommand);
        linkCommand.Subcommands.Add(linkMd5Command);
        linkCommand.Subcommands.Add(linkReplayCommand);
        linkCommand.Subcommands.Add(linkIdCommand);
        root.Subcommands.Add(linkCommand);

        var diffCommand = new Command("diff", "Link beatmaps that are missing from another osu!lazer installation");
        var diffTarget = new Argument<string>("target")
        {
            Description = "Path to the other osu!lazer directory",
            Arity = ArgumentArity.ExactlyOne,
        };
        var diffOutput = CreateOutputArgument(useDefaultValue: true);
        var diffCopy = CreateCopyOption();
        diffCommand.Arguments.Add(diffTarget);
        diffCommand.Arguments.Add(diffOutput);
        diffCommand.Options.Add(diffCopy);
        diffCommand.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.Diff,
            parsed.GetValue(diffOutput),
            isCopy: parsed.GetValue(diffCopy),
            target: parsed.GetValue(diffTarget))));
        root.Subcommands.Add(diffCommand);

        var validateCommand = new Command("validate", "Remove invalid links from an output directory");
        var validateSource = new Argument<string?>("source")
        {
            Description = "Output directory to validate",
            HelpName = "source",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => Args.DefaultOutPath,
        };
        validateCommand.Arguments.Add(validateSource);
        validateCommand.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.Validate,
            parsed.GetValue(validateSource))));
        root.Subcommands.Add(validateCommand);

        var helpCommand = new Command("help", "Show help for the command-line interface");
        helpCommand.SetAction(_ =>
        {
            root.Parse(["--help"]).Invoke();
        });
        root.Subcommands.Add(helpCommand);

        var versionCommand = new Command("version", "Show version information");
        versionCommand.SetAction(_ => Console.WriteLine(GetVersion()));
        root.Subcommands.Add(versionCommand);

        root.SetAction(parsed => RunDefault(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet));

        return root;
    }

    private static Argument<string?> CreateOutputArgument(bool useDefaultValue = false)
    {
        var output = new Argument<string?>("out")
        {
            Description = "Path to the output directory or file",
            Arity = ArgumentArity.ZeroOrOne,
        };

        if (useDefaultValue)
        {
            output.DefaultValueFactory = _ => Args.DefaultOutPath;
        }

        return output;
    }

    private static Option<bool> CreateCopyOption()
    {
        return new Option<bool>("--copy", "-c")
        {
            Description = "Copy files instead of creating symbolic links",
        };
    }

    private static Args CreateArgs(
        ParseResult parsed,
        Option<DirectoryInfo> lazerPath,
        Option<bool> isVerbose,
        Option<bool> isQuiet,
        Args.Operation command,
        string? outPath = null,
        bool isCopy = false,
        string? target = null,
        string? md5Hash = null,
        string? replayPath = null,
        long? onlineId = null,
        bool pretty = false)
    {
        return new Args
        {
            LazerPath = parsed.GetValue(lazerPath)!.FullName,
            OutPath = outPath,
            IsCopy = isCopy,
            IsQuiet = parsed.GetValue(isQuiet),
            IsVerbose = parsed.GetValue(isVerbose),
            Command = command,
            Target = target,
            MD5Hash = md5Hash,
            ReplayPath = replayPath,
            OnlineID = onlineId,
            Pretty = pretty,
        };
    }

    private static void RunDefault(
        ParseResult parsed,
        Option<DirectoryInfo> lazerPath,
        Option<bool> isVerbose,
        Option<bool> isQuiet)
    {
        var outputPath = Path.GetFullPath(Args.DefaultOutPath);
        var args = new Args
        {
            LazerPath = parsed.GetValue(lazerPath)!.FullName,
            OutPath = outputPath,
            IsCopy = false,
            IsQuiet = parsed.GetValue(isQuiet),
            IsVerbose = parsed.GetValue(isVerbose),
            Command = Args.Operation.LinkAll,
        };

        RunInteractive(args, validate: false);
    }

    private static bool TryRunDroppedDirectory(string[] args, out int exitCode)
    {
        exitCode = 0;

        if (args.Length != 1 || args[0].StartsWith('-') ||
            (!Directory.Exists(args[0]) && !System.IO.File.Exists(args[0])))
        {
            return false;
        }

        var outputPath = Path.GetFullPath(args[0]);
        var lazerPath = Path.GetFullPath(Api.GetDefaultLazerPath());
        var programArgs = new Args
        {
            LazerPath = lazerPath,
            OutPath = outputPath,
            IsCopy = false,
            IsQuiet = false,
            IsVerbose = true,
            Command = Args.Operation.LinkAll,
        };

        if (System.IO.File.Exists(outputPath))
        {
            Console.WriteLine($"{outputPath} is not a directory");
            ReadKey();
            return true;
        }

        RunInteractive(programArgs, validate: true);
        return true;
    }

    private static void RunInteractive(Args args, bool validate)
    {
        if (!Directory.Exists(args.LazerPath))
        {
            Console.WriteLine($"{args.LazerPath} not found");
            Console.WriteLine("Maybe you have a custom lazer install? use -d to specify where to find it");
            return;
        }

        if (CheckDrives(args.LazerPath, args.OutPath!))
        {
            return;
        }

        if (Directory.Exists(args.OutPath) &&
            Directory.EnumerateFileSystemEntries(args.OutPath).Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("Output directory contains files, confirm its correct");
            Console.ResetColor();
            Console.WriteLine();
        }

        if (validate)
        {
            Console.WriteLine("This will validate existing symlinks and symlink all beatmaps!");
        }
        else
        {
            Console.WriteLine("This will symlink all beatmaps!");
        }

        Console.WriteLine(" ");
        Console.WriteLine($"Lazer path: {args.LazerPath}");
        Console.WriteLine($"Output Path: {args.OutPath}");
        Console.WriteLine(" ");
        Console.WriteLine("Press <ESC> or <Ctrl+C> to exit...");
        Console.WriteLine("Press any key to continue...");

        if (ReadKey())
        {
            return;
        }

        Run(args);
    }

    private static void Run(Args args)
    {
        var previousOut = Console.Out;

        if (args.IsQuiet)
        {
            Console.SetOut(TextWriter.Null);
        }

        try
        {
            if (args.Command == Args.Operation.Validate)
            {
                var source = args.OutPath!;
                EnsureOutputDirectory(source);
                Console.WriteLine("Validating paths, this can take a while...");
                Api.ValidatePaths(source);
                Console.WriteLine("Validated paths");
                return;
            }

            var api = new Api(args.LazerPath, args.IsVerbose);

            switch (args.Command)
            {
                case Args.Operation.ExportJson:
                    RunExport(api, ExportFormat.Json, args.OutPath, args.Pretty);
                    return;
                case Args.Operation.ExportBinary:
                    RunExport(api, ExportFormat.Binary, args.OutPath);
                    return;
            }

            var outputPath = args.OutPath!;
            EnsureOutputDirectory(outputPath);

            switch (args.Command)
            {
                case Args.Operation.LinkAll:
                    Console.WriteLine("Creating links...");
                    api.CreateLinksAll(outputPath, args.IsCopy);
                    Console.WriteLine("Done.");
                    return;
                case Args.Operation.LinkMD5:
                    var beatmap = api.Realm.All<Beatmap>().First(b => b.MD5Hash == args.MD5Hash);
                    api.CreateLinks(beatmap, outputPath, args.IsCopy);
                    return;
                case Args.Operation.LinkReplay:
                    var md5Hash = Api.GetMD5HashFromReplay(args.ReplayPath);
                    var replayBeatmap = api.Realm.All<Beatmap>().First(b => b.MD5Hash == md5Hash);
                    api.CreateLinks(replayBeatmap, outputPath, args.IsCopy);
                    return;
                case Args.Operation.LinkID:
                    var onlineId = args.OnlineID;
                    Func<IList<Beatmap>, bool> hasAny = maps => maps.Any(map => map.OnlineID == onlineId);
                    var set = api.Realm.All<BeatmapSet>().First(b =>
                        b.OnlineID == onlineId || hasAny(b.Beatmaps));
                    api.CreateLinks(set, outputPath, args.IsCopy);
                    return;
                case Args.Operation.Diff:
                    RunDiff(api, outputPath, args.Target!, args.IsCopy);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        finally
        {
            if (args.IsQuiet)
            {
                Console.SetOut(previousOut);
            }
        }
    }

    private static void EnsureOutputDirectory(string outPath)
    {
        if (System.IO.File.Exists(outPath))
        {
            throw new FileLoadException("not a directory", outPath);
        }

        if (!Directory.Exists(outPath))
        {
            Directory.CreateDirectory(outPath);
        }
    }

    private static string GetVersion()
    {
        var assembly = typeof(CommandLine).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    public static bool CheckDrives(string lazerPath, string outPath)
    {
        if (Path.GetPathRoot(lazerPath) != Path.GetPathRoot(outPath))
        {
            Console.WriteLine("These are on different drives");
            Console.WriteLine(" ");
            Console.WriteLine($"Lazer path: {lazerPath}");
            Console.WriteLine($"Output Path: {outPath}");
            Console.WriteLine(" ");
            return true;
        }

        return false;
    }

    public static bool ReadKey()
    {
        var key = Console.ReadKey();
        return key.Key == ConsoleKey.Escape;
    }

    private enum ExportFormat
    {
        Json,
        Binary,
    }

    private static void RunExport(Api api, ExportFormat format, string? outPath, bool pretty = false)
    {
        if (Directory.Exists(outPath))
        {
            Console.WriteLine($"{outPath} is a directory not a file");
            return;
        }

        switch (format)
        {
            case ExportFormat.Json:
                ExportJson(api, outPath, pretty);
                break;
            case ExportFormat.Binary:
                ExportBinary(api, outPath);
                break;
        }
    }

    private static void ExportJson(Api api, string? outPath, bool pretty)
    {
        var json = api.ExportToJson(pretty);

        if (outPath is not null)
        {
            Console.WriteLine($"Saving to {outPath}...");

            using var file = System.IO.File.CreateText(outPath);
            file.Write(json);
            Console.WriteLine("Done.");
        }
        else
        {
            Console.WriteLine(json);
        }
    }

    private static void ExportBinary(Api api, string? outPath)
    {
        using var stream = outPath is not null
            ? new FileStream(outPath, FileMode.OpenOrCreate)
            : Console.OpenStandardOutput();
        using var writer = new BinaryWriter(stream);

        api.ExportToBinary(writer);
    }

    private static void RunDiff(Api api, string outPath, string diffLazerPath, bool isCopy)
    {
        var diff = new Api(diffLazerPath, api.Verbose);
        var lookup = diff.Realm.All<BeatmapSet>().ToLookup(set => set.OnlineID);
        var main = api.Realm.All<BeatmapSet>();

        Console.WriteLine("Creating links (difference)...");
        foreach (var set in main)
        {
            if (lookup.Contains(set.OnlineID))
            {
                continue;
            }

            api.CreateLinks(set, outPath, isCopy);
        }

        Console.WriteLine("Done.");
    }
}
