using Realms;

// ReSharper disable ReplaceAutoPropertyWithComputedProperty
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace OsuLazerFilesSymlinker;

// I had to make this by hand since realm studio export C# doesn't work
public static class RealmSchema
{
    public static readonly Type[] Types =
    [
        typeof(Beatmap),
        typeof(BeatmapCollection),
        typeof(BeatmapDifficulty),
        typeof(BeatmapMetadata),
        typeof(BeatmapSet),
        typeof(BeatmapUserSettings),
        typeof(File),
        typeof(KeyBinding),
        typeof(ModPreset),
        typeof(RealmNamedFileUsage),
        typeof(RealmUser),
        typeof(Ruleset),
        typeof(RulesetSetting),
        typeof(Score),
        typeof(Skin),
    ];
}

[Preserve(AllMembers = true)]
public class Beatmap : RealmObject
{
    [PrimaryKey] public Guid ID { get; private set; }
    public string DifficultyName { get; private set; } = null!;
    public Ruleset Ruleset { get; private set; } = null!;
    public BeatmapDifficulty Difficulty { get; private set; } = null!;
    public BeatmapMetadata Metadata { get; private set; } = null!;
    public BeatmapUserSettings UserSettings { get; private set; } = null!;
    public BeatmapSet BeatmapSet { get; private set; } = null!;
    public long Status { get; private set; } = 0;
    [Indexed] public long OnlineID { get; private set; }
    public double Length { get; private set; } = 0;
    public double BPM { get; private set; } = 0;
    public string Hash { get; private set; } = null!;
    public double StarRating { get; private set; } = 0;
    [Indexed] public string MD5Hash { get; private set; } = null!;
    public string OnlineMD5Hash { get; private set; } = null!;
    public DateTimeOffset LastLocalUpdate { get; private set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastOnlineUpdate { get; private set; } = DateTimeOffset.MinValue;
    public bool Hidden { get; private set; } = false;
    public long EndTimeObjectCount { get; private set; } = 0;
    public long TotalObjectCount { get; private set; } = 0;
    public DateTimeOffset LastPlayed { get; private set; } = DateTimeOffset.MinValue;
    public long BeatDivisor { get; private set; } = 0;
    public double EditorTimestamp { get; private set; } = 0;
}

[Preserve(AllMembers = true)]
public class BeatmapCollection : RealmObject
{
    [PrimaryKey] public Guid ID { get; private set; }
    public string Name { get; private set; } = null!;
    public IList<string> BeatmapMD5Hashes { get; } = null!;
    public DateTimeOffset LastModified { get; } = DateTimeOffset.MinValue;
}

[Preserve(AllMembers = true)]
public class BeatmapDifficulty : EmbeddedObject
{
    public float DrainRate { get; private set; } = 0;
    public float CircleSize { get; private set; } = 0;
    public float OverallDifficulty { get; private set; } = 0;
    public float ApproachRate { get; private set; } = 0;
    public double SliderMultiplier { get; private set; } = 0;
    public double SliderTickRate { get; private set; } = 0;
}

[Preserve(AllMembers = true)]
public class BeatmapMetadata : RealmObject
{
    public string Title { get; private set; } = null!;
    public string TitleUnicode { get; private set; } = null!;
    public string Artist { get; private set; } = null!;
    public string ArtistUnicode { get; private set; } = null!;
    public RealmUser Author { get; private set; } = null!;
    public string Source { get; private set; } = null!;
    public string Tags { get; private set; } = null!;
    public long PreviewTime { get; private set; } = 0;
    public string AudioFile { get; private set; } = null!;
    public string BackgroundFile { get; private set; } = null!;
    public IList<string> UserTags { get; } = null!;
}

[Preserve(AllMembers = true)]
public class BeatmapSet : RealmObject
{
    [PrimaryKey] public Guid ID { get; private set; }
    [Indexed] public long OnlineID { get; private set; }
    public IList<RealmNamedFileUsage> Files { get; } = null!;
    public IList<Beatmap> Beatmaps { get; } = null!;
}

[Preserve(AllMembers = true)]
public class BeatmapUserSettings : EmbeddedObject
{
    public double Offset { get; private set; } = 0;
}

[Preserve(AllMembers = true)]
public class File : RealmObject
{
    [PrimaryKey] public string Hash { get; private set; } = null!;

