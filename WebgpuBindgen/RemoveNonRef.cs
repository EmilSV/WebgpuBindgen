using System.Security.Cryptography.X509Certificates;
using CapiGenerator.CSModel;
using ClangSharp;

namespace WebgpuBindgen;


public static class RemoveNonRefHandler
{
    public static void DeepFindStructs(ICSType? typeInstance, HashSet<CSStruct> structs, HashSet<CSEnum> enums, HashSet<CSStaticClass> staticClasses)
    {
        if (typeInstance == null)
        {
            return;
        }

        if (typeInstance is CSStruct structValue)
        {
            DeepFindStructs(structValue, structs, enums, staticClasses);
        }
        else if (typeInstance is CSEnum enumValue)
        {
            enums.Add(enumValue);
        }
        else if (typeInstance is CSStaticClass staticValue)
        {
            DeepFindStructs(staticValue, structs, enums, staticClasses);
        }
        else if (typeInstance is CSUnmanagedFunctionType unmanagedFunctionValue)
        {
            DeepFindStructs(unmanagedFunctionValue, structs, enums, staticClasses);
        }
    }

    public static void DeepFindStructs(CSStruct structType, HashSet<CSStruct> structs, HashSet<CSEnum> enums, HashSet<CSStaticClass> staticClasses)
    {
        if (!structs.Add(structType))
        {
            return;
        }

        foreach (var field in structType.Fields)
        {
            DeepFindStructs(field.Type.Type, structs, enums, staticClasses);
        }

        foreach (var method in structType.Methods)
        {
            foreach (var param in method.Parameters)
            {
                DeepFindStructs(param.Type.Type, structs, enums, staticClasses);
            }
            DeepFindStructs(method.ReturnType.Type, structs, enums, staticClasses);
        }
    }

    public static void DeepFindStructs(CSStaticClass staticClass, HashSet<CSStruct> structs, HashSet<CSEnum> enums, HashSet<CSStaticClass> staticClasses)
    {
        if (!staticClasses.Add(staticClass))
        {
            return;
        }

        foreach (var field in staticClass.Fields)
        {
            DeepFindStructs(field.Type.Type, structs, enums, staticClasses);
        }

        foreach (var method in staticClass.Methods)
        {
            foreach (var param in method.Parameters)
            {
                DeepFindStructs(param.Type.Type, structs, enums, staticClasses);
            }
            DeepFindStructs(method.ReturnType.Type, structs, enums, staticClasses);
        }
    }

    public static void DeepFindStructs(CSUnmanagedFunctionType unmanagedFunction, HashSet<CSStruct> structs, HashSet<CSEnum> enums, HashSet<CSStaticClass> staticClasses)
    {
        foreach (var typeInstance in unmanagedFunction.ParameterTypes)
        {
            if (typeInstance.Type is CSStruct structValue)
            {
                DeepFindStructs(structValue, structs, enums, staticClasses);
            }
            else if (typeInstance.Type is CSEnum enumValue)
            {
                enums.Add(enumValue);
            }
            else if (typeInstance.Type is CSStaticClass staticValue)
            {
                DeepFindStructs(staticValue, structs, enums, staticClasses);
            }
            else if (typeInstance.Type is CSUnmanagedFunctionType unmanagedFunctionValue)
            {
                DeepFindStructs(unmanagedFunctionValue, structs, enums, staticClasses);
            }
        }

        {
            var returnType = unmanagedFunction.ReturnType.Type;
            if (returnType is CSStruct structValue)
            {
                DeepFindStructs(structValue, structs, enums, staticClasses);
            }
            else if (returnType is CSEnum enumValue)
            {
                enums.Add(enumValue);
            }
            else if (returnType is CSStaticClass staticValue)
            {
                DeepFindStructs(staticValue, structs, enums, staticClasses);
            }
            else if (returnType is CSUnmanagedFunctionType unmanagedFunctionValue)
            {
                DeepFindStructs(unmanagedFunctionValue, structs, enums, staticClasses);
            }
        }
    }

    private static bool CompareTypeNames(string? typeNameA, string? typeNameB)
    {
        if (ReferenceEquals(typeNameA, typeNameB))
        {
            return true;
        }

        if(typeNameA == null || typeNameB == null)
        {
            return false;
        }

        static ReadOnlySpan<char> RightTrimNumbers(ReadOnlySpan<char> span)
        {
            var index = span.Length - 1;
            while (index >= 0 && char.IsDigit(span[index]))
            {
                index--;
            }

            return span[..(index + 1)];
        }

        var typeNameASpan = RightTrimNumbers(typeNameA);
        var typeNameBSpan = RightTrimNumbers(typeNameB);

        return typeNameASpan.SequenceEqual(typeNameBSpan);
    }



    public static void RemoveNonRef(
        TranslationResult translationResult,
        TranslationResult translationResultRef)
    {
        List<CSMethod> methodsRemoved = [];

        var staticClassStartingPoint = translationResult.StaticClasses
            .Where(i => translationResultRef.StaticClasses.Any(j => CompareTypeNames(i.Name, j.Name)))
            .ToList();

        var structStartingPoint = translationResult.Structs
            .Where(i => translationResultRef.Structs.Any(j => CompareTypeNames(i.Name, j.Name)))
            .ToList();

        var enumStartingPoint = translationResult.Enums
            .Where(i => translationResultRef.Enums.Any(j => CompareTypeNames(i.Name, j.Name)))
            .ToList();

        foreach (var staticClass in staticClassStartingPoint)
        {
            var refStaticClass = translationResultRef.StaticClasses.FirstOrDefault(i => i.Name == staticClass.Name);
            if (refStaticClass == null)
            {
                continue;
            }

            staticClass.Fields.RemoveWhere(i => !refStaticClass.Fields.Any(j => CompareTypeNames(j.Name, i.Name)));
            staticClass.Methods.RemoveWhere(i => !refStaticClass.Methods.Any(j => CompareTypeNames(j.Name, i.Name)));
        }

        foreach (var structType in structStartingPoint)
        {
            var refStructType = translationResultRef.Structs.FirstOrDefault(i => i.Name == structType.Name);
            if (refStructType == null)
            {
                continue;
            }

            structType.Methods.RemoveWhere(i => !refStructType.Methods.Any(j => CompareTypeNames(j.Name, i.Name)));
        }



        var deepFindStructsHashSet = new HashSet<CSStruct>();
        var deepFindEnumsHashSet = new HashSet<CSEnum>();
        var deepFindStaticClassHashSet = new HashSet<CSStaticClass>();

        foreach (var staticClass in staticClassStartingPoint)
        {
            DeepFindStructs(staticClass, deepFindStructsHashSet, deepFindEnumsHashSet, deepFindStaticClassHashSet);
        }

        foreach (var structType in structStartingPoint)
        {
            DeepFindStructs(structType, deepFindStructsHashSet, deepFindEnumsHashSet, deepFindStaticClassHashSet);
        }

        translationResult.StaticClasses.RemoveAll(i => !deepFindStaticClassHashSet.Contains(i));
        translationResult.Structs.RemoveAll(i => !deepFindStructsHashSet.Contains(i));
        translationResult.Enums.RemoveAll(i => !deepFindEnumsHashSet.Contains(i));
    }
}