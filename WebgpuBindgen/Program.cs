using CapiGenerator;
using CapiGenerator.Parser;
using CapiGenerator.Translator;
using CapiGenerator.Writer;
using CppAst;


string headerFile = Path.Combine(Directory.GetCurrentDirectory(), args[0]);

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
    new CSConstTranslator("WebGPU"),
    new CSEnumTranslator(),
    new CSFunctionTranslator("WebGPU", "webgpu_dawn"),
    new CSStructTranslator(),
    new CSTypedefTranslator()
]);

translationUnit.Translate([compilationUnit]);

var structWriter = new CSStructWriter();
var enumWriter = new CSEnumWriter();

foreach (var csStruct in translationUnit.GetCSStructEnumerable())
{
    csStruct.Namespace = "TestProject";

    await structWriter.Write(csStruct, new CSWriteConfig
    {
        OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output"),
        Usings = [
            "System"
        ]
    });
}

foreach (var csEnum in translationUnit.GetCSEnumEnumerable())
{
    csEnum.Namespace = "TestProject";

    await enumWriter.Write(csEnum, new CSWriteConfig
    {
        OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output"),
        Usings = [
            "System"
        ]
    });
}

foreach (var csStaticClass in translationUnit.GetCSStaticClassesEnumerable())
{
    csStaticClass.Namespace = "TestProject";

    var staticClassWriter = new CSStaticClassWriter();

    await staticClassWriter.Write(csStaticClass, new CSWriteConfig
    {
        OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output"),
        Usings = [
            "System"
        ]
    });
}

return 0;