using CapiGenerator.CSModel;

namespace WebgpuBindgen;


public class TranslationResult
{
    public List<CSStaticClass> StaticClasses { get; set; } = [];
    public List<CSEnum> Enums { get; set; } = [];
    public List<CSStruct> Structs { get; set; } = [];
}