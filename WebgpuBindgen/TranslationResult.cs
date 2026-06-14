using CapiGenerator.CSModel;
using CapiGenerator.XmlComments;

namespace WebgpuBindgen;

public class TranslationResult : IXmlCommentsTypeProvider
{
    public List<CSStaticClass> StaticClasses { get; set; } = [];
    public List<CSEnum> Enums { get; set; } = [];
    public List<CSStruct> Structs { get; set; } = [];

    IEnumerable<CSEnum> IXmlCommentsTypeProvider.GetCSEnumsEnumerable()
    {
        return Enums;
    }

    IEnumerable<CSStaticClass> IXmlCommentsTypeProvider.GetCSStaticClassesEnumerable()
    {
        return StaticClasses;
    }

    IEnumerable<CSStruct> IXmlCommentsTypeProvider.GetCSStructsEnumerable()
    {
        return Structs;
    }

}
