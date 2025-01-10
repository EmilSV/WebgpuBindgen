using CapiGenerator.Writer;
using WebgpuBindgen;

string headerFile = Path.GetFullPath(args[0]);
string headerRefFile = Path.GetFullPath(args[1]);
string outputDirectory = Path.GetFullPath(args[2]);
string? jsonFile = args.Length > 2 ? Path.GetFullPath(args[3]) : null;
XmlCommentDocs? doc = args.Length > 3 ? await XmlCommentDocs.Create(Path.GetFullPath(args[3])) : null;

var specDocLookup = await SpecLoader.LoadSpecDocLookup(jsonFile);
var translationResult = await MainTranslationFlow.Translate(headerFile, specDocLookup);
var translationResultRef = await MainTranslationFlow.Translate(headerRefFile, specDocLookup);

RemoveNonRefHandler.RemoveNonRef(translationResult, translationResultRef);

var enums = translationResult.Enums;
var staticClasses = translationResult.StaticClasses;
var structs = translationResult.Structs;

try
{
    Directory.Delete(outputDirectory, true);
}
catch
{ }

foreach (var csEnum in enums)
{
    csEnum.Namespace ??= "WebGpuSharp";
}

foreach (var csStaticClass in staticClasses)
{
    csStaticClass.Namespace ??= "WebGpuSharp";
}

foreach (var csStruct in structs)
{
    csStruct.Namespace ??= "WebGpuSharp";
}

foreach (var csEnum in enums)
{
    doc?.AssignComment(csEnum);

    var enumWriter = new CSEnumWriter();
    await enumWriter.Write(csEnum, new CSWriteConfig
    {
        OutputDirectory = outputDirectory,
        Usings = [
            "System",
            "System.Runtime.InteropServices"
        ]
    });
}

foreach (var csStaticClass in staticClasses)
{
    doc?.AssignComment(csStaticClass);

    var staticClassWriter = new CSStaticClassWriter();

    await staticClassWriter.Write(csStaticClass, new CSWriteConfig
    {
        OutputDirectory = outputDirectory,
        Usings = [
            "System",
            "System.Runtime.InteropServices"
        ]
    });
}

foreach (var csStruct in structs)
{
    doc?.AssignComment(csStruct);

    var structWriter = new CSStructWriter();
    await structWriter.Write(csStruct, new CSWriteConfig
    {
        OutputDirectory = outputDirectory,
        Usings = [
            "System",
            "System.Runtime.InteropServices"
        ]
    });
}

return 0;
