using System.CommandLine;
using System.Reflection;

namespace OsuFilesUtility;

internal static class CommandLine
{
    internal sealed class Args
    {
        public const string DefaultOutPath = "./songs";

        public required string LazerPath { get; init; }
        public required string? OutPath { get; init; }
        public required bool IsCopy { get; init; }
        public required bool IsQuiet { get; init; }
        public required bool IsVerbose { get; init; }
        public required Operation Command { get; init; }
        public string? Target { get; init; }
        public string? Md5Hash { get; init; }
        public string? ReplayPath { get; init; }
        public long? OnlineId { get; init; }
        public JsonExporter.ExportSettings JsonExportSettings { get; init; } = new();

        public enum Operation
        {
            ExportJson,
            ExportJsonStream,
            ExportBinary,
            LinkAll,
            LinkMd5,
            LinkReplay,
            LinkId,
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

        var exportCommand = new Command("export", "Export osu!lazer data");
        var exportJsonCommand = new Command("json", "Export data as JSON");
        var prettyJson = new Option<bool>("--pretty")
        {
            Description = "Format the JSON with indentation",
        };
        var allowNullsJson = new Option<bool>("--allow-nulls")
        {
            Description = "Include null values in the JSON output",
        };
        var allExceptJson = new Option<bool>("--all-except")
        {
            Description = "Export all except selected flags",
        };
        var usersJson = new Option<bool>("--users")
        {
            Description = "Export users",
        };
        var rulesetsJson = new Option<bool>("--rulesets")
        {
            Description = "Export rulesets",
        };
        var beatmapsJson = new Option<bool>("--beatmaps", "--maps")
        {
            Description = "Export beatmaps",
        };
        var beatmapSetsJson = new Option<bool>("--beatmapsets", "--sets")
        {
            Description = "Export beatmap sets",
        };
        var collectionsJson = new Option<bool>("--collections")
        {
            Description = "Export collections",
        };
        var scoresJson = new Option<bool>("--scores")
        {
            Description = "Export scores",
        };
        var skinsJson = new Option<bool>("--skins")
        {
            Description = "Export skins",
        };
        var jsonOutput = CreateOutputArgument();
        exportJsonCommand.Options.Add(prettyJson);
        exportJsonCommand.Options.Add(allowNullsJson);
        exportJsonCommand.Options.Add(allExceptJson);
        exportJsonCommand.Options.Add(usersJson);
        exportJsonCommand.Options.Add(rulesetsJson);
        exportJsonCommand.Options.Add(beatmapsJson);
        exportJsonCommand.Options.Add(beatmapSetsJson);
        exportJsonCommand.Options.Add(collectionsJson);
        exportJsonCommand.Options.Add(scoresJson);
        exportJsonCommand.Options.Add(skinsJson);
        exportJsonCommand.Arguments.Add(jsonOutput);
        var jsonFlagOptions = new[]
        {
            (Option: usersJson, Flag: JsonExporter.ExportFlags.Users),
            (Option: rulesetsJson, Flag: JsonExporter.ExportFlags.Rulesets),
            (Option: beatmapsJson, Flag: JsonExporter.ExportFlags.Beatmaps),
            (Option: beatmapSetsJson, Flag: JsonExporter.ExportFlags.BeatmapSets),
            (Option: collectionsJson, Flag: JsonExporter.ExportFlags.Collections),
            (Option: scoresJson, Flag: JsonExporter.ExportFlags.Scores),
            (Option: skinsJson, Flag: JsonExporter.ExportFlags.Skins),
        };
        exportJsonCommand.Validators.Add(commandResult =>
        {
            if (commandResult.GetResult(allExceptJson) is not null &&
                jsonFlagOptions.All(option => commandResult.GetResult(option.Option) is null))
            {
                commandResult.AddError("--all-except requires at least one export flag to specify the exceptions");
            }
        });
        exportJsonCommand.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.ExportJson,
            parsed.GetValue(jsonOutput),
            jsonSettings: CreateJsonExportSettings(
                parsed,
                allExceptJson,
                jsonFlagOptions,
                pretty: prettyJson,
                allowNulls: allowNullsJson))));

        var exportNdjsonCommand = new Command("ndjson", "Export beatmap data as newline-delimited JSON");
        var allowNullsNdjson = new Option<bool>("--allow-nulls")
        {
            Description = "Include null values in the JSON output",
        };
        var allExceptNdjson = new Option<bool>("--all-except")
        {
            Description = "Export all except selected flags",
        };
        var usersNdjson = new Option<bool>("--users")
        {
            Description = "Export users",
        };
        var rulesetsNdjson = new Option<bool>("--rulesets")
        {
            Description = "Export rulesets",
        };
        var beatmapsNdjson = new Option<bool>("--beatmaps", "--maps")
        {
            Description = "Export beatmaps",
        };
        var beatmapSetsNdjson = new Option<bool>("--beatmapsets", "--sets")
        {
            Description = "Export beatmap sets",
        };
        var collectionsNdjson = new Option<bool>("--collections")
        {
            Description = "Export collections",
        };
        var scoresNdjson = new Option<bool>("--scores")
        {
            Description = "Export scores",
        };
        var skinsNdjson = new Option<bool>("--skins")
        {
            Description = "Export skins",
        };
        exportNdjsonCommand.Options.Add(allExceptNdjson);
        exportNdjsonCommand.Options.Add(allowNullsNdjson);
        exportNdjsonCommand.Options.Add(usersNdjson);
        exportNdjsonCommand.Options.Add(rulesetsNdjson);
        exportNdjsonCommand.Options.Add(beatmapsNdjson);
        exportNdjsonCommand.Options.Add(beatmapSetsNdjson);
        exportNdjsonCommand.Options.Add(collectionsNdjson);
        exportNdjsonCommand.Options.Add(scoresNdjson);
        exportNdjsonCommand.Options.Add(skinsNdjson);
        var ndjsonFlagOptions = new[]
        {
            (Option: usersNdjson, Flag: JsonExporter.ExportFlags.Users),
            (Option: rulesetsNdjson, Flag: JsonExporter.ExportFlags.Rulesets),
            (Option: beatmapsNdjson, Flag: JsonExporter.ExportFlags.Beatmaps),
            (Option: beatmapSetsNdjson, Flag: JsonExporter.ExportFlags.BeatmapSets),
            (Option: collectionsNdjson, Flag: JsonExporter.ExportFlags.Collections),
            (Option: scoresNdjson, Flag: JsonExporter.ExportFlags.Scores),
            (Option: skinsNdjson, Flag: JsonExporter.ExportFlags.Skins),
        };
        exportNdjsonCommand.Validators.Add(commandResult =>
        {
            if (commandResult.GetResult(allExceptNdjson) is not null &&
                ndjsonFlagOptions.All(option => commandResult.GetResult(option.Option) is null))
            {
                commandResult.AddError("--all-except requires at least one export flag to specify the exceptions");
            }
        });
        exportNdjsonCommand.SetAction(parsed => Run(CreateArgs(
            parsed,
            lazerPath,
            isVerbose,
            isQuiet,
            Args.Operation.ExportJsonStream,
            jsonSettings: CreateJsonExportSettings(
                parsed,
                allExceptNdjson,
                ndjsonFlagOptions,
                allowNulls: allowNullsNdjson))));

        var exportBinaryCommand = new Command("binary", "Export beatmap data in binary format");
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
        exportCommand.Subcommands.Add(exportNdjsonCommand);
        exportCommand.Subcommands.Add(exportBinaryCommand);
        exportCommand.Description += exportCommand.SubcommandHelpValues();
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
            Args.Operation.LinkMd5,
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
            Args.Operation.LinkId,
            parsed.GetValue(linkIdOutput),
            isCopy: parsed.GetValue(linkIdCopy),
            onlineId: parsed.GetValue(onlineId))));

        linkCommand.Subcommands.Add(linkAllCommand);
        linkCommand.Subcommands.Add(linkMd5Command);
        linkCommand.Subcommands.Add(linkReplayCommand);
        linkCommand.Subcommands.Add(linkIdCommand);
        linkCommand.Description += linkCommand.SubcommandHelpValues();
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
        helpCommand.SetAction(_ => { root.Parse(["--help"]).Invoke(); });
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
        JsonExporter.ExportSettings? jsonSettings = null)
    {
        return new Args
        {
            LazerPath = Api.ResolveLazerPath(parsed.GetValue(lazerPath)!.FullName),
            OutPath = outPath,
            IsCopy = isCopy,
            IsQuiet = parsed.GetValue(isQuiet),
            IsVerbose = parsed.GetValue(isVerbose),
            Command = command,
            Target = target,
            Md5Hash = md5Hash,
            ReplayPath = replayPath,
            OnlineId = onlineId,
            JsonExportSettings = jsonSettings ?? new JsonExporter.ExportSettings(),
        };
    }

    private static JsonExporter.ExportSettings CreateJsonExportSettings(
        ParseResult parsed,
        Option<bool> allExcept,
        IReadOnlyList<(Option<bool> Option, JsonExporter.ExportFlags Flag)> flagOptions,
        Option<bool>? pretty = null,
        Option<bool>? allowNulls = null)
    {
        var selectedFlags = flagOptions
            .Where(option => parsed.GetValue(option.Option))
            .Aggregate(JsonExporter.ExportFlags.None, (flags, option) => flags | option.Flag);

        var hasSelectedFlags = selectedFlags != JsonExporter.ExportFlags.None;
        var flags = hasSelectedFlags ? selectedFlags : JsonExporter.ExportFlags.All;

        if (parsed.GetValue(allExcept))
        {
            flags = JsonExporter.ExportFlags.All & ~selectedFlags;
        }

        return new JsonExporter.ExportSettings(
            IsPretty: pretty is not null && parsed.GetValue(pretty),
            AllowNulls: allowNulls is not null && parsed.GetValue(allowNulls),
            Flags: flags);
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
            LazerPath = Api.ResolveLazerPath(parsed.GetValue(lazerPath)!.FullName),
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

        Console.WriteLine(validate
            ? "This will validate existing symlinks and symlink all beatmaps!"
            : "This will symlink all beatmaps!");

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
                    RunExport(api, ExportFormat.Json, args.OutPath, args.JsonExportSettings);
                    return;
                case Args.Operation.ExportJsonStream:
                    api.ExportToJsonStream(args.JsonExportSettings);
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
                case Args.Operation.LinkMd5:
                    var beatmap = api.Realm.All<Beatmap>().First(b => b.MD5Hash == args.Md5Hash);
                    api.CreateLinks(beatmap, outputPath, args.IsCopy);
                    return;
                case Args.Operation.LinkReplay:
                    var md5Hash = Api.GetMd5HashFromReplay(args.ReplayPath);
                    var replayBeatmap = api.Realm.All<Beatmap>().First(b => b.MD5Hash == md5Hash);
                    api.CreateLinks(replayBeatmap, outputPath, args.IsCopy);
                    return;
                case Args.Operation.LinkId:
                    var onlineId = args.OnlineId;
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

    private static void RunExport(
        Api api,
        ExportFormat format,
        string? outPath,
        JsonExporter.ExportSettings? jsonSettings = null)
    {
        if (Directory.Exists(outPath))
        {
            Console.WriteLine($"{outPath} is a directory not a file");
            return;
        }

        switch (format)
        {
            case ExportFormat.Json:
                ExportJson(api, outPath, jsonSettings ?? new JsonExporter.ExportSettings());
                break;
            case ExportFormat.Binary:
                ExportBinary(api, outPath);
                break;
        }
    }

    private static void ExportJson(Api api, string? outPath, JsonExporter.ExportSettings settings)
    {
        if (outPath is not null)
        {
            using var file = System.IO.File.CreateText(outPath);
            api.ExportToJson(settings, file);
        }
        else
        {
            using var writer = new StreamWriter(Console.OpenStandardOutput());
            api.ExportToJson(settings, writer);
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