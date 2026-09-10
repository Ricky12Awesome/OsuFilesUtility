using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OsuFilesUtility;

public static class Extensions
{
    private static string? NullIfEmpty(this string? str)
    {
        return str != string.Empty ? str : null;
    }

    public static string SubcommandHelpValues(this Command command)
    {
        var options = string.Join(", ", command.Subcommands.Select(c => c.Name));

        return $" [values: {options}]";
    }

    extension(Utf8JsonWriter writer)
    {
        public void WriteIfNotNull(string name, string? value)
        {
            if (value is null)
            {
                return;
            }

            writer.WritePropertyName(name);
            writer.WriteStringValue(value.Length == 0 ? null : value);
        }

        public void WriteIfNotNull(string name, long? value)
        {
            if (value is not null)
            {
                writer.WriteNumber(name, value.Value);
            }
        }

        public void WriteIfNotNull(string name, double? value)
        {
            if (value is not null)
            {
                writer.WriteNumber(name, value.Value);
            }
        }

        public void WriteIfNotNull(string name, DateTimeOffset? value)
        {
            if (value is not null)
            {
                writer.WriteDateTime(name, value.Value);
            }
        }

        public void WriteDateTime(string name, DateTimeOffset value)
        {
            writer.WritePropertyName(name);
            writer.WriteStringValue(value);
        }

        public void FlushLine(Stream output)
        {
            writer.Flush();
            output.WriteByte((byte)'\n');
            output.Flush();
            writer.Reset(output);
        }

        public void WriteFiles(IEnumerable<RealmNamedFileUsage> files)
        {
            writer.WriteStartObject();
            foreach (var file in files)
            {
                writer.WritePropertyName(file.Filename);
                writer.WriteStringValue(file.File.Hash);
            }

            writer.WriteEndObject();
        }

        public void WriteObjectSection<T>(
            string name,
            IEnumerable<T> items,
            bool removeEmpty,
            Action<Utf8JsonWriter, T> writeItem)
        {
            using var enumerator = items.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                if (removeEmpty)
                {
                    return;
                }

                writer.WritePropertyName(name);
                writer.WriteStartObject();
                writer.WriteEndObject();
                return;
            }

            writer.WritePropertyName(name);
            writer.WriteStartObject();

            do
            {
                writeItem(writer, enumerator.Current);
            } while (enumerator.MoveNext());

            writer.WriteEndObject();
        }

        public void WriteArraySection<T>(
            string name,
            IEnumerable<T> items,
            bool removeEmpty,
            Action<Utf8JsonWriter, T> writeItem)
        {
            using var enumerator = items.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                if (removeEmpty)
                {
                    return;
                }

                writer.WritePropertyName(name);
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }

            writer.WritePropertyName(name);
            writer.WriteStartArray();

            do
            {
                writeItem(writer, enumerator.Current);
            } while (enumerator.MoveNext());

            writer.WriteEndArray();
        }
    }

    extension(JsonObject obj)
    {
        public void AddIfNotNull(string name, string? child)
        {
            if (child is not null)
            {
                obj.Add(name, child.NullIfEmpty());
            }
        }

        public void AddIfNotNull(string name, JsonNode? child)
        {
            if (child is not null)
            {
                obj.Add(name, child);
            }
        }
    }

    extension(BinaryWriter writer)
    {
        public void WriteString(string? value)
        {
            if (value is null)
            {
                writer.Write((ushort)0);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);

            if (bytes.Length >= ushort.MaxValue)
            {
                throw new OverflowException("String too long");
            }

            writer.Write((ushort)bytes.Length);

            writer.Write(bytes);
        }

        public void WriteHash(string value)
        {
            var bytes = Convert.FromHexString(value);
            writer.Write(bytes);
        }

        public void WriteDateTime(DateTimeOffset? time)
        {
            writer.Write(time?.ToUnixTimeMilliseconds() ?? 0);
        }
    }
}
