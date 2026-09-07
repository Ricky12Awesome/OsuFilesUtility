using System.Text;
using System.Text.Json.Nodes;

namespace OsuFilesUtility;

public static class Extensions
{
    private static string? NullIfEmpty(this string? str)
    {
        return str != string.Empty ? str : null;
    }
    
    public static void AddIfNotNull(this JsonArray obj, JsonNode? child)
    {
        if (child is not null)
        {
            obj.Add(child);
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
