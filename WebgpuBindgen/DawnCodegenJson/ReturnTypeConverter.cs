using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;


public class ReturnTypeConverter : JsonConverter<ReturnType>
{
    public override ReturnType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return new ReturnType(null, null);
            case JsonTokenType.String:
                return new ReturnType(reader.GetString() ?? throw new JsonException("String value is null."), null);
            case JsonTokenType.StartObject:
                {
                    string? type = null;
                    bool? optional = null;

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string propertyName = reader.GetString() ?? throw new JsonException("Property name is null.");
                            reader.Read(); // Move to the value

                            switch (propertyName)
                            {
                                case "type":
                                    type = reader.GetString() ?? throw new JsonException("Type value is null.");
                                    break;
                                case "optional":
                                    optional = reader.GetBoolean();
                                    break;
                                default:
                                    throw new JsonException($"Unexpected property: {propertyName}");
                            }
                        }
                    }

                    if (type == null)
                    {
                        throw new JsonException("Missing required property 'type' in ReturnType object.");
                    }

                    return new ReturnType(type, optional);
                }
            default:
                throw new JsonException($"Unable to convert token type {reader.TokenType} to ReturnType.");
        }
    }

    public override void Write(Utf8JsonWriter writer, ReturnType value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case { Type: not null, Optional: null }:
                writer.WriteStringValue(value.Type);
                break;
            case { Type: not null, Optional: not null }:
                writer.WriteStartObject();
                writer.WriteString("type", value.Type);
                writer.WriteBoolean("optional", value.Optional.Value);
                writer.WriteEndObject();
                break;
            case { Type: null, Optional: null }:
                writer.WriteNullValue();
                break;
            default:
                break;
        }
    }
}