using System.CommandLine;

namespace OsuFilesUtility;

internal static class CommandLine
{
    internal sealed class Args
    {
        public const string DefaultOutPath = "./YOU-CAN-RENAME-THIS-AND-MOVE-THIS-ON-SAME-DRIVE";

        public required string LazerPath { get; init; }
        public required string? DiffLazerPath { get; init; }
        public required string OutPath { get; init; }
        public required string? ReplayPath { get; init; }
        public required bool IsCopy { get; init; }
        public required bool IsQuiet { get; init; }
        public required bool IsVerbose { get; init; }
        public required bool All { get; init; }
        public required ExportFormat? Export { get; init; }
        public required bool Validate { get; init; }
        public required string? MD5Hash { get; init; }
        public required long? OnlineID { get; init; }

        public bool CannotRun()
        {
            return this is
            {
                All: false,
                DiffLazerPath: null,
                Validate: false,
                MD5Hash: null,
                OnlineID: null,
                ReplayPath: null,
                Export: null,
            };
        }

        public bool CannotContinue()
        {
            return this is { All: false, DiffLazerPath: null, MD5Hash: null, OnlineID: null, ReplayPath: null };
        }

        public enum ExportFormat
        {
            Json,
            PrettyJson,
            Binary,
        }
    }

    public static void Run(string[] args)
    {
        RootCommand root = new();

        Option<DirectoryInfo> lazerPath = new("--dir", "-d")
        {
            DefaultValueFactory = _ => new DirectoryInfo(Api.GetDefaultLazerPath()),
            Description = "Path to lazer directory"
        };
        Option<DirectoryInfo?> diffLazerPath = new("--diff")
        {
            DefaultValueFactory = _ => null,
            Description = "Other Path to lazer directory (will implicitly use --all)"
        };
        Option<string> outPath = new("--out", "-o")
        {
            DefaultValueFactory = _ => Args.DefaultOutPath,
            Description = "Path to output directory or file (file is for exports)"
        };
        Option<FileInfo?> replayPath = new("--replay", "-r")
        {
            DefaultValueFactory = _ => null,
            Description = "Path to replay file"
        };
        Option<bool> isCopy = new("--copy", "-c")
        {
            DefaultValueFactory = _ => false,
            Description = "copy instead of symlinks"
        };
        Option<bool> isQuiet = new("--quiet", "-q")
        {
            DefaultValueFactory = _ => false,
            Description = "suppress output"
        };
        Option<bool> isVerbose = new("--verbose")
        {
            DefaultValueFactory = _ => false,
            Description = "show detailed info (does nothing now)"
        };
        Option<bool> all = new("--all", "-a")
        {
            DefaultValueFactory = _ => false,
            Description = "symlink all beatmaps, enabled if ran with no other args"
        };
        Option<Args.ExportFormat?> export = new("--export", "-e")
        {
            DefaultValueFactory = _ => null,
            Description = "Exports to a specified format, if no out file is specified prints to stdout"
        };
        Option<bool> validate = new("--validate", "-v")
        {
            DefaultValueFactory = _ => false,
            Description = "validate symlinks in output directory"
        };
        Option<string?> md5hash = new("--md5", "-m")
        {
            DefaultValueFactory = _ => null,
            Description = "beatmap md5hash"
        };
        Option<long?> onlineId = new("--id", "-i")
        {
            DefaultValueFactory = _ => null,
            Description = "beatmap online id (not beatset)"
        };

        root.Options.Add(lazerPath);
        root.Options.Add(diffLazerPath);
        root.Options.Add(outPath);
        root.Options.Add(replayPath);
        root.Options.Add(isCopy);
        root.Options.Add(isQuiet);
        root.Options.Add(isVerbose);
        root.Options.Add(all);
        root.Options.Add(export);
        root.Options.Add(validate);
        root.Options.Add(md5hash);
        root.Options.Add(onlineId);

        var defLazerPath = lazerPath.DefaultValueFactory(null!).FullName;

        if (!Directory.Exists(defLazerPath))
        {
            Console.WriteLine($"{defLazerPath} not found");
            Console.WriteLine("Maybe you have a custom lazer install? use -d to specify where to find it");
            return;
        }

        switch (args.Length)
        {
            // drag and dropped on exe
            case 1:
            {
                if (CheckArg(args[0], root.Options))
                {
                    break;
                }

                var programArgs = new Args
                {
                    LazerPath = defLazerPath,
                    DiffLazerPath = null,
                    OutPath = Path.GetFullPath(args[0]),
                    ReplayPath = null,
                    IsCopy = false,
                    IsQuiet = false,
                    IsVerbose = true,
                    All = true,
                    Export = null,
                    Validate = true,
                    MD5Hash = null,
                    OnlineID = null,
                };

                if (System.IO.File.Exists(programArgs.OutPath))
                {
                    Console.WriteLine($"{programArgs.OutPath} is not a directory");
                    ReadKey();
                    return;
                }

                if (CheckDrives(programArgs.LazerPath, programArgs.OutPath))
                {
                    return;
                }

                if (Directory.Exists(programArgs.OutPath) &&
                    Directory.EnumerateFileSystemEntries(programArgs.OutPath).Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("");
                    Console.WriteLine("Output directory contains files, confirm its correct");
                    Console.ResetColor();
                    Console.WriteLine("");
                }

                Console.WriteLine("This will validate existing symlinks and symlink all beatmaps!");
                Console.WriteLine(" ");
                Console.WriteLine($"Lazer path: {programArgs.LazerPath}");
                Console.WriteLine($"Output Path: {programArgs.OutPath}");
                Console.WriteLine(" ");
                Console.WriteLine("Press <ESC> or <Ctrl+C> to exit...");
                Console.WriteLine("Press any key to continue...");

                if (ReadKey())
                {
                    return;
                }

                Run(programArgs);
                return;
            }
            // double-clicking exe directly
            case 0:
            {
                var defOutPath = new DirectoryInfo(outPath.DefaultValueFactory(null!)).FullName;

                var programArgs = new Args
                {
                    LazerPath = defLazerPath,
                    DiffLazerPath = null,
                    OutPath = defOutPath,
                    ReplayPath = null,
                    IsCopy = false,
                    IsQuiet = false,
                    IsVerbose = true,
                    All = true,
                    Export = null,
                    Validate = false,
                    MD5Hash = null,
                    OnlineID = null,
                };

                if (CheckDrives(programArgs.LazerPath, programArgs.OutPath))
                {
                    return;
                }

                Console.WriteLine("This will symlink all beatmaps!");
                Console.WriteLine(" ");
                Console.WriteLine($"Lazer path: {programArgs.LazerPath}");
                Console.WriteLine($"Output Path: {programArgs.OutPath}");
                Console.WriteLine(" ");
                Console.WriteLine("Press <ESC> or <Ctrl+C> to exit...");
                Console.WriteLine("Press any key to continue...");

                if (ReadKey())
                {
                    return;
                }

                Run(programArgs);
                return;
            }
        }

        root.SetAction(parsed =>
        {
            var parsedArgs = new Args
            {
                LazerPath = parsed.GetValue(lazerPath)?.FullName!,
                DiffLazerPath = parsed.GetValue(diffLazerPath)?.FullName!,
                OutPath = parsed.GetValue(outPath)!,
                ReplayPath = parsed.GetValue(replayPath)?.FullName,
                IsCopy = parsed.GetValue(isCopy),
                IsQuiet = parsed.GetValue(isQuiet),
                IsVerbose = parsed.GetValue(isVerbose),
                All = parsed.GetValue(all),
                Export = parsed.GetValue(export),
                Validate = parsed.GetValue(validate),
                MD5Hash = parsed.GetValue(md5hash),
                OnlineID = parsed.GetValue(onlineId),
            };

            Run(parsedArgs);
            return 0;
        });

        root.Parse(args).Invoke();
    }

