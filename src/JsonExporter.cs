using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace OsuFilesUtility;

internal sealed class JsonExporter
{
    private readonly ExportSettings _settings;
    private readonly Api _api;

    internal sealed record ExportSettings(
        bool IsPretty = false,
        bool IsStream = false,
        bool RemoveEmpty = false,
        bool AllowNulls = false,
        ExportFlags Flags = ExportFlags.All
    );

    [Flags]
    internal enum ExportFlags
    {
        None = 0,
        Users = 1 << 0,
        Rulesets = 1 << 1,
        Beatmaps = 1 << 2,
        BeatmapSets = 1 << 3,
        Collections = 1 << 4,
        Scores = 1 << 5,
        Skins = 1 << 6,

        All = Users
              | Rulesets
              | Beatmaps
              | BeatmapSets
              | Collections
              | Scores
              | Skins
    }

    internal JsonExporter(ExportSettings settings, Api api)
    {
        _settings = settings;
        _api = api;
    }

    public void ExportStream()
    {
        if (_settings.Flags == ExportFlags.None)
            return;

        // var processingCount = Environment.ProcessorCount ;
        var processingCount = BitOperations.PopCount((uint)ExportFlags.All);

        var all = Enumerable.Empty<object>().AsQueryable();

        if (_settings.Flags.HasFlag(ExportFlags.Users))
            all = all.Concat(_api.NewRealmInstance().Freeze().All<RealmUser>());

        if (_settings.Flags.HasFlag(ExportFlags.Rulesets))
            all = all.Concat(_api.NewRealmInstance().Freeze().All<Ruleset>());

        if (_settings.Flags.HasFlag(ExportFlags.Beatmaps))
            all = all.Concat(_api.NewRealmInstance().Freeze().All<Beatmap>());

        if (_settings.Flags.HasFlag(ExportFlags.BeatmapSets))
            all = all.Concat(_api.NewRealmInstance().Freeze().All<BeatmapSet>());

        if (_settings.Flags.HasFlag(ExportFlags.Collections))
            all = all.Concat(_api.NewRealmInstance().Freeze().All<BeatmapCollection>());

        if (_settings.Flags.HasFlag(ExportFlags.Scores))
            all = all.Concat(_api.NewRealmInstance().Freeze().All<Score>());

        if (_settings.Flags.HasFlag(ExportFlags.Skins))
            all = all.Concat(_api.NewRealmInstance().Freeze().All<Skin>());

        var totalCount = all.Count();

        if (totalCount <= processingCount)
        {
            processingCount = 1;
        }

        var chunks = all.Chunk(all.Count() / processingCount);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = processingCount
        };

        var opts = new JsonWriterOptions { Indented = false, SkipValidation = true };
        using var stdout = Console.OpenStandardOutput();
        var outlock = new object();

        Parallel.ForEach(chunks, options, chunk =>
        {
            using var writer = new Utf8JsonWriter(stdout, opts);

            foreach (var obj in chunk)
            {
                switch (obj)
                {
                    case RealmUser user:
                        WriteUser(writer, user);
                        break;
                    case Ruleset ruleset:
                        WriteRuleset(writer, ruleset);
                        break;
                    case Beatmap map:
                        WriteBeatmap(writer, map);
                        break;
                    case BeatmapSet set:
                        WriteBeatmapSet(writer, set);
                        break;
                    case BeatmapCollection collection:
                        WriteCollection(writer, collection);
                        break;
                    case Score score:
                        WriteScore(writer, score);
                        break;
                    case Skin skin:
                        WriteSkin(writer, skin);
                        break;
                }

                lock (outlock)
                {
                    writer.Flush();
                    stdout.WriteByte((byte)'\n');
                    writer.Reset(stdout);
                }
            }
        });

        stdout.Flush();

