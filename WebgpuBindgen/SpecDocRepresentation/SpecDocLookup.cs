using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen.SpecDocRepresentation;


public class SpecDocLookup
{
    private readonly Dictionary<string, RootWebidlTypeBase> _jsonLookup;
    private readonly Dictionary<string, string> _typeNameOverrides = new();

    public SpecDocLookup(Dictionary<string, RootWebidlTypeBase> jsonLookup)
    {
        _jsonLookup = jsonLookup;
    }

    public void AddTypeNameOverride(string typeName, string newTypeName)
    {
        _typeNameOverrides.Add(typeName, newTypeName);
    }

    public RootWebidlTypeBase? GetStructLikeType(string typeName)
    {
        RootWebidlTypeBase? type;
        if (_typeNameOverrides.TryGetValue(typeName, out var newTypeName))
        {
            return _jsonLookup.TryGetValue(newTypeName, out type) ? type : null;
        }
        else
        {
            typeName = "GPU" + typeName;
            if (typeName.EndsWith("Flags"))
            {
                typeName = typeName[0..^"Flags".Length];
            }
            if (typeName.EndsWith("Handle"))
            {
                typeName = typeName[0..^"Handle".Length];
            }
            if (typeName.EndsWith("FFI"))
            {
                typeName = typeName[0..^"FFI".Length];
            }
            return _jsonLookup.TryGetValue(typeName, out type) ? type : null;
        }
    }
}