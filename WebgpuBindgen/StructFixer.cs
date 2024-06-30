using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using CapiGenerator.CModel;
using CapiGenerator.CSModel;
using static CapiGenerator.CSModel.CSClassMemberModifierConsts;

namespace WebgpuBindgen;

public partial class StructFixerRegex
{
    [GeneratedRegex("callback[0-9]*", RegexOptions.IgnoreCase, "en-US")]
    public static partial Regex GetCallbackRegex();
}

public static class StructFixer
{
    public static Task RemoveUsedTypes(List<CSStruct> structs, List<CSStaticClass> staticClasses, List<CSEnum> enums)
    {
        HashSet<CSStruct> structToRemove = new(structs);
        HashSet<CSEnum> enumToRemove = new(enums);

        List<CSMethod> allMethods = [];
        foreach (var item in staticClasses)
        {
            foreach (var method in item.Methods)
            {
                allMethods.Add(method);
            }
        }

        void SearchForTypes(ICSType? type)
        {
            if (type is null)
            {
                return;
            }

            switch (type)
            {
                case CSEnum enumType:
                    enumToRemove.Remove(enumType);
                    break;
                case CSStruct structType:
                    if (structToRemove.Remove(structType))
                    {
                        foreach (var field in structType.Fields)
                        {
                            SearchForTypes(field.Type.Type);
                        }
                    }
                    break;
                case CSUnmanagedFunctionType functionType:
                    SearchForTypes(functionType.ReturnType.Type);
                    foreach (var parameterType in functionType.ParameterTypes)
                    {
                        SearchForTypes(parameterType.Type);
                    }
                    break;
            }
        }

        foreach (var item in allMethods)
        {
            SearchForTypes(item.ReturnType.Type);
            foreach (var parameters in item.Parameters)
            {
                SearchForTypes(parameters.Type.Type);
            }
        }

        // Chain structs
        structToRemove.RemoveWhere(i =>
        {
            var chainFelid = i.Fields.FirstOrDefault(i => string.Equals(i.Name, "chain", StringComparison.OrdinalIgnoreCase));
            if (chainFelid == null)
            {
                return false;
            }

            var chainType = chainFelid.Type.Type;
            if (chainType is CSStruct structType)
            {
                return structType.Name.Contains("ChainedStruct");
            }

            return false;
        });

        foreach (var item in structToRemove)
        {
            structs.Remove(item);
        }

        return Task.CompletedTask;
    }

    public static Task UnwrapCallbacks(List<CSStruct> structs, List<CSStaticClass> staticClasses, List<CSEnum> enums)
    {
        Dictionary<CSStruct, CSUnmanagedFunctionType> callbacks = [];
        Regex callbackRegex = StructFixerRegex.GetCallbackRegex();
        foreach (var item in structs)
        {
            if (callbackRegex.IsMatch(item.Name) && item.Fields.Count == 1)
            {
                var field = item.Fields.First();
                if (field.Name == "Value" && field.Type.Type is CSUnmanagedFunctionType functionType)
                {
                    callbacks.Add(item, functionType);
                }
            }
        }

        ITypeReplace.ReplaceTypes([.. structs, .. staticClasses, .. enums], (ICSType type, [NotNullWhen(true)] out ICSType? newType) =>
        {
            if (type is CSStruct structType && callbacks.TryGetValue(structType, out var newTypeValue))
            {
                newType = newTypeValue;
                return true;
            }


            newType = default;
            return false;
        });

        foreach (var item in callbacks.Keys)
        {
            structs.Remove(item);
        }

        return Task.CompletedTask;
    }

