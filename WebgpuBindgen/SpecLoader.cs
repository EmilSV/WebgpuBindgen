using System.Text.Json;
using WebgpuBindgen.SpecDocRepresentation;
using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen;


public static class SpecLoader
{
    public static async Task<SpecDocLookup> LoadSpecDocLookup(string jsonFile)
    {
        ArgumentNullException.ThrowIfNull(jsonFile);

        Dictionary<string, RootWebidlTypeBase>? jsonLookup = null;

        if (jsonFile != null)
        {
            using Stream stream = File.OpenRead(jsonFile!);
            using StreamReader reader = new(stream);
            jsonLookup = (await JsonSerializer.DeserializeAsync<Dictionary<string, RootWebidlTypeBase>>(
                stream, JsonOptions.Value
            ))!;
        }

        var specDocLookup = new SpecDocLookup(jsonLookup!);
        specDocLookup.AddTypeNameOverride("Extent3D", "GPUExtent3DDict");
        specDocLookup.AddTypeNameOverride("Origin2D", "GPUOrigin2DDict");
        specDocLookup.AddTypeNameOverride("Origin3D", "GPUOrigin3DDict");
        specDocLookup.AddTypeNameOverride("Color", "GPUColorDict");

        return specDocLookup;
    }

}