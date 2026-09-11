// using System.Buffers;
// using System.Collections.Immutable;
// using System.Text.Json;

namespace OsuFilesUtility;

internal static class Program
{
    public static int Main(string[] args)
    {
        // var api = new Api(null, false);
        // // var processingCount = Environment.ProcessorCount ;
        // var processingCount = 7;
        //
        // var users = api.NewRealmInstance().Freeze().All<RealmUser>();
        // var beatmaps = api.NewRealmInstance().Freeze().All<Beatmap>();
        // var rulesets = api.NewRealmInstance().Freeze().All<Ruleset>();
        // var beatmapSets = api.NewRealmInstance().Freeze().All<BeatmapSet>();
        // var collections = api.NewRealmInstance().Freeze().All<BeatmapCollection>();
        // var scores = api.NewRealmInstance().Freeze().All<Score>();
        // var skins = api.NewRealmInstance().Freeze().All<Skin>();
        //
        // var all = users
        //     .ToImmutableList()
        //     .Concat<object>([.. rulesets])
        //     .Concat<object>([.. beatmaps])
        //     .Concat<object>([.. beatmapSets])
        //     .Concat<object>([.. collections])
        //     .Concat<object>([.. scores])
        //     .Concat<object>([.. skins]);
        //
        // var chunks = all.Chunk(all.Count() / processingCount);
        //
        // var options = new ParallelOptions
        // {
        //     MaxDegreeOfParallelism = processingCount
        // };
        //
        // var exporter = new JsonExporter(new JsonExporter.ExportSettings(IsStream: true), null);
        // var opts = new JsonWriterOptions { Indented = false, SkipValidation = true };
        // var buffer = new ArrayBufferWriter<byte>(1024 * 1024 * 64);
        //
        // Parallel.ForEach(chunks, options, chunk =>
        // {
        //     var writer = new Utf8JsonWriter(buffer, opts);
        //
        //     foreach (var obj in chunk)
        //     {
        //         switch (obj)
        //         {
        //             case RealmUser user:
        //                 exporter.WriteUser(writer, user);
        //                 break;
        //             case Ruleset ruleset:
        //                 exporter.WriteRuleset(writer, ruleset);
        //                 break;
        //             case Beatmap map:
        //                 exporter.WriteBeatmap(writer, map);
        //                 break;
        //             case BeatmapSet set:
        //                 exporter.WriteBeatmapSet(writer, set);
        //                 break;
        //             case BeatmapCollection collection:
        //                 exporter.WriteCollection(writer, collection);
        //                 break;
        //             case Score score:
        //                 exporter.WriteScore(writer, score);
        //                 break;
        //             case Skin skin:
        //                 exporter.WriteSkin(writer, skin);
        //                 break;
        //         }
        //     }
        // });
        //
        // return 0;

        return CommandLine.Run(args);
    }
}