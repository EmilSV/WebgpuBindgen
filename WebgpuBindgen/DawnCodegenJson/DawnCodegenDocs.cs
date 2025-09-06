using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;


public class DawnCodegenDocs
{
    public ImmutableDictionary<string, CodegenItem> Items { get; private init; }

    public static async Task<DawnCodegenDocs> LoadFromFileAsync(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var items = await JsonSerializer.DeserializeAsync<ImmutableDictionary<string, CodegenItem?>>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowOutOfOrderMetadataProperties = true, // This allows metadata properties in any order
                Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
            });
            return new DawnCodegenDocs { Items = items! };
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON Error at line {ex.LineNumber}, position {ex.BytePositionInLine}: {ex.Message}");
            Console.WriteLine($"Path: {ex.Path}");
            throw;
        }
    }
}