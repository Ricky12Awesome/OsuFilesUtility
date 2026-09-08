using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OsuFilesUtility;

internal sealed class JsonExporter
{
    private readonly ExportSettings _settings;
    private readonly Api _api;

    internal sealed record ExportSettings(
        bool IsPretty = false,
        bool IsStream = false,
        bool RemoveEmpty = false,
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
        var tasks = new List<Task>();

        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        if (_settings.Flags.HasFlag(ExportFlags.Users))
        {
            tasks.Add(Task.Run(async () =>
            {
                var realm = _api.NewRealmInstance();

                await foreach (var realmUser in realm.All<RealmUser>()
                                   .ToAsyncEnumerable()
                                   .DistinctBy(item => item.OnlineID))
                {
                    var jsonObject = CreateUser(realmUser);
                    var json = jsonObject.ToJsonString(options);

                    await Console.Out.WriteLineAsync(json);
                    await Console.Out.FlushAsync();
                }
            }));
        }

        if (_settings.Flags.HasFlag(ExportFlags.Rulesets))
        {
            tasks.Add(Task.Run(async () =>
            {
                var realm = _api.NewRealmInstance();
                await foreach (var ruleset in realm.All<Ruleset>().ToAsyncEnumerable())
                {
                    var jsonObject = CreateRuleset(ruleset);
                    var json = jsonObject.ToJsonString(options);

                    await Console.Out.WriteLineAsync(json);
                    await Console.Out.FlushAsync();
                }
            }));
        }

        if (_settings.Flags.HasFlag(ExportFlags.Beatmaps))
        {
            tasks.Add(Task.Run(async () =>
            {
                var realm = _api.NewRealmInstance();
                await foreach (var beatmap in realm.All<Beatmap>().ToAsyncEnumerable())
                {
                    var jsonObject = CreateBeatmap(beatmap);
                    var json = jsonObject.ToJsonString(options);

                    await Console.Out.WriteLineAsync(json);
                    await Console.Out.FlushAsync();
                }
            }));
        }

        if (_settings.Flags.HasFlag(ExportFlags.BeatmapSets))
        {
            tasks.Add(Task.Run(async () =>
            {
                var realm = _api.NewRealmInstance();
                await foreach (var beatmapSet in realm.All<BeatmapSet>().ToAsyncEnumerable())
                {
                    var jsonObject = CreateBeatmapSet(beatmapSet);
                    var json = jsonObject.ToJsonString(options);

                    await Console.Out.WriteLineAsync(json);
                    await Console.Out.FlushAsync();
                }
            }));
        }

        if (_settings.Flags.HasFlag(ExportFlags.Collections))
        {
            tasks.Add(Task.Run(async () =>
            {
                var realm = _api.NewRealmInstance();
                await foreach (var beatmapCollection in realm.All<BeatmapCollection>().ToAsyncEnumerable())
                {
                    var jsonObject = CreateCollection(beatmapCollection);
                    var json = jsonObject.ToJsonString(options);

                    await Console.Out.WriteLineAsync(json);
                    await Console.Out.FlushAsync();
                }
            }));
        }

        if (_settings.Flags.HasFlag(ExportFlags.Scores))
        {
            tasks.Add(Task.Run(async () =>
            {
                var realm = _api.NewRealmInstance();
                await foreach (var score in realm.All<Score>().ToAsyncEnumerable())
                {
                    var jsonObject = CreateScore(score);
                    var json = jsonObject.ToJsonString(options);

                    await Console.Out.WriteLineAsync(json);
                    await Console.Out.FlushAsync();
                }
            }));
        }

        if (_settings.Flags.HasFlag(ExportFlags.Skins))
        {
            tasks.Add(Task.Run(async () =>
            {
                var realm = _api.NewRealmInstance();
                await foreach (var skin in realm.All<Skin>().ToAsyncEnumerable())
                {
                    var jsonObject = CreateSkin(skin);
                    var json = jsonObject.ToJsonString(options);

                    await Console.Out.WriteLineAsync(json);
                    await Console.Out.FlushAsync();
                }
            }));
        }

        var all = Task.WhenAll(tasks);

        all.GetAwaiter().GetResult();
    }

