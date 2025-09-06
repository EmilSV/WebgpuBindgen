using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;

public class ExtensibleConverter : JsonConverter<Extensible?>
{
    public override Extensible? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return Extensible.True;
            case JsonTokenType.False:
                return Extensible.False;
            case JsonTokenType.String:
                var stringValue = reader.GetString()?.ToLowerInvariant();
                return stringValue switch
                {
                    "in" => Extensible.In,
                    "out" => Extensible.Out,
                    "true" => Extensible.True,
                    "false" => Extensible.False,
                    _ => throw new JsonException($"Unable to convert \"{stringValue}\" to Extensible enum.")
                };
            case JsonTokenType.Null:
                return null;
            default:
                throw new JsonException($"Unable to convert token type {reader.TokenType} to Extensible enum.");
        }
    }

    public override void Write(Utf8JsonWriter writer, Extensible? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case Extensible.True:
                writer.WriteBooleanValue(true);
                break;
            case Extensible.False:
                writer.WriteBooleanValue(false);
                break;
            case Extensible.In:
                writer.WriteStringValue("in");
                break;
            case Extensible.Out:
                writer.WriteStringValue("out");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }
}