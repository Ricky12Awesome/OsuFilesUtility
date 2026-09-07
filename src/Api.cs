using System.Runtime.InteropServices;
using System.Text;
using Realms;

// ReSharper disable MemberCanBePrivate.Global

namespace OsuFilesUtility;

public class Api
{
    public Realm Realm { get; private set; }
    public string LazerPath { get; private set; }
    public bool Verbose { get; private set; }

    // ReSharper disable InconsistentNaming
    public Api(string? _lazerPath, bool verbose)
    {
        var lazerPath = _lazerPath ?? GetDefaultLazerPath();
        var realmPath = Path.Join(lazerPath, "client.realm");
        var filesPath = Path.Join(lazerPath, "files");

        if (!Path.Exists(lazerPath)) throw new FileNotFoundException(lazerPath);
        if (!Path.Exists(realmPath)) throw new FileNotFoundException(realmPath);
        if (!Path.Exists(filesPath)) throw new FileNotFoundException(filesPath);

        var config = new RealmConfiguration(realmPath)
        {
            SchemaVersion = 52,
            IsReadOnly = true,
            Schema = RealmSchema.Types
        };

        Realm = Realm.GetInstance(config);
        LazerPath = lazerPath;
        Verbose = verbose;
    }

    // https://osu.ppy.sh/wiki/en/Client/Release_stream/Lazer/File_storage
    public static string GetDefaultLazerPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appdata = Environment.GetEnvironmentVariable("APPDATA");

            return appdata is null ? "" : Path.Join(appdata, "osu");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetEnvironmentVariable("HOME");

            return home is null ? "" : Path.Join(home, ".local", "share", "osu");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetEnvironmentVariable("HOME");

            return home is null ? "" : Path.Join(home, "Library", "Application Support", "osu");
        }

        return "";
    }

    // https://osu.ppy.sh/wiki/en/Client/File_formats/osr_%28file_format%29
    public static string GetMd5HashFromReplay(string? path)
    {
        if (!Path.Exists(path)) throw new FileLoadException("Path to replay file not found");

        using var stream = System.IO.File.OpenRead(path);

        if (!stream.CanRead) throw new FileLoadException("Failed to read replay file.");

        Span<byte> buf = stackalloc byte[39];
        stream.ReadExactly(buf);

        return Encoding.UTF8.GetString(buf[7..]);
    }

    public static void ValidatePaths(string outPath)
    {
        var files = Directory.EnumerateFiles(outPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var info = System.IO.File.ResolveLinkTarget(file, false);

            if (info is not null && !info.Exists)
            {
                // todo: add logging here for removed
                System.IO.File.Delete(file);
            }
        }

        var dirs = Directory.EnumerateDirectories(outPath, "*", SearchOption.AllDirectories);

        foreach (var dir in dirs)
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                // todo: add logging here for removed
                Directory.Delete(dir);
            }
        }
    }

    public void CreateLinksAll(string outPath, bool isCopy = false)
    {
        var beatmaps = Realm.All<Beatmap>();

        foreach (var beatmap in beatmaps)
        {
            CreateLinks(beatmap, outPath, isCopy);
        }
    }

    public void CreateLinks(BeatmapSet set, string outPath, bool isCopy = false)
    {
        CreateLinks(set.OnlineID, set.Files, outPath, isCopy);
    }

    public void CreateLinks(Beatmap beatmap, string outPath, bool isCopy = false)
    {
        // just id because parsing title and artist can be invalid for paths
        CreateLinks(beatmap.BeatmapSet.OnlineID, beatmap.BeatmapSet.Files, outPath, isCopy);
    }

    private void CreateLinks(long onlineId, IEnumerable<RealmNamedFileUsage> files, string outPath, bool isCopy)
    {
        var dirname = onlineId.ToString();
        var dirpath = Path.Join(outPath, dirname);

        if (Directory.Exists(dirpath))
        {
            return;
        }

        Directory.CreateDirectory(dirpath);

        foreach (var f in files)
        {
            var src = Path.Join(LazerPath, "files", f.File.Hash[..1], f.File.Hash[..2], f.File.Hash);
            var dst = Path.Join(dirpath, f.Filename);

            if (Verbose)
            {
                // todo: add logging here
                // Console.WriteLine($"{src} -> {dirname}/{f.Filename}");
            }

            Directory.CreateDirectory(Directory.GetParent(dst)!.FullName);

            if (isCopy)
            {
                System.IO.File.Copy(src, dst, false);
                return;
            }

            System.IO.File.CreateSymbolicLink(dst, src);
        }
    }

    public string ExportToJson(bool pretty)
    {
        return JsonExporter.Export(this, pretty);
    }

    public void ExportToBinary(BinaryWriter writer)
    {
        BinaryExporter.Export(this, writer);
    }
}