    public static Task CreateHandleTypes(
        List<CSStruct> structs, List<CSStaticClass> staticClasses, List<CSEnum> enums)
    {
        HashSet<CSStruct> structToRemove = [];
        List<CSStruct> newHandles = [];
        List<(CSStruct structToReplace, CSStruct newStruct)> structsToReplace = [];
        foreach (var item in structs)
        {
            if (item.Fields.Count != 1)
            {
                continue;
            }

            var field = item.Fields.First();
            if (!field.Type.Modifiers.Contains(CsPointerType.Instance) ||
                field.Name != "Value")
            {
                continue;
            }

            var innerType = field.Type.Type;
            if (innerType is not CSStruct structType)
            {
                continue;
            }

            if (!structType.Name.EndsWith("Impl"))
            {
                continue;
            }

            var newStruct = new CSStruct()
            {
                Name = structType.Name.Replace("Impl", "Handle"),
                IsPartial = true,
                IsReadOnly = true,
                Namespace = "WebGpuSharp.FFI",
            };

            newStruct.EnrichingDataStore.Set(IsHandleMarker.Instance);

            var newStructType = new CSTypeInstance(newStruct);
            var newStructTypeNullable = new CSTypeInstance(newStruct) { IsNullable = true };
            var boolType = new CSTypeInstance(CSPrimitiveType.Instances.Bool);
            var intType = new CSTypeInstance(CSPrimitiveType.Instances.Int);
            var uIntPtrType = new CSTypeInstance(CSUIntPtrType.Instance);
            var objectType = new CSTypeInstance(CSPrimitiveType.Instances.Object);
            var nullableObjectType = new CSTypeInstance(CSPrimitiveType.Instances.Object) { IsNullable = true };

            newStruct.Interfaces.Add(new("IEquatable<{0}>", () => newStruct.Name));

            newStruct.Fields.Add(new(PRIVATE | READONLY, uIntPtrType, "_ptr"));
            newStruct.Fields.Add(new(PUBLIC | STATIC, newStructType, "Null")
            {
                GetterBody = new(" => new(nuint.Zero);"),
            });

            newStruct.Constructors.Add(new(PUBLIC, [(uIntPtrType, "ptr")])
            {
                Body = "=> _ptr = ptr;",
            });


            newStruct.Methods.AddRange([
                new(PUBLIC | STATIC | EXPLICIT, uIntPtrType, [(newStructType, "handle")])
                {
                    Body = "=> handle._ptr;",
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "==", [(newStructType, "left"), (newStructType, "right")])
                {
                    Body = "=> left._ptr == right._ptr;",
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "!=", [(newStructType, "left"), (newStructType, "right")])
                {
                    Body = "=> left._ptr != right._ptr;",
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "==", [(newStructType, "left"), (newStructTypeNullable, "right")])
                {
                    Body = "=> left._ptr == right.GetValueOrDefault()._ptr;",
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "!=", [(newStructType, "left"), (newStructTypeNullable, "right")])
                {
                    Body = "=> left._ptr != right.GetValueOrDefault()._ptr;",
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "==", [(newStructType, "left"), (uIntPtrType, "right")])
                {
                    Body = "=> left._ptr == right;",
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "!=", [(newStructType, "left"), (uIntPtrType, "right")])
                {
                    Body = "=> left._ptr != right;",
                },
                new(PUBLIC, uIntPtrType, "GetAddress", CSParameter.EmptyParameters)
                {
                    Body = "=> _ptr;",
                },
                new(PUBLIC, boolType, "Equals", [(newStructType, "other")])
                {
                    Body = "=> _ptr == other._ptr;",
                },
                new(PUBLIC | OVERRIDE, boolType, "Equals", [(nullableObjectType, "other")])
                {
                    Body = new("=> (other is {0} h && Equals(h)) || (other is null && _ptr == UIntPtr.Zero);", () => newStruct.Name)
                },
                new(PUBLIC | OVERRIDE, intType, "GetHashCode", CSParameter.EmptyParameters)
                {
                    Body = "=> _ptr.GetHashCode();",
                },
            ]);

            structToRemove.Add(item);
            structToRemove.Add(structType);
            newHandles.Add(newStruct);
            structsToReplace.Add((item, newStruct));
        }

        structs.RemoveAll(structToRemove.Contains);

        structs.AddRange(newHandles);

        foreach (var (structToReplace, newStruct) in structsToReplace)
        {
            ITypeReplace.ReplaceTypes([structs, staticClasses, enums], type =>
            {
                if (type is CSStruct structType && structType == structToReplace)
                {
                    return (true, newStruct);
                }

                return (false, default);
            });
        }

        return Task.CompletedTask;
    }

    public static Task FixStructsName(List<CSStruct> structs)
    {
        foreach (var item in structs)
        {
            const string prefix = "WGPU";
            if (item.Name.StartsWith(prefix))
            {
                item.Name = item.Name[prefix.Length..];
            }
        }

        return Task.CompletedTask;
    }

    public static Task AddStructModifiers(List<CSStruct> structs)
    {
        foreach (var item in structs)
        {
            item.IsPartial = true;
            foreach (var field in item.Fields)
            {
                if (field.Type.Modifiers.Contains(CsPointerType.Instance))
                {
                    item.IsUnsafe = true;
                }
            }
        }

        return Task.CompletedTask;
    }

    public static Task RemoveStructs(List<CSStruct> structs)
    {
        structs.RemoveAll(i => i.Name == "Proc" || i.Name == "WGPUProc");

        return Task.CompletedTask;
    }

