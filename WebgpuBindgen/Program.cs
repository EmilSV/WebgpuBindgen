using System.Reflection;
using System.Text.Json;
using CapiGenerator;
using CapiGenerator.CSModel;
using CapiGenerator.Parser;
using CapiGenerator.Translator;
using CapiGenerator.Writer;
using CppAst;
using WebgpuBindgen;
using WebgpuBindgen.SpecDocRepresentation.Defaults;
using WebgpuBindgen.SpecDocRepresentation.Members;
using WebgpuBindgen.SpecDocRepresentation.Types;

var assembly = Assembly.GetExecutingAssembly();
var resourceName = "WebgpuBindgen.SpecDocRepresentation.index.json";
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

// const string testJson =
// """

//       {
//         "type": "setlike",
//         "idlType": [
//           {
//             "type": "null",
//             "extAttrs": [],
//             "generic": "",
//             "nullable": false,
//             "union": false,
//             "idlType": "DOMString"
//           }
//         ],
//         "arguments": [],
//         "extAttrs": [],
//         "readonly": true,
//         "async": false
//       }

// """;

// var jsonIdl = JsonSerializer.Deserialize<WebidlMemberBase>(testJson, jsonOptions)!;

// Console.WriteLine(jsonIdl);
// return 1;


using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
using (StreamReader reader = new(stream))
{
    try
    {
        var json = await JsonSerializer.DeserializeAsync<Dictionary<string, RootWebidlTypeBase>>(stream, jsonOptions)!;
        var gpuTextureDescriptor = json["GPUTextureDescriptor"] as DictionaryWebidlType;
        var mipLevelCount = gpuTextureDescriptor!.Members.OfType<FieldMember>().First(i => i.Name == "mipLevelCount");
        var defaultValue = mipLevelCount.Default as DefaultNumber;

        Console.WriteLine(defaultValue!.Value);
        return 1;
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"JSON Deserialization error at {ex.Path}: {ex.Message}");
        return 1;
    }
}


string headerFile = Path.GetFullPath(args[0]);
string outputDirectory = Path.GetFullPath(args[1]);

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
    new ConstantParser(),
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



await EnumFixer.FixFlagEnumAttributes(enums, structs, staticClasses);
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
await StructFixer.AddConstructorsToStructs(structs);

var structWriter = new CSStructWriter();
var enumWriter = new CSEnumWriter();


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
