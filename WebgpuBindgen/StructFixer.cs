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
using CapiGenerator.CSModel.ConstantToken;
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
                IsUnsafe = true,
                Namespace = "WebGpuSharp.FFI",
            };

            newStruct.EnrichingDataStore.Set(IsHandleMarker.Instance);

            var newStructType = new CSTypeInstance(newStruct);
            var newStructTypeNullable = new CSTypeInstance(newStruct) { IsNullable = true };
            var boolType = new CSTypeInstance(CSPrimitiveType.Instances.Bool);
            var intType = new CSTypeInstance(CSPrimitiveType.Instances.Int);
            var uIntPtrType = new CSTypeInstance(CSPrimitiveType.Instances.NUInt);
            var objectType = new CSTypeInstance(CSPrimitiveType.Instances.Object);
            var nullableObjectType = new CSTypeInstance(CSPrimitiveType.Instances.Object) { IsNullable = true };

            newStruct.Interfaces.Add(new("IEquatable<{0}>", () => newStruct.Name));

            newStruct.Fields.Add(new(PRIVATE | READONLY, uIntPtrType, "_ptr"));
            newStruct.Fields.Add(new(PUBLIC | STATIC, newStructType, "Null")
            {
                GetterBody = new(" => new(nuint.Zero);"),
                Comments = new()
                {
                    Summary = new()
                    {
                        Description = "Get a null handle.",
                    },
                },
            });

            newStruct.Constructors.Add(new(PUBLIC, [(uIntPtrType, "ptr")])
            {
                Body = "=> _ptr = ptr;",
            });


            newStruct.Methods.AddRange([
                new(PUBLIC | STATIC | EXPLICIT, uIntPtrType, [(newStructType, "handle")])
                {
                    Body = "=> handle._ptr;",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Convert a handle to a pointer.",
                        },
                        Parameters = [
                            new(){Name = "handle",  Description = "The handle to convert."},
                        ]
                    },
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "==", [(newStructType, "left"), (newStructType, "right")])
                {
                    Body = "=> left._ptr == right._ptr;",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Check if two handles are equal.",
                        },
                        Parameters = [
                            new(){Name = "left",  Description = "The left handle."},
                            new(){Name = "right",  Description = "The right handle."},
                        ]
                    },
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "!=", [(newStructType, "left"), (newStructType, "right")])
                {
                    Body = "=> left._ptr != right._ptr;",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Check if two handles are not equal.",
                        },

                        Parameters = [
                            new(){Name = "left",  Description = "The left handle."},
                            new(){Name = "right",  Description = "The right handle."},
                        ]
                    },
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "==", [(newStructType, "left"), (newStructTypeNullable, "right")])
                {
                    Body = "=> left._ptr == right.GetValueOrDefault()._ptr;",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Check if two handles are equal.",
                        },
                        Parameters = [
                            new(){Name = "left",  Description = "The left handle."},
                            new(){Name = "right",  Description = "The right handle."},
                        ]
                    },
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "!=", [(newStructType, "left"), (newStructTypeNullable, "right")])
                {
                    Body = "=> left._ptr != right.GetValueOrDefault()._ptr;",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Check if two handles are not equal.",
                        },
                        Parameters = [
                            new(){Name = "left",  Description = "The left handle."},
                            new(){Name = "right",  Description = "The right handle."},
                        ]
                    },
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "==", [(newStructType, "left"), (uIntPtrType, "right")])
                {
                    Body = "=> left._ptr == right;",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Check if a handle is equal to a pointer.",
                        },
                        Parameters = [
                            new(){Name = "left",  Description = "The left handle."},
                            new(){Name = "right",  Description = "The right pointer."},
                        ]
                    },
                },
                new(PUBLIC | STATIC | OPERATOR, boolType, "!=", [(newStructType, "left"), (uIntPtrType, "right")])
                {
                    Body = "=> left._ptr != right;",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Check if a handle is not equal to a pointer.",
                        },
                        Parameters = [
                            new(){Name = "left",  Description = "The left handle."},
                            new(){Name = "right",  Description = "The right pointer."},
                        ]
                    },
                },
                new(PUBLIC, uIntPtrType, "GetAddress", CSParameter.EmptyParameters)
                {
                    Body = "=> _ptr;",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Get the address of the handle.",
                        },
                    },
                },
                new(PUBLIC, boolType, "Equals", [(newStructType, "other")])
                {
                    Body = "=> _ptr == other._ptr;",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Indicates whether the current object is equal to another object of the same type.",
                        },
                        Parameters = [
                            new(){Name = "other",  Description = "The other handle to compare with"},
                        ]
                    },
                },
                new(PUBLIC | OVERRIDE, boolType, "Equals", [(nullableObjectType, "other")])
                {
                    Body = new("=> (other is {0} h && Equals(h)) || (other is null && _ptr == UIntPtr.Zero);", () => newStruct.Name),
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Returns a value indicating whether this instance is equal to a specified object.",
                        },
                        Parameters = [
                            new(){Name = "other",  Description = "The other object to compare with"},
                        ]
                    },

                },
                new(PUBLIC | OVERRIDE, intType, "GetHashCode", CSParameter.EmptyParameters)
                {
                    Body = "=> _ptr.GetHashCode();",
                    Comments = new()
                    {
                        Summary = new()
                        {
                            Description = "Returns the hash code for this instance.",
                        },
                    },
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
        structs.RemoveAll(i => i.Name.StartsWith("INTERNAL__"));

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
                Body = "{}",
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

    public static Task AddEmptyConstructorsToStructs(List<CSStruct> structs)
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
                Body = "{}",
            });
        }

        return Task.CompletedTask;
    }

    public static Task AddDefaultValueFromStructFelids(List<CSStruct> structs)
    {
        string[] useDefaultValueList = [
            "TextureBindingLayout",
            "BufferBindingLayout",
            "SamplerBindingLayout",
            "StorageTextureBindingLayout"
        ];

        static bool IsPointer(CSTypeInstance type)
        {
            var modifiers = type.GetModifiersAsSpan();
            foreach (var modifier in modifiers)
            {
                if (modifier == CsPointerType.Instance)
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsEnum(CSTypeInstance type)
        {
            return type.Type is CSEnum;
        }

        static bool HasRequiredMembers(CSTypeInstance type)
        {
            return type.Type is CSStruct structType && structType.Fields.Any(i => i.IsRequired);
        }

        foreach (var item in structs)
        {
            var members = item.Fields.Where(
                    x =>
                    !x.IsRequired &&
                    x.GetterBody is null &&
                    x.SetterBody is null &&
                    !x.Name.EndsWith("chain", StringComparison.OrdinalIgnoreCase) &&
                    x.Type.Type is not CSPrimitiveType &&
                    x.Type.Type!.TryGetName(out var name) &&
                    !name.EndsWith("Handle", StringComparison.OrdinalIgnoreCase) &&
                    !IsPointer(x.Type) &&
                    !IsEnum(x.Type) &&
                    !HasRequiredMembers(x.Type)).ToList();
            foreach (var member in members)
            {
                var memberTypeName = member.Type.Type.TryGetName(out var name) ? name : null;
                if (memberTypeName is null)
                {
                    continue;
                }

                if (useDefaultValueList.Any(i => i.Contains(memberTypeName, StringComparison.OrdinalIgnoreCase)))
                {
                    member.DefaultValue = new([new CSArbitraryCodeToken("default")]);
                }

                if (member.DefaultValue == CSDefaultValue.NullValue)
                {
                    member.DefaultValue = new([new CSArbitraryCodeToken("new()")]);
                }
            }
        }

        return Task.CompletedTask;
    }

    public static Task FixLimits(List<CSStruct> structs, List<CSStaticClass> staticClasses)
    {
        var limitsStructs = structs.FindAll(i => i.Name == "Limits");
        if (limitsStructs.Count == 0)
        {
            Console.Error.WriteLine("Could not find Limits struct");
            return Task.CompletedTask;
        }
        else if (limitsStructs.Count > 1)
        {
            Console.Error.WriteLine("Found more than one Limits struct");
            return Task.CompletedTask;
        }

        var webGPUConst = staticClasses.Find(i => i.Name == "WebGPU_FFI");

        if (webGPUConst is null)
        {
            Console.Error.WriteLine("Could not find WebGPU_FFI const");
            return Task.CompletedTask;
        }

        var limitU32UndefinedFelid = webGPUConst.Fields.FirstOrDefault(i => i.Name == "LIMIT_U32_UNDEFINED");
        if (limitU32UndefinedFelid is null)
        {
            Console.Error.WriteLine("Could not find LIMIT_U32_UNDEFINED field");
            return Task.CompletedTask;
        }

        var limitU64UndefinedFelid = webGPUConst.Fields.FirstOrDefault(i => i.Name == "LIMIT_U64_UNDEFINED");
        if (limitU64UndefinedFelid is null)
        {
            Console.Error.WriteLine("Could not find LIMIT_U64_UNDEFINED field");
            return Task.CompletedTask;
        }

        var limitsStruct = limitsStructs.First();
        foreach (var field in limitsStruct.Fields)
        {
            if (field.Type.Type is not CSPrimitiveType primitiveType)
            {
                continue;
            }

            if (primitiveType.KindValue == CSPrimitiveType.Kind.UInt)
            {
                field.DefaultValue = new([new CSConstIdentifierToken(limitU32UndefinedFelid, false)]);
            }

            if (primitiveType.KindValue == CSPrimitiveType.Kind.ULong)
            {
                field.DefaultValue = new([new CSConstIdentifierToken(limitU64UndefinedFelid, false)]);
            }
        }

        return Task.CompletedTask;
    }

    public static Task FixPassTimestampWrites(List<CSStruct> structs, List<CSStaticClass> staticClasses)
    {
        var passTimestampWrites = structs.Find(i => i.Name == "PassTimestampWritesFFI");
        if (passTimestampWrites is null)
        {
            Console.Error.WriteLine("Could not find PassTimestampWritesFFI struct");
            return Task.CompletedTask;
        }

        var webGPUFFIClass = staticClasses.Find(i => i.Name == "WebGPU_FFI");
        if (webGPUFFIClass is null)
        {
            Console.Error.WriteLine("Could not find WebGPU_FFI class");
            return Task.CompletedTask;
        }

        var querySetIndexUndefinedFelid = webGPUFFIClass!.Fields.FirstOrDefault(i => i.Name == "QUERY_SET_INDEX_UNDEFINED");

        if (querySetIndexUndefinedFelid is null)
        {
            Console.Error.WriteLine("Could not find QUERY_SET_INDEX_UNDEFINED field");
            return Task.CompletedTask;
        }

        var beginningOfPassWriteIndexFelid = passTimestampWrites.Fields.FirstOrDefault(i => i.Name == "BeginningOfPassWriteIndex");
        if (beginningOfPassWriteIndexFelid is null)
        {
            Console.Error.WriteLine("Could not find BeginningOfPassWriteIndex field");
            return Task.CompletedTask;
        }

        var endOfPassWriteIndexFelid = passTimestampWrites.Fields.FirstOrDefault(i => i.Name == "EndOfPassWriteIndex");
        if (endOfPassWriteIndexFelid is null)
        {
            Console.Error.WriteLine("Could not find EndOfPassWriteIndex field");
            return Task.CompletedTask;
        }

        beginningOfPassWriteIndexFelid.DefaultValue = new([new CSConstIdentifierToken(querySetIndexUndefinedFelid, false)]);
        endOfPassWriteIndexFelid.DefaultValue = new([new CSConstIdentifierToken(querySetIndexUndefinedFelid, false)]);

        return Task.CompletedTask;
    }

    public static void AddNextInChainDocs(List<CSStruct> structs)
    {
        foreach (var item in structs)
        {
            var nextInChainField = item.Fields.FirstOrDefault(i => i.Name.Equals("NextInChain", StringComparison.OrdinalIgnoreCase));
            if (nextInChainField is null || nextInChainField?.Type?.Type?.TryGetName(out var name) != true || name != "ChainedStruct")
            {
                continue;
            }

            nextInChainField.Comments ??= new();
            nextInChainField.Comments.Summary = new()
            {
                Description =
                """
                Pointer to the first element in a chain of structures that extends this descriptor.
                """
            };

            nextInChainField.Comments.Remarks.Add(new()
            {
                Description =
                """
                Enables struct-chaining, a pattern that extends existing structs with new members while 
                maintaining API compatibility. Each extension struct must be properly initialized with 
                correct sType values and linked together. For detailed information about struct-chaining,
                see: <see href="https://webgpu-native.github.io/webgpu-headers/StructChaining.html"/>
                """
            });

        }

        foreach (var item in structs)
        {
            var nextInChainField = item.Fields.FirstOrDefault(i => i.Name.Equals("Chain", StringComparison.OrdinalIgnoreCase));
            if (nextInChainField is null || nextInChainField?.Type?.Type?.TryGetName(out var name) != true || name != "ChainedStruct")
            {
                continue;
            }

            nextInChainField.Comments ??= new();
            nextInChainField.Comments.Summary = new()
            {
                Description =
                """
                The chain link for struct chaining.
                """
            };
        }
    }

    public static void AddAddRefAndReleaseDocs(List<CSStruct> structs)
    {
        foreach (var item in structs)
        {
            if (!item.Name.EndsWith("Handle", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = $"""<see cref="{item.GetFullName()}"/>""";


            var addRefMethod = item.Methods.FirstOrDefault(i => i.Name?.Equals("AddRef", StringComparison.OrdinalIgnoreCase) == true);
            if (addRefMethod is null)
            {
                continue;
            }

            addRefMethod.Comments ??= new();
            addRefMethod.Comments.Summary = new()
            {
                Description =
                $"""
                Increments the reference count of the {name}.
                """
            };
            addRefMethod.Comments.Remarks.Add(new()
            {
                Description =
                $"""
                WebGPU objects are refcounted. Each call to <see cref="AddRef"/> must be balanced with a corresponding
                call to <see cref="Release"/> when the reference is no longer needed. Objects returned directly from
                the API start with a reference count of 1.
                
                Applications don't need to maintain refs to WebGPU objects that are internally used by other 
                WebGPU objects, as the implementation maintains internal references as needed.
                """
            });

            var releaseMethod = item.Methods.FirstOrDefault(i => i.Name?.Equals("Release", StringComparison.OrdinalIgnoreCase) == true);
            if (releaseMethod is null)
            {
                continue;
            }

            releaseMethod.Comments ??= new();
            releaseMethod.Comments.Summary = new()
            {
                Description =
                $"""
                Decrements the reference count of the {name}. When the reference count reaches zero, the {name} and associated resources may be freed.
                """
            };
            releaseMethod.Comments.Remarks.Add(new()
            {
                Description =
                $"""
                It's unsafe to use an object after its reference count has reached zero, even if other
                WebGPU objects internally reference it.
                
                Applications must call <see cref="Release"/> on all {name} references they own before losing the pointer.
                Failing to balance <see cref="AddRef"/> and <see cref="Release"/> calls will result in memory leaks or use-after-free errors.
                """
            });
        }
    }
}