    [Ignored] public string Path => System.IO.Path.Join(Hash[..1], Hash[..2], Hash);
}

[Preserve(AllMembers = true)]
public class KeyBinding : RealmObject
{
    [PrimaryKey] public Guid ID { get; private set; }
    public string RulesetName { get; private set; } = null!;
    public long Variant { get; private set; } = 0;
    public long Action { get; private set; } = 0;
    public string KeyCombination { get; private set; } = null!;
}

[Preserve(AllMembers = true)]
public class ModPreset : RealmObject
{
    [PrimaryKey] public Guid ID { get; private set; }
    public Ruleset Ruleset { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Mods { get; private set; } = null!;
    public bool DeletePending { get; private set; } = false;
}

[Preserve(AllMembers = true)]
public class RealmNamedFileUsage : EmbeddedObject
{
    public File File { get; private set; } = null!;
    public string Filename { get; private set; } = null!;
}

[Preserve(AllMembers = true)]
public class RealmUser : RealmObject
{
    [Indexed] public long OnlineID { get; private set; }
    public string Username { get; private set; } = null!;
    public string CountryCOde { get; private set; } = null!;
}

[Preserve(AllMembers = true)]
public class Ruleset : RealmObject
{
    [PrimaryKey] public string ShortName { get; private set; } = null!;
    [Indexed] public long OnlineID { get; private set; }
    public string Name { get; private set; } = null!;
    public string InstantiationInfo { get; private set; } = null!;
    public long LastAppliedDifficultyVersion { get; private set; } = 0;
    public bool Available { get; private set; } = false;
}

[Preserve(AllMembers = true)]
public class RulesetSetting : RealmObject
{
    [Indexed] public string RulesetName { get; private set; } = null!;
    [Indexed] public long Variant { get; private set; } = 0;
    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
}

[Preserve(AllMembers = true)]
public class Score : RealmObject
{
    [PrimaryKey] public Guid ID { get; private set; }
    public Beatmap BeatmapInfo { get; private set; } = null!;
    public string ClientVersion { get; private set; } = null!;
    public string BeatmapHash { get; private set; } = null!;
    public Ruleset Ruleset { get; private set; } = null!;
    public IList<RealmNamedFileUsage> Files { get; } = null!;
    public string Hash { get; private set; } = null!;
    public bool DeletePending { get; private set; } = false;
    public long TotalScore { get; private set; } = 0;
    public long TotalScoreWithoutMods { get; private set; } = 0;
    public long TotalScoreVersion { get; private set; } = 0;
    public long LegacyTotalScore { get; private set; } = 0;
    public bool BackgroundReprocessingFailed { get; private set; } = false;
    public long MaxCombo { get; private set; } = 0;
    public double Accuracy { get; private set; } = 0;
    public DateTimeOffset Date { get; private set; } = DateTimeOffset.MinValue;
    public double PP { get; private set; } = 0;
    [Indexed] public long OnlineID { get; private set; } = 0;
    [Indexed] public long LegacyOnlineID { get; private set; } = 0;
    public RealmUser User { get; private set; } = null!;
    public string Mods { get; private set; } = null!;
    public string Statistics { get; private set; } = null!;
    public string MaximumStatistics { get; private set; } = null!;
    public long Rank { get; private set; } = 0;
    public long Combo { get; private set; } = 0;
    public bool IsLegacyScore { get; private set; } = false;
    public IList<long> Pauses { get; } = null!;
}

[Preserve(AllMembers = true)]
public class Skin : RealmObject
{
    [PrimaryKey] public Guid ID { get; private set; }
    public string Name { get; private set; } = null!;
    public string Creator { get; private set; } = null!;
    public string InstantiationInfo { get; private set; } = null!;
    public string Hash { get; private set; } = null!;
    public bool Protected { get; private set; } = false;
    public IList<RealmNamedFileUsage> Files { get; } = null!;
    public bool DeletePending { get; private set; } = false;
}