    public string Export()
    {
        var root = new JsonObject();
        var usersRoot = new JsonObject();
        var rulesetsRoot = new JsonObject();
        var beatmapsRoot = new JsonObject();
        var beatmapSetsRoot = new JsonObject();
        var collectionsRoot = new JsonArray();
        var scoresRoot = new JsonArray();
        var skinsRoot = new JsonArray();

        if (_settings.Flags.HasFlag(ExportFlags.Users))
        {
            var users = _api.Realm.All<RealmUser>()
                .AsEnumerable()
                .DistinctBy(item => item.OnlineID) // idky there is duplicate users, so I have to have this
                .ToImmutableSortedDictionary(key => key.OnlineID, value => value);

            AddUsers(usersRoot, users);
        }

        if (_settings.Flags.HasFlag(ExportFlags.Rulesets))
        {
            var rulesets = _api.Realm.All<Ruleset>()
                .ToImmutableSortedDictionary(key => key.OnlineID, value => value);

            AddRulesets(rulesetsRoot, rulesets);
        }

        if (_settings.Flags.HasFlag(ExportFlags.Beatmaps))
        {
            var beatmaps = _api.Realm.All<Beatmap>()
                .ToImmutableSortedDictionary(key => key.MD5Hash, value => value);

            AddBeatmaps(beatmapsRoot, beatmaps);
        }

        if (_settings.Flags.HasFlag(ExportFlags.BeatmapSets))
        {
            var beatmapsets = _api.Realm.All<BeatmapSet>()
                .ToImmutableSortedDictionary(key => key.OnlineID, value => value);

            AddBeatmapSets(beatmapSetsRoot, beatmapsets);
        }

        if (_settings.Flags.HasFlag(ExportFlags.Collections))
        {
            var collections = _api.Realm.All<BeatmapCollection>().ToImmutableList();

            AddCollections(collectionsRoot, collections);
        }

        if (_settings.Flags.HasFlag(ExportFlags.Scores))
        {
            var scores = _api.Realm.All<Score>().ToImmutableList();

            AddScores(scoresRoot, scores);
        }

        if (_settings.Flags.HasFlag(ExportFlags.Skins))
        {
            var skins = _api.Realm.All<Skin>().ToImmutableList();

            AddSkins(skinsRoot, skins);
        }

        if (!_settings.RemoveEmpty || usersRoot.Count > 0)
            root.Add("Users", usersRoot);

        if (!_settings.RemoveEmpty || rulesetsRoot.Count > 0)
            root.Add("Rulesets", rulesetsRoot);

        if (!_settings.RemoveEmpty || beatmapsRoot.Count > 0)
            root.Add("Beatmaps", beatmapsRoot);

        if (!_settings.RemoveEmpty || beatmapSetsRoot.Count > 0)
            root.Add("BeatmapSets", beatmapSetsRoot);

        if (!_settings.RemoveEmpty || collectionsRoot.Count > 0)
            root.Add("Collections", collectionsRoot);

        if (!_settings.RemoveEmpty || scoresRoot.Count > 0)
            root.Add("Scores", scoresRoot);

        if (!_settings.RemoveEmpty || skinsRoot.Count > 0)
            root.Add("Skins", skinsRoot);

        return root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = _settings.IsPretty,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }

    private void AddUsers(JsonObject usersRoot, IEnumerable<KeyValuePair<long, RealmUser>> users)
    {
        foreach (var (id, user) in users)
        {
            var userRoot = CreateUser(user);

            usersRoot.Add($"{id}", userRoot);
        }
    }

    private JsonObject CreateUser(RealmUser user)
    {
        var userRoot = new JsonObject();

        if (_settings.IsStream)
        {
            userRoot["Type"] = "User";
        }

        userRoot.AddIfNotNull("OnlineID", user.OnlineID);
        userRoot.AddIfNotNull("Username", user.Username);
        userRoot.AddIfNotNull("CountryCode", user.CountryCode);

        return userRoot;
    }