    private static bool CheckArg(string arg, IEnumerable<Option> options)
    {
        return options.Any(o => o.Name == arg || o.Aliases.Any(a => a == arg));
    }

    private static void Run(Args args)
    {
        if (args.CannotRun())
        {
            Console.WriteLine("Nothing to do, use one of [-a, -m, -i, -r] options");
            return;
        }

        var api = new Api(args.LazerPath, args.IsVerbose);

        if (args.IsQuiet)
        {
            Console.SetOut(TextWriter.Null);
        }

        if (args.Export is not null)
        {
            RunExport(api, args.Export.Value, args.OutPath);
            return;
        }

        if (System.IO.File.Exists(args.OutPath))
        {
            throw new FileLoadException("not a directory", args.OutPath);
        }

        if (!Directory.Exists(args.OutPath))
        {
            Directory.CreateDirectory(args.OutPath);
        }

        if (args.Validate)
        {
            Console.WriteLine("Validating paths, this can take a while...");
            Api.ValidatePaths(args.OutPath);
            Console.WriteLine("Validated paths");

            if (args.CannotContinue())
            {
                return;
            }
        }

        if (args.DiffLazerPath is not null)
        {
            RunDiff(api, args.OutPath, args.DiffLazerPath, args.IsCopy);
            return;
        }

        if (args.All)
        {
            Console.WriteLine("Creating links...");
            api.CreateLinksAll(args.OutPath, args.IsCopy);
            Console.WriteLine("Done.");
            return;
        }

        if (args.MD5Hash is not null || args.ReplayPath is not null)
        {
            var md5hash = args.MD5Hash ?? Api.GetMD5HashFromReplay(args.ReplayPath);
            var beatmap = api.Realm.All<Beatmap>().First(b => b.MD5Hash == md5hash);

            api.CreateLinks(beatmap, args.OutPath, args.IsCopy);
            return;
        }

        if (args.OnlineID is not null)
        {
            var onlineID = args.OnlineID;
            // Why do I need this?
            Func<IList<Beatmap>, bool> hasAny = maps => maps.Any(map => map.OnlineID == onlineID);

            var beatmap = api.Realm.All<BeatmapSet>().First(b =>
                b.OnlineID == onlineID || hasAny(b.Beatmaps));
            // this will not work "Unhandled exception: System.NotSupportedException: The method 'Any' is not supported"
            // b.OnlineID == onlineID || b.Beatmaps.Any(map => map.OnlineID == onlineID));

            api.CreateLinks(beatmap, args.OutPath, args.IsCopy);
        }
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

    private static void RunExport(Api api, Args.ExportFormat format, string? outPath)
    {
        outPath = outPath is Args.DefaultOutPath ? null : outPath;

        if (Directory.Exists(outPath))
        {
            Console.WriteLine($"{outPath} is a directory not a file");
            return;
        }

        switch (format)
        {
            case Args.ExportFormat.Json:
                ExportJson(api, outPath, false);
                break;
            case Args.ExportFormat.PrettyJson:
                ExportJson(api, outPath, true);
                break;
            case Args.ExportFormat.Binary:
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