using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;


public class NumberOrStringConverter : JsonConverter<NumberOrString>
{
    public override NumberOrString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return NumberOrString.Null;
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int intValue))
                {
                    return new NumberOrString(intValue);
                }
                else if (reader.TryGetDouble(out double doubleValue))
                {
                    return new NumberOrString(doubleValue);
                }
                throw new JsonException("Number value is neither int nor double.");
            case JsonTokenType.String:
                return new NumberOrString(reader.GetString() ?? throw new JsonException("String value is null."));
            default:
                throw new JsonException($"Unable to convert token type {reader.TokenType} to NumberOrString.");
        }
    }

    public override void Write(Utf8JsonWriter writer, NumberOrString value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case { } when value.TryGetValue(out int intValue):
                writer.WriteNumberValue(intValue);
                break;
            case { } when value.TryGetValue(out double doubleValue):
                writer.WriteNumberValue(doubleValue);
                break;
            case { } when value.TryGetValue(out string str):
                writer.WriteStringValue(str);
                break;
            case { } when value.GetValueType() == NumberOrString.Type.Null:
                writer.WriteNullValue();
                break;
        }
    }
}