    private void AddRulesets(JsonObject rulesetsRoot, IEnumerable<KeyValuePair<long, Ruleset>> rulesets)
    {
        foreach (var (id, ruleset) in rulesets)
        {
            var rulesetRoot = CreateRuleset(ruleset);

            rulesetsRoot.Add($"{id}", rulesetRoot);
        }
    }

    private JsonObject CreateRuleset(Ruleset ruleset)
    {
        var rulesetRoot = new JsonObject();

        if (_settings.IsStream)
        {
            rulesetRoot["Type"] = "Ruleset";
        }

        rulesetRoot.AddIfNotNull("ShortName", ruleset.ShortName);
        rulesetRoot.AddIfNotNull("OnlineID", ruleset.OnlineID);
        rulesetRoot.AddIfNotNull("Name", ruleset.Name);
        rulesetRoot.AddIfNotNull("InstantiationInfo", ruleset.InstantiationInfo);
        rulesetRoot.AddIfNotNull("LastAppliedDifficultyVersion", ruleset.LastAppliedDifficultyVersion);
        rulesetRoot.AddIfNotNull("Available", ruleset.Available);

        return rulesetRoot;
    }

    private void AddBeatmaps(JsonObject beatmapsRoot, IEnumerable<KeyValuePair<string, Beatmap>> beatmaps)
    {
        foreach (var (md5, beatmap) in beatmaps)
        {
            var beatmapRoot = CreateBeatmap(beatmap);

            beatmapsRoot.Add(md5, beatmapRoot);
        }
    }

    private JsonObject CreateBeatmap(Beatmap beatmap)
    {
        var beatmapRoot = new JsonObject();
        var metadataRoot = new JsonObject();

        if (_settings.IsStream)
        {
            beatmapRoot["Type"] = "Beatmap";
        }

        metadataRoot.AddIfNotNull("Title", beatmap.Metadata.Title);
        metadataRoot.AddIfNotNull("TitleUnicode", beatmap.Metadata.TitleUnicode);
        metadataRoot.AddIfNotNull("Artist", beatmap.Metadata.Artist);
        metadataRoot.AddIfNotNull("ArtistUnicode", beatmap.Metadata.ArtistUnicode);
        metadataRoot.AddIfNotNull("Author", beatmap.Metadata.Author.OnlineID);
        metadataRoot.AddIfNotNull("Source", beatmap.Metadata.Source);
        metadataRoot.AddIfNotNull("Tags", beatmap.Metadata.Tags);
        metadataRoot.AddIfNotNull("PreviewTime", beatmap.Metadata.PreviewTime);
        metadataRoot.AddIfNotNull("AudioFile", beatmap.Metadata.AudioFile);
        metadataRoot.AddIfNotNull("BackgroundFile", beatmap.Metadata.BackgroundFile);

        var userTags = new JsonArray();
        foreach (var tag in beatmap.Metadata.UserTags)
        {
            userTags.Add(tag);
        }

        metadataRoot.AddIfNotNull("UserTags", userTags);
        beatmapRoot.AddIfNotNull("DifficultyName", beatmap.DifficultyName);
        beatmapRoot.AddIfNotNull("Ruleset", beatmap.Ruleset.OnlineID);
        beatmapRoot.AddIfNotNull("Difficulty", new JsonObject
        {
            ["DrainRate"] = beatmap.Difficulty.DrainRate,
            ["CircleSize"] = beatmap.Difficulty.CircleSize,
            ["OverallDifficulty"] = beatmap.Difficulty.OverallDifficulty,
            ["ApproachRate"] = beatmap.Difficulty.ApproachRate,
            ["SliderMultiplier"] = beatmap.Difficulty.SliderMultiplier,
            ["SliderTickRate"] = beatmap.Difficulty.SliderTickRate,
        });

        beatmapRoot.AddIfNotNull("Metadata", metadataRoot);
        beatmapRoot.AddIfNotNull("UserSettings", new JsonObject
        {
            ["Offset"] = beatmap.UserSettings.Offset
        });

        beatmapRoot.AddIfNotNull("BeatmapSet", beatmap.BeatmapSet.OnlineID);
        beatmapRoot.AddIfNotNull("Status", beatmap.Status);
        beatmapRoot.AddIfNotNull("OnlineID", beatmap.OnlineID);
        beatmapRoot.AddIfNotNull("Length", beatmap.Length);
        beatmapRoot.AddIfNotNull("BPM", beatmap.BPM);
        beatmapRoot.AddIfNotNull("Hash", beatmap.Hash);
        beatmapRoot.AddIfNotNull("StarRating", beatmap.StarRating);
        beatmapRoot.AddIfNotNull("MD5Hash", beatmap.MD5Hash);
        beatmapRoot.AddIfNotNull("OnlineMD5Hash", beatmap.OnlineMD5Hash);
        beatmapRoot.AddIfNotNull("LastLocalUpdate", beatmap.LastLocalUpdate);
        beatmapRoot.AddIfNotNull("LastOnlineUpdate", beatmap.LastOnlineUpdate);
        beatmapRoot.AddIfNotNull("Hidden", beatmap.Hidden);
        beatmapRoot.AddIfNotNull("EndTimeObjectCount", beatmap.EndTimeObjectCount);
        beatmapRoot.AddIfNotNull("TotalObjectCount", beatmap.TotalObjectCount);
        beatmapRoot.AddIfNotNull("LastPlayed", beatmap.LastPlayed);
        beatmapRoot.AddIfNotNull("BeatDivisor", beatmap.BeatDivisor);
        beatmapRoot.AddIfNotNull("EditorTimestamp", beatmap.EditorTimestamp);

        return beatmapRoot;
    }

