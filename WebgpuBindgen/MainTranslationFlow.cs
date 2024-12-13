using CapiGenerator;
using CapiGenerator.CSModel;
using CapiGenerator.Parser;
using CapiGenerator.Translator;
using CppAst;
using WebgpuBindgen.SpecDocRepresentation;

namespace WebgpuBindgen;


public static class MainTranslationFlow
{
    public static async Task<TranslationResult> Translate(
        string headerFilePath, SpecDocLookup specDocLookup)
    {
        string headerPath = FakeCStdHeader.CreateFakeStdHeaderFolder();

        var options = new CppParserOptions
        {
            ParseMacros = true,
        };

        options.Defines.Add("WGPU_SKIP_PROCS");

        options.IncludeFolders.Add(headerPath);

        var cppCompilation = CppParser.ParseFile(headerFilePath, options);

        if (cppCompilation.HasErrors)
        {
            Console.WriteLine("Errors occurred while parsing the header file.");

            foreach (var error in cppCompilation.Diagnostics.Messages)
            {
                Console.WriteLine(error);
            }

            throw new Exception("Errors occurred while parsing the header file.");
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

        return new TranslationResult
        {
            StaticClasses = staticClasses,
            Enums = enums,
            Structs = structs
        };
    }

}