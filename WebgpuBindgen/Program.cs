using CapiGenerator;
using CapiGenerator.CSModel;
using CapiGenerator.Parser;
using CapiGenerator.Translator;
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
    new CSConstAndFunctionTranslator("WebGPU", "webgpu_dawn"),
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

var structWriter = new CSStructWriter();
var enumWriter = new CSEnumWriter();



foreach (var csEnum in enums)
{
    csEnum.Namespace = "WebGpuSharp";
}

foreach (var csStaticClass in staticClasses)
{
    csStaticClass.Namespace = "WebGpuSharp";
}

foreach (var csStruct in structs)
{
    csStruct.Namespace = "WebGpuSharp";
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