    private void AddBeatmapSets(JsonObject beatmapSetsRoot,
        IEnumerable<KeyValuePair<long, BeatmapSet>> beatmapsets)
    {
        foreach (var (id, beatmapset) in beatmapsets)
        {
            var beatmapSetRoot = CreateBeatmapSet(beatmapset);

            beatmapSetsRoot.Add($"{id}", beatmapSetRoot);
        }
    }

    private JsonObject CreateBeatmapSet(BeatmapSet beatmapset)
    {
        var beatmapSetRoot = new JsonObject();
        var files = CreateFilesObject(beatmapset.Files);

        if (_settings.IsStream)
        {
            beatmapSetRoot["Type"] = "BeatmapSet";
        }

        beatmapSetRoot.Add("OnlineID", beatmapset.OnlineID);
        beatmapSetRoot.Add("Files", files);

        var beatmapsetBeatmaps = new JsonArray();
        foreach (var beatmapsetBeatmap in beatmapset.Beatmaps)
        {
            beatmapsetBeatmaps.Add(beatmapsetBeatmap.MD5Hash);
        }

        beatmapSetRoot.Add("Beatmaps", beatmapsetBeatmaps);

        return beatmapSetRoot;
    }

    private void AddScores(JsonArray scoresRoot, IEnumerable<Score> scores)
    {
        foreach (var score in scores)
        {
            var scoreRoot = CreateScore(score);

            scoresRoot.Add(scoreRoot);
        }
    }

