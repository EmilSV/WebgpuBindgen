using CapiGenerator;
using CapiGenerator.CSModel;
using CapiGenerator.Parser;
using CapiGenerator.Translator;
using CapiGenerator.UtilTypes;
using CapiGenerator.Writer;
using CppAst;
using WebgpuBindgen;


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