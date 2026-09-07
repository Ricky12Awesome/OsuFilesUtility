using System.Collections.Immutable;
// ReSharper disable RedundantCast

namespace OsuFilesUtility;

internal static class BinaryExporter
{
    public static void Export(Api api, BinaryWriter writer)
    {
        var users = api.Realm.All<RealmUser>()
            .AsEnumerable()
            .DistinctBy(item => item.OnlineID) // idky there is duplicate users, so I have to have this
            .ToImmutableList();

        var rulesets = api.Realm.All<Ruleset>().ToImmutableList();
        var beatmaps = api.Realm.All<Beatmap>().ToImmutableList();
        var beatmapsets = api.Realm.All<BeatmapSet>().ToImmutableList();
        var collections = api.Realm.All<BeatmapCollection>().ToImmutableList();
        var scores = api.Realm.All<Score>().ToImmutableList();
        var skins = api.Realm.All<Skin>().ToImmutableList();

        writer.Write((uint)users.Count);
        foreach (var user in users)
        {
            writer.Write((long)user.OnlineID);
            writer.WriteString(user.Username);
            writer.WriteString(user.CountryCode);
        }

        writer.Write((uint)rulesets.Count);
        foreach (var ruleset in rulesets)
        {
            writer.Write((long)ruleset.OnlineID);
            writer.WriteString(ruleset.ShortName);
            writer.WriteString(ruleset.Name);
            writer.WriteString(ruleset.InstantiationInfo);
            writer.Write((long)ruleset.LastAppliedDifficultyVersion);
            writer.Write((bool)ruleset.Available);
        }

        foreach (var beatmap in beatmaps)
        {
            writer.WriteHash(beatmap.MD5Hash);

            // Ruleset
            writer.Write((long)beatmap.Ruleset.OnlineID);

            // Difficulty
            writer.WriteString(beatmap.DifficultyName);
            writer.Write((float)beatmap.Difficulty.DrainRate);
            writer.Write((float)beatmap.Difficulty.CircleSize);
            writer.Write((float)beatmap.Difficulty.OverallDifficulty);
            writer.Write((float)beatmap.Difficulty.ApproachRate);
            writer.Write((double)beatmap.Difficulty.SliderMultiplier);
            writer.Write((double)beatmap.Difficulty.SliderTickRate);

            // Metadata
            writer.WriteString(beatmap.Metadata.Title);
            writer.WriteString(beatmap.Metadata.TitleUnicode);
            writer.WriteString(beatmap.Metadata.Artist);
            writer.WriteString(beatmap.Metadata.ArtistUnicode);
            writer.Write((long)beatmap.Metadata.Author.OnlineID);
            writer.WriteString(beatmap.Metadata.Source);
            writer.WriteString(beatmap.Metadata.Tags);
            writer.Write((long)beatmap.Metadata.PreviewTime);
            writer.WriteString(beatmap.Metadata.AudioFile);
            writer.WriteString(beatmap.Metadata.BackgroundFile);

            writer.Write((uint)beatmap.Metadata.UserTags.Count);
            foreach (var tag in beatmap.Metadata.UserTags)
            {
                writer.WriteString(tag);
            }

            writer.Write((double)beatmap.UserSettings.Offset);

            // Beatmap
            writer.Write((long)beatmap.BeatmapSet.OnlineID);
            writer.Write((long)beatmap.Status);
            writer.Write((long)beatmap.OnlineID);
            writer.Write((double)beatmap.Length);
            writer.Write((double)beatmap.BPM);
            writer.WriteHash(beatmap.Hash);
            writer.Write((double)beatmap.StarRating);
            writer.WriteHash(beatmap.OnlineMD5Hash);
            writer.WriteDateTime(beatmap.LastLocalUpdate);
            writer.WriteDateTime(beatmap.LastOnlineUpdate);
            writer.Write((bool)beatmap.Hidden);
            writer.Write((long)beatmap.EndTimeObjectCount);
            writer.Write((long)beatmap.TotalObjectCount);
            writer.WriteDateTime(beatmap.LastPlayed);
            writer.Write((long)beatmap.BeatDivisor);
            writer.Write((double)(beatmap.EditorTimestamp ?? 0));
        }

        writer.Write((uint)beatmapsets.Count);
        foreach (var beatmapset in beatmapsets)
        {
            writer.Write((long)beatmapset.OnlineID);

            writer.Write((uint)beatmapset.Files.Count);
            foreach (var file in beatmapset.Files)
            {
                writer.WriteString(file.Filename);
                writer.WriteHash(file.File.Hash);
            }

            writer.Write((uint)beatmapset.Beatmaps.Count);
            foreach (var beatmap in beatmapset.Beatmaps)
            {
                writer.Write((long)beatmap.OnlineID);
            }
        }

        writer.Write((uint)scores.Count);
        foreach (var score in scores)
        {
            writer.WriteString(score.BeatmapInfo?.MD5Hash);
            writer.WriteString(score.ClientVersion);
            writer.WriteHash(score.BeatmapHash);
            writer.Write((long)score.Ruleset.OnlineID);

            writer.Write((uint)score.Files.Count);
            foreach (var file in score.Files)
            {
                writer.WriteString(file.Filename);
                writer.WriteHash(file.File.Hash);
            }

            writer.WriteHash(score.Hash);
            writer.Write((bool)score.DeletePending);
            writer.Write((long)score.TotalScore);
            writer.Write((long)score.TotalScoreWithoutMods);
            writer.Write((long)score.TotalScoreVersion);
            writer.Write((long)(score.LegacyTotalScore ?? 0));
            writer.Write((bool)score.BackgroundReprocessingFailed);
            writer.Write((long)score.MaxCombo);
            writer.Write((double)score.Accuracy);
            writer.WriteDateTime(score.Date);
            writer.Write((double)(score.PP ?? 0));
            writer.Write((long)score.OnlineID);
            writer.Write((long)score.LegacyOnlineID);
            writer.Write((long)score.User.OnlineID);
            writer.WriteString(score.Mods);
            writer.WriteString(score.Statistics);
            writer.WriteString(score.MaximumStatistics);
            writer.Write((long)score.Rank);
            writer.Write((long)score.Combo);
            writer.Write((bool)score.IsLegacyScore);
        }

        foreach (var collection in collections)
        {
            writer.WriteString(collection.Name);

            writer.Write((long)collection.BeatmapMD5Hashes.Count);
            foreach (var hash in collection.BeatmapMD5Hashes)
            {
                writer.WriteHash(hash);
            }

            writer.WriteDateTime(collection.LastModified);
        }

        writer.Write((uint)skins.Count);
        foreach (var skin in skins)
        {
            writer.WriteString(skin.Name);
            writer.WriteString(skin.Creator);
            writer.WriteString(skin.InstantiationInfo);
            writer.WriteHash(skin.Hash);
            writer.Write((bool)skin.Protected);

            writer.Write((uint)skin.Files.Count);
            foreach (var file in skin.Files)
            {
                writer.WriteString(file.Filename);
                writer.WriteHash(file.File.Hash);
            }

            writer.Write((bool)skin.DeletePending);
        }
    }
}
