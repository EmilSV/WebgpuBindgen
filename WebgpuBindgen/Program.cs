using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using CapiGenerator;
using CapiGenerator.CSModel;
using CapiGenerator.Parser;
using CapiGenerator.Translator;
using CapiGenerator.Writer;
using CppAst;
using WebgpuBindgen;
using WebgpuBindgen.SpecDocRepresentation;
using WebgpuBindgen.SpecDocRepresentation.Comments;
using WebgpuBindgen.SpecDocRepresentation.Types;

string headerFile = Path.GetFullPath(args[0]);
string outputDirectory = Path.GetFullPath(args[1]);
string? jsonFile = args.Length > 2 ? Path.GetFullPath(args[2]) : null;

if (jsonFile != null && !File.Exists(jsonFile))
{
    Console.WriteLine($"JSON file {jsonFile} does not exist.");
    return 1;
}

Dictionary<string, RootWebidlTypeBase>? jsonLookup = null;

if (jsonFile != null)
{
    using Stream stream = File.OpenRead(jsonFile!);
    using StreamReader reader = new(stream);
    try
    {
        jsonLookup = await JsonSerializer.DeserializeAsync<Dictionary<string, RootWebidlTypeBase>>(
            stream, JsonOptions.Value
        )!;
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"JSON Deserialization error at {ex.Path}: {ex.Message}");
        return 1;
    }
}


if (!File.Exists(headerFile))
{
    Console.WriteLine($"Header file {headerFile} does not exist.");
    return 1;
}

string headerPath = FakeCStdHeader.CreateFakeStdHeaderFolder();

var options = new CppParserOptions
{
    ParseMacros = true,
};

options.Defines.Add("WGPU_SKIP_PROCS");

options.IncludeFolders.Add(headerPath);

var cppCompilation = CppParser.ParseFile(headerFile, options);


if (cppCompilation.HasErrors)
{
    Console.WriteLine("Errors occurred while parsing the header file.");

    foreach (var error in cppCompilation.Diagnostics.Messages)
    {
        Console.WriteLine(error);
    }

    return 1;
}

var compilationUnit = new CCompilationUnit();

compilationUnit.AddParser([
    new WebgpuBindgenConstantParser(),
    new EnumParser(),
    new FunctionParser(),
    new StructParser(),
    new TypedefParser()
]);


compilationUnit.Parse([cppCompilation]);

foreach (var constant in compilationUnit.GetEnumEnumerable())
{
    Console.WriteLine(constant.Name);
}


var translationUnit = new CSTranslationUnit();

translationUnit.AddTranslator([
    new CSEnumTranslator(),
    new CSConstAndFunctionTranslator("WebGPU_FFI", "webgpu_dawn"),
    new CSStructTranslator(),
    new CSTypedefTranslator()
]);

translationUnit.Translate([compilationUnit]);

List<CSStaticClass> staticClasses = new();
List<CSEnum> enums = new();
List<CSStruct> structs = new();

staticClasses.AddRange(translationUnit.GetCSStaticClassesEnumerable());
enums.AddRange(translationUnit.GetCSEnumEnumerable());
structs.AddRange(translationUnit.GetCSStructEnumerable());

SpecDocLookup? specDocLookup = null;
if (jsonLookup != null)
{
    specDocLookup = new SpecDocLookup(jsonLookup);
    specDocLookup.AddTypeNameOverride("Extent3D", "GPUExtent3DDict");
    specDocLookup.AddTypeNameOverride("Origin2D", "GPUOrigin2DDict");
    specDocLookup.AddTypeNameOverride("Origin3D", "GPUOrigin3DDict");
    specDocLookup.AddTypeNameOverride("Color", "GPUColorDict");
}




await EnumFixer.FixFlagEnums(enums, structs, staticClasses);
await EnumFixer.FixEnums(enums);

await StructFixer.FixStructsName(structs);
await StructFixer.UnwrapCallbacks(structs, staticClasses, enums);
await StructFixer.CreateHandleTypes(structs, staticClasses, enums);
await StructFixer.AddStructModifiers(structs);
await StructFixer.RemoveStructs(structs);

await StaticClassFixer.RemoveMembers(staticClasses);
await StaticClassFixer.FixMemberNames(staticClasses);
await StaticClassFixer.MakeStaticClassesPartial(staticClasses);
await StructFixer.FFIRenameStructs(structs);
//await StructFixer.RemoveUsedTypes(structs, staticClasses, enums);
await StructFixer.FixWebgpuBoolType(structs);
await StructFixer.FieldNameFix(structs);
await StructFixer.AddEmptyConstructorsToStructs(structs);
await AddHandlerMethods.AddMethods(structs, staticClasses.First(i => i.Name == "WebGPU_FFI"));
//await StructFixer.AddConstructorsToStructs(structs);

if (specDocLookup != null)
{
    await DefaultFixer.FixDefaults(structs, specDocLookup);
}
await StringViewFixer.FixStringViewClassMembers(structs, staticClasses);
await StructFixer.AddDefaultValueFromStructFelids(structs);

if (specDocLookup != null)
{
    var csTypeLookup = new CsTypeLookup(structs, enums, staticClasses);
    var commentConvert = new CommentConvert(csTypeLookup);
    var commentAssigner = new CommentAssigner(csTypeLookup, commentConvert);

    commentAssigner.AssignComment(specDocLookup);
}
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