    public static Task FFIRenameStructs(List<CSStruct> structs)
    {
        HashSet<CSStruct> ffiStructs = [];

        bool SetIsFFIType(CSStruct type)
        {
            if (type.Name.StartsWith("Chained"))
            {
                return false;
            }

            if (ffiStructs.Contains(type))
            {
                return true;
            }

            if (type.EnrichingDataStore.Has<IsHandleMarker>())
            {
                return true;
            }

            foreach (var field in type.Fields)
            {
                if (field.Type.Modifiers.Contains(CsPointerType.Instance) &&
                    (field.Type.Type is not CSStruct structTypeOfPtr ||
                    !structTypeOfPtr.Name.StartsWith("Chained")))
                {
                    ffiStructs.Add(type);
                    return true;
                }
                var felidType = field.Type.Type;
                if (felidType is CSUnmanagedFunctionType)
                {
                    ffiStructs.Add(type);
                    return true;
                }

                if (field.Type.Type is CSStruct structType)
                {
                    if (SetIsFFIType(structType))
                    {
                        ffiStructs.Add(type);
                        return true;
                    }
                }
            }

            return false;
        }

        foreach (var item in structs)
        {
            SetIsFFIType(item);
        }

        foreach (var item in ffiStructs)
        {
            item.IsUnsafe = true;
            item.Name += "FFI";
            item.Namespace = "WebGpuSharp.FFI";
        }

        return Task.CompletedTask;
    }

    public static Task FixWebgpuBoolType(List<CSStruct> structs)
    {
        var webgpuBool = structs.Find(i => i.Name.EndsWith("Bool") && i.Fields.Count == 1);
        if (webgpuBool is null)
        {
            return Task.CompletedTask;
        }

        webgpuBool.IsReadOnly = true;
        webgpuBool.IsUnsafe = false;
        webgpuBool.IsPartial = true;

        var webgpuField = webgpuBool.Fields.First();
        webgpuField.IsReadOnly = true;
        webgpuField.AccessModifier = CSAccessModifier.Private;

        webgpuBool.Name = "WebGPUBool";

        return Task.CompletedTask;
    }

    public static Task FieldNameFix(List<CSStruct> structs)
    {
        foreach (var item in structs)
        {
            foreach (var field in item.Fields)
            {
                if (field.AccessModifier == CSAccessModifier.Public && char.IsLower(field.Name[0]))
                {
                    field.Name = char.ToUpper(field.Name[0]) + field.Name[1..];
                }

                var name = field.Name;
                switch (field.AccessModifier)
                {
                    case CSAccessModifier.Public when !char.IsUpper(name[0]):
                        field.Name = name switch
                        {
                        [] => name,
                        [var first] => char.ToUpper(first).ToString(),
                        [var first, .. var rest] => char.ToUpper(first) + rest
                        };
                        break;

                    case CSAccessModifier.Private when name[0] != '_':
                        field.Name = $"_{char.ToLower(name[0])}{name[1..]}";
                        break;
                }
            }
        }

        return Task.CompletedTask;
    }

    public static Task AddConstructorsToStructs(List<CSStruct> structs)
    {
        foreach (var item in structs)
        {
            if (item.Fields.Count == 0)
            {
                continue;
            }

            var anyPrivateField = item.Fields.Any(i => i.AccessModifier == CSAccessModifier.Private);
            if (anyPrivateField)
            {
                continue;
            }


            item.Constructors.Add(new(PUBLIC, CSParameter.EmptyParameters)
            {
                Body = "",
            });

            List<CSParameter> parameters = [];
            List<(string fieldName, string parameterName)> fieldToParameter = [];

            foreach (var field in item.Fields)
            {
                var name = field.Name switch
                {
                ['_', ..] => field.Name[1..],
                [var first, .. var rest] when char.IsUpper(first) => char.ToLower(first) + rest,
                    _ => field.Name,
                };

                parameters.Add(new CSParameter(field.Type, name, CSDefaultValue.DefaultValue));
                fieldToParameter.Add((field.Name, name));
            }

            var body1 = new StringBuilder();
            foreach (var (fieldName, parameterName) in fieldToParameter)
            {
                body1.AppendLine($"this.{fieldName} = {parameterName};");
            }

            item.Constructors.Add(new(PUBLIC, parameters.ToArray())
            {
                Body = body1.ToString(),
            });

            if (parameters.RemoveAll(i => string.Equals(i.Name, "nextInChain", StringComparison.OrdinalIgnoreCase)) == 0 || parameters.Count == 0)
            {
                continue;
            }

            fieldToParameter.RemoveAll(i => string.Equals(i.fieldName, "nextInChain", StringComparison.OrdinalIgnoreCase));

            var body2 = new StringBuilder();
            foreach (var (fieldName, parameterName) in fieldToParameter)
            {
                body2.AppendLine($"this.{fieldName} = {parameterName};");
            }

            item.Constructors.Add(new(PUBLIC, parameters.ToArray())
            {
                Body = body2.ToString(),
            });
        }

        return Task.CompletedTask;
    }
}