        // if (_settings.Flags == ExportFlags.None)
        // {
        //     return;
        // }
        //
        // using var output = Console.OpenStandardOutput();
        // using var realm = _api.NewRealmInstance().Freeze();
        // var outputLock = new object();
        // var tasks = new List<Task>(BitOperations.PopCount((uint)ExportFlags.All));
        //
        // if (_settings.Flags.HasFlag(ExportFlags.Users))
        // {
        //     tasks.Add(WriteStream(
        //         realm.All<RealmUser>(),
        //         output,
        //         outputLock,
        //         WriteUser
        //     ));
        // }
        //
        // if (_settings.Flags.HasFlag(ExportFlags.Rulesets))
        // {
        //     tasks.Add(WriteStream(
        //         realm.All<Ruleset>(),
        //         output,
        //         outputLock,
        //         WriteRuleset
        //     ));
        // }
        //
        // if (_settings.Flags.HasFlag(ExportFlags.Beatmaps))
        // {
        //     tasks.Add(WriteStream(
        //         realm.All<Beatmap>(),
        //         output,
        //         outputLock,
        //         WriteBeatmap
        //     ));
        // }
        //
        // if (_settings.Flags.HasFlag(ExportFlags.BeatmapSets))
        // {
        //     tasks.Add(WriteStream(
        //         realm.All<BeatmapSet>(),
        //         output,
        //         outputLock,
        //         WriteBeatmapSet
        //     ));
        // }
        //
        // if (_settings.Flags.HasFlag(ExportFlags.Collections))
        // {
        //     tasks.Add(WriteStream(
        //         realm.All<BeatmapCollection>(),
        //         output,
        //         outputLock,
        //         WriteCollection
        //     ));
        // }
        //
        // if (_settings.Flags.HasFlag(ExportFlags.Scores))
        // {
        //     tasks.Add(WriteStream(
        //         realm.All<Score>(),
        //         output,
        //         outputLock,
        //         WriteScore
        //     ));
        // }
        //
        // if (_settings.Flags.HasFlag(ExportFlags.Skins))
        // {
        //     tasks.Add(WriteStream(
        //         realm.All<Skin>(),
        //         output,
        //         outputLock,
        //         WriteSkin
        //     ));
        // }
        //
        // Task.WhenAll(tasks).GetAwaiter().GetResult();
        // output.Flush();
    }

    // internal static Task WriteStream<T>(
    //     IQueryable<T> items,
    //     Stream output,
    //     object outputLock,
    //     Action<Utf8JsonWriter, T> writeItem)
    // {
    //     return Task.Run(() =>
    //     {
    //         using var writer = new Utf8JsonWriter(
    //             output,
    //             CreateWriterOptions(indented: false, skipValidation: true)
    //         );
    //
    //         foreach (var item in items)
    //         {
    //             writeItem(writer, item);
    //
    //             lock (outputLock)
    //             {
    //                 writer.Flush();
    //                 output.WriteByte((byte)'\n');
    //                 writer.Reset(output);
    //             }
    //         }
    //
    //         writer.Flush();
    //     });
    // }

    public string Export()
    {
        var output = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(output, CreateWriterOptions(_settings.IsPretty)))
        {
            writer.WriteStartObject();

            if (_settings.Flags.HasFlag(ExportFlags.Users))
            {
                var users = _api.Realm.All<RealmUser>()
                    .AsEnumerable()
                    .DistinctBy(item => item.OnlineID)
                    .OrderBy(item => item.OnlineID);

                writer.WriteObjectSection("Users", users, _settings.RemoveEmpty, WriteUserEntry);
            }

            if (_settings.Flags.HasFlag(ExportFlags.Rulesets))
            {
                var rulesets = _api.Realm.All<Ruleset>()
                    .AsEnumerable()
                    .OrderBy(item => item.OnlineID);

                writer.WriteObjectSection("Rulesets", rulesets, _settings.RemoveEmpty, WriteRulesetEntry);
            }

            if (_settings.Flags.HasFlag(ExportFlags.Beatmaps))
            {
                var beatmaps = _api.Realm.All<Beatmap>()
                    .AsEnumerable()
                    .OrderBy(item => item.MD5Hash);

                writer.WriteObjectSection("Beatmaps", beatmaps, _settings.RemoveEmpty, WriteBeatmapEntry);
            }

            if (_settings.Flags.HasFlag(ExportFlags.BeatmapSets))
            {
                var beatmapSets = _api.Realm.All<BeatmapSet>()
                    .AsEnumerable()
                    .OrderBy(item => item.OnlineID);

                writer.WriteObjectSection("BeatmapSets", beatmapSets, _settings.RemoveEmpty, WriteBeatmapSetEntry);
            }

            if (_settings.Flags.HasFlag(ExportFlags.Collections))
            {
                var collections = _api.Realm.All<BeatmapCollection>().AsEnumerable();
                writer.WriteArraySection("Collections", collections, _settings.RemoveEmpty, WriteCollection);
            }

            if (_settings.Flags.HasFlag(ExportFlags.Scores))
            {
                var scores = _api.Realm.All<Score>().AsEnumerable();
                writer.WriteArraySection("Scores", scores, _settings.RemoveEmpty, WriteScore);
            }

            if (_settings.Flags.HasFlag(ExportFlags.Skins))
            {
                var skins = _api.Realm.All<Skin>().AsEnumerable();
                writer.WriteArraySection("Skins", skins, _settings.RemoveEmpty, WriteSkin);
            }

            writer.WriteEndObject();
            writer.Flush();
        }

        return Encoding.UTF8.GetString(output.WrittenSpan);
    }

    internal static JsonWriterOptions CreateWriterOptions(bool indented, bool skipValidation = false)
    {
        return new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = indented,
            SkipValidation = skipValidation,
        };
    }

    internal void WriteUserEntry(Utf8JsonWriter writer, RealmUser user)
    {
        writer.WritePropertyName(user.OnlineID.ToString(CultureInfo.InvariantCulture));
        WriteUser(writer, user);
    }

    internal void WriteRulesetEntry(Utf8JsonWriter writer, Ruleset ruleset)
    {
        writer.WritePropertyName(ruleset.OnlineID.ToString(CultureInfo.InvariantCulture));
        WriteRuleset(writer, ruleset);
    }

    internal void WriteBeatmapEntry(Utf8JsonWriter writer, Beatmap beatmap)
    {
        writer.WritePropertyName(beatmap.MD5Hash);
        WriteBeatmap(writer, beatmap);
    }

    internal void WriteBeatmapSetEntry(Utf8JsonWriter writer, BeatmapSet beatmapSet)
    {
        writer.WritePropertyName(beatmapSet.OnlineID.ToString(CultureInfo.InvariantCulture));
        WriteBeatmapSet(writer, beatmapSet);
    }

    internal void WriteUser(Utf8JsonWriter writer, RealmUser user)
    {
        writer.WriteStartObject();

        if (_settings.IsStream)
        {
            writer.WriteString("Type", "User");
        }

        writer.WriteNumber("OnlineID", user.OnlineID);
        writer.WriteString("Username", user.Username);
        writer.WriteString("CountryCode", user.CountryCode);

        writer.WriteEndObject();
    }

    internal void WriteRuleset(Utf8JsonWriter writer, Ruleset ruleset)
    {
        writer.WriteStartObject();

        if (_settings.IsStream)
        {
            writer.WriteString("Type", "Ruleset");
        }

        writer.WriteString("ShortName", ruleset.ShortName);
        writer.WriteNumber("OnlineID", ruleset.OnlineID);
        writer.WriteString("Name", ruleset.Name);
        writer.WriteString("InstantiationInfo", ruleset.InstantiationInfo);
        writer.WriteNumber("LastAppliedDifficultyVersion", ruleset.LastAppliedDifficultyVersion);
        writer.WriteBoolean("Available", ruleset.Available);

        writer.WriteEndObject();
    }

    internal void WriteBeatmap(Utf8JsonWriter writer, Beatmap beatmap)
    {
        writer.WriteStartObject();

        if (_settings.IsStream)
        {
            writer.WriteString("Type", "Beatmap");
        }

        writer.WriteString("DifficultyName", beatmap.DifficultyName);
        writer.WriteNumber("Ruleset", beatmap.Ruleset.OnlineID);

        writer.WritePropertyName("Difficulty");
        writer.WriteStartObject();
        writer.WriteNumber("DrainRate", beatmap.Difficulty.DrainRate);
        writer.WriteNumber("CircleSize", beatmap.Difficulty.CircleSize);
        writer.WriteNumber("OverallDifficulty", beatmap.Difficulty.OverallDifficulty);
        writer.WriteNumber("ApproachRate", beatmap.Difficulty.ApproachRate);
        writer.WriteNumber("SliderMultiplier", beatmap.Difficulty.SliderMultiplier);
        writer.WriteNumber("SliderTickRate", beatmap.Difficulty.SliderTickRate);
        writer.WriteEndObject();

        writer.WritePropertyName("Metadata");
        writer.WriteStartObject();
        writer.WriteString("Title", beatmap.Metadata.Title);
        writer.WriteIfNotNull("TitleUnicode", beatmap.Metadata.TitleUnicode, _settings.AllowNulls);
        writer.WriteString("Artist", beatmap.Metadata.Artist);
        writer.WriteIfNotNull("ArtistUnicode", beatmap.Metadata.ArtistUnicode, _settings.AllowNulls);
        writer.WriteNumber("Author", beatmap.Metadata.Author.OnlineID);
        writer.WriteIfNotNull("Source", beatmap.Metadata.Source);
        writer.WriteIfNotNull("Tags", beatmap.Metadata.Tags, _settings.AllowNulls);
        writer.WriteNumber("PreviewTime", beatmap.Metadata.PreviewTime);
        writer.WriteString("AudioFile", beatmap.Metadata.AudioFile);
        writer.WriteIfNotNull("BackgroundFile", beatmap.Metadata.BackgroundFile, _settings.AllowNulls);

        writer.WritePropertyName("UserTags");
        writer.WriteStartArray();
        foreach (var tag in beatmap.Metadata.UserTags)
        {
            writer.WriteStringValue(tag);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.WritePropertyName("UserSettings");
        writer.WriteStartObject();
        writer.WriteNumber("Offset", beatmap.UserSettings.Offset);
        writer.WriteEndObject();

        writer.WriteNumber("BeatmapSet", beatmap.BeatmapSet.OnlineID);
        writer.WriteNumber("Status", beatmap.Status);
        writer.WriteNumber("OnlineID", beatmap.OnlineID);
        writer.WriteNumber("Length", beatmap.Length);
        writer.WriteNumber("BPM", beatmap.BPM);
        writer.WriteString("Hash", beatmap.Hash);
        writer.WriteNumber("StarRating", beatmap.StarRating);
        writer.WriteString("MD5Hash", beatmap.MD5Hash);
        writer.WriteIfNotNull("OnlineMD5Hash", beatmap.OnlineMD5Hash, _settings.AllowNulls);
        writer.WriteIfNotNull("LastLocalUpdate", beatmap.LastLocalUpdate, _settings.AllowNulls);
        writer.WriteIfNotNull("LastOnlineUpdate", beatmap.LastOnlineUpdate, _settings.AllowNulls);
        writer.WriteBoolean("Hidden", beatmap.Hidden);
        writer.WriteNumber("EndTimeObjectCount", beatmap.EndTimeObjectCount);
        writer.WriteNumber("TotalObjectCount", beatmap.TotalObjectCount);
        writer.WriteIfNotNull("LastPlayed", beatmap.LastPlayed, _settings.AllowNulls);
        writer.WriteNumber("BeatDivisor", beatmap.BeatDivisor);
        writer.WriteIfNotNull("EditorTimestamp", beatmap.EditorTimestamp, _settings.AllowNulls);

        writer.WriteEndObject();
    }

    internal void WriteBeatmapSet(Utf8JsonWriter writer, BeatmapSet beatmapSet)
    {
        writer.WriteStartObject();

        if (_settings.IsStream)
        {
            writer.WriteString("Type", "BeatmapSet");
        }

        writer.WriteNumber("OnlineID", beatmapSet.OnlineID);
        writer.WritePropertyName("Files");
        writer.WriteFiles(beatmapSet.Files);

        writer.WritePropertyName("Beatmaps");
        writer.WriteStartArray();
        foreach (var beatmap in beatmapSet.Beatmaps)
        {
            writer.WriteStringValue(beatmap.MD5Hash);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal void WriteScore(Utf8JsonWriter writer, Score score)
    {
        writer.WriteStartObject();

        if (_settings.IsStream)
        {
            writer.WriteString("Type", "Score");
        }

        if (score.BeatmapInfo is not null)
        {
            writer.WriteIfNotNull("BeatmapInfo", score.BeatmapInfo.MD5Hash, _settings.AllowNulls);
        }

        writer.WriteIfNotNull("ClientVersion", score.ClientVersion, _settings.AllowNulls);
        writer.WriteString("BeatmapHash", score.BeatmapHash);
        writer.WriteNumber("Ruleset", score.Ruleset.OnlineID);
        writer.WritePropertyName("Files");
        writer.WriteFiles(score.Files);
        writer.WriteString("Hash", score.Hash);
        writer.WriteBoolean("DeletePending", score.DeletePending);
        writer.WriteNumber("TotalScore", score.TotalScore);
        writer.WriteNumber("TotalScoreWithoutMods", score.TotalScoreWithoutMods);
        writer.WriteNumber("TotalScoreVersion", score.TotalScoreVersion);
        writer.WriteIfNotNull("LegacyTotalScore", score.LegacyTotalScore, _settings.AllowNulls);
        writer.WriteBoolean("BackgroundReprocessingFailed", score.BackgroundReprocessingFailed);
        writer.WriteNumber("MaxCombo", score.MaxCombo);
        writer.WriteNumber("Accuracy", score.Accuracy);
        writer.WriteDateTime("Date", score.Date);
        writer.WriteIfNotNull("PP", score.PP, _settings.AllowNulls);
        writer.WriteNumber("OnlineID", score.OnlineID);
        writer.WriteNumber("LegacyOnlineID", score.LegacyOnlineID);
        writer.WriteNumber("User", score.User.OnlineID);
        writer.WriteIfNotNull("Mods", score.Mods, _settings.AllowNulls);
        writer.WriteString("Statistics", score.Statistics);
        writer.WriteString("MaximumStatistics", score.MaximumStatistics);
        writer.WriteNumber("Rank", score.Rank);
        writer.WriteNumber("Combo", score.Combo);
        writer.WriteBoolean("IsLegacyScore", score.IsLegacyScore);

        writer.WritePropertyName("Pauses");
        writer.WriteStartArray();
        foreach (var pause in score.Pauses)
        {
            writer.WriteNumberValue(pause);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal void WriteCollection(Utf8JsonWriter writer, BeatmapCollection collection)
    {
        writer.WriteStartObject();

        if (_settings.IsStream)
        {
            writer.WriteString("Type", "Collection");
        }

        writer.WriteString("Name", collection.Name);

        writer.WritePropertyName("BeatmapMD5Hashes");
        writer.WriteStartArray();
        foreach (var hash in collection.BeatmapMD5Hashes)
        {
            writer.WriteStringValue(hash);
        }

        writer.WriteEndArray();
        writer.WriteDateTime("LastModified", collection.LastModified);
        writer.WriteEndObject();
    }

    internal void WriteSkin(Utf8JsonWriter writer, Skin skin)
    {
        writer.WriteStartObject();

        if (_settings.IsStream)
        {
            writer.WriteString("Type", "Skin");
        }

        writer.WriteString("Name", skin.Name);
        writer.WriteString("Creator", skin.Creator);
        writer.WriteString("InstantiationInfo", skin.InstantiationInfo);
        writer.WriteIfNotNull("Hash", skin.Hash, _settings.AllowNulls);
        writer.WriteBoolean("Protected", skin.Protected);
        writer.WritePropertyName("Files");
        writer.WriteFiles(skin.Files);
        writer.WriteBoolean("DeletePending", skin.DeletePending);

        writer.WriteEndObject();
    }
}