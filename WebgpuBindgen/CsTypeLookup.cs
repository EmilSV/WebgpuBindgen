using CapiGenerator.CSModel;

namespace WebgpuBindgen;

public class CsTypeLookup(
    List<CSStruct> structs,
    List<CSEnum> enums,
    List<CSStaticClass> staticClasses
)
{
    public BaseCSType? FindType(string name)
    {
        return FindType(name, structs) ??
            FindType(name, enums) ??
            FindType(name, staticClasses);
    }

    static BaseCSType? FindType<T>(string name, List<T> types)
        where T : BaseCSType
    {
        var csType = types.FirstOrDefault(i => i.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        if (csType != null)
        {
            return csType;
        }

        var ffiName = $"{name}FFI";

        csType = types.FirstOrDefault(i => i.Name.Equals(ffiName, StringComparison.CurrentCultureIgnoreCase));
        if (csType != null)
        {
            return csType;
        }


        var handleName = $"{name}Handle";

        csType = types.FirstOrDefault(i => i.Name.Equals(handleName, StringComparison.CurrentCultureIgnoreCase));
        if (csType != null)
        {
            return csType;
        }

        return null;
    }

}