    private JsonObject CreateScore(Score score)
    {
        var scoreRoot = new JsonObject();

        if (_settings.IsStream)
        {
            scoreRoot["Type"] = "Score";
        }

        if (score.BeatmapInfo is not null)
        {
            scoreRoot.AddIfNotNull("BeatmapInfo", score.BeatmapInfo.MD5Hash);
        }

        scoreRoot.AddIfNotNull("ClientVersion", score.ClientVersion);
        scoreRoot.AddIfNotNull("BeatmapHash", score.BeatmapHash);
        scoreRoot.AddIfNotNull("Ruleset", score.Ruleset.OnlineID);
        scoreRoot.AddIfNotNull("Files", CreateFilesObject(score.Files));
        scoreRoot.AddIfNotNull("Hash", score.Hash);
        scoreRoot.AddIfNotNull("DeletePending", score.DeletePending);
        scoreRoot.AddIfNotNull("TotalScore", score.TotalScore);
        scoreRoot.AddIfNotNull("TotalScoreWithoutMods", score.TotalScoreWithoutMods);
        scoreRoot.AddIfNotNull("TotalScoreVersion", score.TotalScoreVersion);
        scoreRoot.AddIfNotNull("LegacyTotalScore", score.LegacyTotalScore);
        scoreRoot.AddIfNotNull("BackgroundReprocessingFailed", score.BackgroundReprocessingFailed);
        scoreRoot.AddIfNotNull("MaxCombo", score.MaxCombo);
        scoreRoot.AddIfNotNull("Accuracy", score.Accuracy);
        scoreRoot.AddIfNotNull("Date", score.Date);
        scoreRoot.AddIfNotNull("PP", score.PP);
        scoreRoot.AddIfNotNull("OnlineID", score.OnlineID);
        scoreRoot.AddIfNotNull("LegacyOnlineID", score.LegacyOnlineID);
        scoreRoot.AddIfNotNull("User", score.User.OnlineID);
        scoreRoot.AddIfNotNull("Mods", score.Mods);
        scoreRoot.AddIfNotNull("Statistics", score.Statistics);
        scoreRoot.AddIfNotNull("MaximumStatistics", score.MaximumStatistics);
        scoreRoot.AddIfNotNull("Rank", score.Rank);
        scoreRoot.AddIfNotNull("Combo", score.Combo);
        scoreRoot.AddIfNotNull("IsLegacyScore", score.IsLegacyScore);

        var pauses = new JsonArray();
        foreach (var pause in score.Pauses)
        {
            pauses.Add(pause);
        }

        scoreRoot.AddIfNotNull("Pauses", pauses);

        return scoreRoot;
    }

    private void AddCollections(JsonArray collectionsRoot, IEnumerable<BeatmapCollection> collections)
    {
        foreach (var collection in collections)
        {
            var collectionRoot = CreateCollection(collection);

            collectionsRoot.Add(collectionRoot);
        }
    }

    private JsonObject CreateCollection(BeatmapCollection collection)
    {
        var collectionRoot = new JsonObject();

        if (_settings.IsStream)
        {
            collectionRoot["Type"] = "Collection";
        }

        collectionRoot.AddIfNotNull("Name", collection.Name);

        var hashes = new JsonArray();
        foreach (var hash in collection.BeatmapMD5Hashes)
        {
            hashes.Add(hash);
        }

        collectionRoot.AddIfNotNull("BeatmapMD5Hashes", hashes);
        collectionRoot.AddIfNotNull("LastModified", collection.LastModified);

        return collectionRoot;
    }

    private void AddSkins(JsonArray skinsRoot, IEnumerable<Skin> skins)
    {
        foreach (var skin in skins)
        {
            var skinRoot = CreateSkin(skin);

            skinsRoot.Add(skinRoot);
        }
    }

    private JsonObject CreateSkin(Skin skin)
    {
        var skinRoot = new JsonObject();

        if (_settings.IsStream)
        {
            skinRoot["Type"] = "Skin";
        }

        skinRoot.AddIfNotNull("Name", skin.Name);
        skinRoot.AddIfNotNull("Creator", skin.Creator);
        skinRoot.AddIfNotNull("InstantiationInfo", skin.InstantiationInfo);
        skinRoot.AddIfNotNull("Hash", skin.Hash);
        skinRoot.AddIfNotNull("Protected", skin.Protected);
        skinRoot.AddIfNotNull("Files", CreateFilesObject(skin.Files));
        skinRoot.AddIfNotNull("DeletePending", skin.DeletePending);

        return skinRoot;
    }

    private static JsonObject CreateFilesObject(IEnumerable<RealmNamedFileUsage> files)
    {
        var result = new JsonObject();
        foreach (var file in files)
        {
            result[file.Filename] = file.File.Hash;
        }

        return result;
    }
}
