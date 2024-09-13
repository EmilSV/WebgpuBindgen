using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Types;

public class IdlType
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public required string Generic { get; set; }
    public required bool Nullable { get; set; }
    public required bool Union { get; set; }
    [JsonPropertyName("idlType")]
    [JsonConverter(typeof(IdlTypeConverter))]
    public required object IdlTypeFelid { get; set; }

    public bool TryGetIdlTypeFelidAsList([NotNullWhen(true)] out List<IdlType>? list)
    {
        list = IdlTypeFelid as List<IdlType>;
        return list != null;
    }


    public bool TryGetIdlTypeFelidAsString([NotNullWhen(true)] out string? str)
    {
        str = IdlTypeFelid as string;
        return str != null;
    }
}

public class IdlTypeConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<IdlType>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }
                list.Add(JsonSerializer.Deserialize<IdlType>(ref reader, options)!);
            }
            return list;
        }
        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is string v)
        {
            writer.WriteStringValue(v);
        }
        else if (value is List<IdlType> list)
        {
            writer.WriteStartArray();
            foreach (var item in list)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
        else
        {
            throw new JsonException("Unexpected value type: " + value.GetType());
        }
    }
}
