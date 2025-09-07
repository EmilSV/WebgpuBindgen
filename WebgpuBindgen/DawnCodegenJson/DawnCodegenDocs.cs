using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;

public class DawnCodegenDocs
{
    private class FilteringDictionaryConverter : JsonConverter<ImmutableDictionary<string, CodegenItem>>
    {
        public override ImmutableDictionary<string, CodegenItem> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            var items = ImmutableDictionary.CreateBuilder<string, CodegenItem>();

            // Create options without this converter to avoid infinite recursion
            var innerOptions = new JsonSerializerOptions(options);
            innerOptions.Converters.Remove(innerOptions.Converters.OfType<FilteringDictionaryConverter>().First());

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName token");

                string propertyName = reader.GetString()!;

                // Skip metadata properties that start with underscore
                if (propertyName.StartsWith("_"))
                {
                    reader.Read(); // Move to the value
                    reader.Skip(); // Skip the entire value
                    continue;
                }

                reader.Read(); // Move to the value

                try
                {
                    var item = JsonSerializer.Deserialize<CodegenItem>(ref reader, innerOptions);
                    if (item != null)
                    {
                        items.Add(propertyName, item);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Warning: Failed to deserialize item '{propertyName}': {ex.Message}");
                    reader.Skip(); // Skip this value and continue
                }
            }

            return items.ToImmutable();
        }

        public override void Write(Utf8JsonWriter writer, ImmutableDictionary<string, CodegenItem> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            var innerOptions = new JsonSerializerOptions(options);
            innerOptions.Converters.Remove(innerOptions.Converters.OfType<FilteringDictionaryConverter>().First());

            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value, innerOptions);
            }

            writer.WriteEndObject();
        }
    }

    public readonly ImmutableDictionary<string, CodegenItem> Items;

    public DawnCodegenDocs(ImmutableDictionary<string, CodegenItem> items)
    {
        Items = items;
    }


    public static async Task<DawnCodegenDocs> LoadFromFileAsync(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var items = await JsonSerializer.DeserializeAsync<ImmutableDictionary<string, CodegenItem>>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowOutOfOrderMetadataProperties = true,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                    new FilteringDictionaryConverter()
                }
            });
            return new DawnCodegenDocs(items ?? throw new JsonException("Failed to deserialize DawnCodegenDocs from JSON file."));
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON Error at line {ex.LineNumber}, position {ex.BytePositionInLine}: {ex.Message}");
            Console.WriteLine($"Path: {ex.Path}");
            throw;
        }
    }
}

