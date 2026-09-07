using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OsuFilesUtility;

internal static class JsonExporter
{
    internal sealed record ExportSettings(
        bool IsPretty = false,
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

    public static string Export(Api api, ExportSettings settings)
    {
        var root = new JsonObject();
        var usersRoot = new JsonObject();
        var rulesetsRoot = new JsonObject();
        var beatmapsRoot = new JsonObject();
        var beatmapSetsRoot = new JsonObject();
        var collectionsRoot = new JsonArray();
        var scoresRoot = new JsonArray();
        var skinsRoot = new JsonArray();

        if (settings.Flags.HasFlag(ExportFlags.Users))
        {
            var users = api.Realm.All<RealmUser>()
                .AsEnumerable()
                .DistinctBy(item => item.OnlineID) // idky there is duplicate users, so I have to have this
                .ToImmutableSortedDictionary(key => key.OnlineID, value => value);

            AddUsers(usersRoot, users);
        }

        if (settings.Flags.HasFlag(ExportFlags.Rulesets))
        {
            var rulesets = api.Realm.All<Ruleset>()
                .ToImmutableSortedDictionary(key => key.OnlineID, value => value);

            AddRulesets(rulesetsRoot, rulesets);
        }

        if (settings.Flags.HasFlag(ExportFlags.Beatmaps))
        {
            var beatmaps = api.Realm.All<Beatmap>()
                .ToImmutableSortedDictionary(key => key.MD5Hash, value => value);

            AddBeatmaps(beatmapsRoot, beatmaps);
        }

        if (settings.Flags.HasFlag(ExportFlags.BeatmapSets))
        {
            var beatmapsets = api.Realm.All<BeatmapSet>()
                .ToImmutableSortedDictionary(key => key.OnlineID, value => value);

            AddBeatmapSets(beatmapSetsRoot, beatmapsets);
        }

        if (settings.Flags.HasFlag(ExportFlags.Collections))
        {
            var collections = api.Realm.All<BeatmapCollection>().ToImmutableList();

            AddCollections(collectionsRoot, collections);
        }

        if (settings.Flags.HasFlag(ExportFlags.Scores))
        {
            var scores = api.Realm.All<Score>().ToImmutableList();

            AddScores(scoresRoot, scores);
        }

        if (settings.Flags.HasFlag(ExportFlags.Skins))
        {
            var skins = api.Realm.All<Skin>().ToImmutableList();

            AddSkins(skinsRoot, skins);
        }

        if (!settings.RemoveEmpty || usersRoot.Count > 0)
            root.Add("Users", usersRoot);

        if (!settings.RemoveEmpty || rulesetsRoot.Count > 0)
            root.Add("Rulesets", rulesetsRoot);

        if (!settings.RemoveEmpty || beatmapsRoot.Count > 0)
            root.Add("Beatmaps", beatmapsRoot);

        if (!settings.RemoveEmpty || beatmapSetsRoot.Count > 0)
            root.Add("BeatmapSets", beatmapSetsRoot);

        if (!settings.RemoveEmpty || collectionsRoot.Count > 0)
            root.Add("Collections", collectionsRoot);

        if (!settings.RemoveEmpty || scoresRoot.Count > 0)
            root.Add("Scores", scoresRoot);

        if (!settings.RemoveEmpty || skinsRoot.Count > 0)
            root.Add("Skins", skinsRoot);


        return root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = settings.IsPretty,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }

    private static void AddUsers(JsonObject usersRoot, IEnumerable<KeyValuePair<long, RealmUser>> users)
    {
        foreach (var (id, user) in users)
        {
            var userRoot = new JsonObject();

            userRoot.AddIfNotNull("OnlineID", user.OnlineID);
            userRoot.AddIfNotNull("Username", user.Username);
            userRoot.AddIfNotNull("CountryCode", user.CountryCode);

            usersRoot.Add($"{id}", userRoot);
        }
    }

    private static void AddRulesets(JsonObject rulesetsRoot, IEnumerable<KeyValuePair<long, Ruleset>> rulesets)
    {
        foreach (var (id, ruleset) in rulesets)
        {
            var rulesetRoot = new JsonObject();

            rulesetRoot.AddIfNotNull("ShortName", ruleset.ShortName);
            rulesetRoot.AddIfNotNull("OnlineID", ruleset.OnlineID);
            rulesetRoot.AddIfNotNull("Name", ruleset.Name);
            rulesetRoot.AddIfNotNull("InstantiationInfo", ruleset.InstantiationInfo);
            rulesetRoot.AddIfNotNull("LastAppliedDifficultyVersion", ruleset.LastAppliedDifficultyVersion);
            rulesetRoot.AddIfNotNull("Available", ruleset.Available);

            rulesetsRoot.Add($"{id}", rulesetRoot);
        }
    }

    private static void AddBeatmaps(JsonObject beatmapsRoot, IEnumerable<KeyValuePair<string, Beatmap>> beatmaps)
    {
        foreach (var (md5, beatmap) in beatmaps)
        {
            var beatmapRoot = new JsonObject();
            var metadataRoot = new JsonObject();

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

            beatmapsRoot.Add(md5, beatmapRoot);
        }
    }

    private static void AddBeatmapSets(JsonObject beatmapSetsRoot,
        IEnumerable<KeyValuePair<long, BeatmapSet>> beatmapsets)
    {
        foreach (var (id, beatmapset) in beatmapsets)
        {
            var beatmapSetRoot = new JsonObject();
            var files = CreateFilesObject(beatmapset.Files);

            beatmapSetRoot.Add("OnlineID", beatmapset.OnlineID);
            beatmapSetRoot.Add("Files", files);

            var beatmapsetBeatmaps = new JsonArray();
            foreach (var beatmapsetBeatmap in beatmapset.Beatmaps)
            {
                beatmapsetBeatmaps.Add(beatmapsetBeatmap.MD5Hash);
            }

            beatmapSetRoot.Add("Beatmaps", beatmapsetBeatmaps);
            beatmapSetsRoot.Add($"{id}", beatmapSetRoot);
        }
    }

    private static void AddScores(JsonArray scoresRoot, IEnumerable<Score> scores)
    {
        foreach (var score in scores)
        {
            var scoreRoot = new JsonObject();

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
            scoresRoot.Add(scoreRoot);
        }
    }

    private static void AddCollections(JsonArray collectionsRoot, IEnumerable<BeatmapCollection> collections)
    {
        foreach (var collection in collections)
        {
            var collectionRoot = new JsonObject();

            collectionRoot.AddIfNotNull("Name", collection.Name);

            var hashes = new JsonArray();
            foreach (var hash in collection.BeatmapMD5Hashes)
            {
                hashes.Add(hash);
            }

            collectionRoot.AddIfNotNull("BeatmapMD5Hashes", hashes);
            collectionRoot.AddIfNotNull("LastModified", collection.LastModified);
            collectionsRoot.Add(collectionRoot);
        }
    }

    private static void AddSkins(JsonArray skinsRoot, IEnumerable<Skin> skins)
    {
        foreach (var skin in skins)
        {
            var skinRoot = new JsonObject();

            skinRoot.AddIfNotNull("Name", skin.Name);
            skinRoot.AddIfNotNull("Creator", skin.Creator);
            skinRoot.AddIfNotNull("InstantiationInfo", skin.InstantiationInfo);
            skinRoot.AddIfNotNull("Hash", skin.Hash);
            skinRoot.AddIfNotNull("Protected", skin.Protected);
            skinRoot.AddIfNotNull("Files", CreateFilesObject(skin.Files));
            skinRoot.AddIfNotNull("DeletePending", skin.DeletePending);
            skinsRoot.Add(skinRoot);
        }
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