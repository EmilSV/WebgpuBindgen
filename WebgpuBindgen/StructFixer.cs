using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CapiGenerator.CSModel;

namespace WebgpuBindgen;



public static class StructFixer
{
    public static Task UnwrapCallbacks(List<CSStruct> structs, List<CSStaticClass> staticClasses, List<CSEnum> enums)
    {
        Dictionary<CSStruct, CSUnmanagedFunctionType> callbacks = [];
        foreach (var item in structs)
        {
            if (item.Name.EndsWith("Callback") && item.Fields.Count == 1)
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

        return Task.CompletedTask;
    }

    public static Task CreateHandleTypes(List<CSStruct> structs, List<CSStaticClass> staticClasses, List<CSEnum> enums)
    {
        HashSet<CSStruct> structToRemove = [];
        List<CSStruct> structsToAdd = [];
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
            };

            newStruct.Interfaces.Add(new("IEquatable<{0}>", () => newStruct.Name));

            newStruct.Fields.Add(new CSField()
            {
                Name = "_ptr",
                Type = new CSTypeInstance(CSUIntPtrType.Instance),
                AccessModifier = CSAccessModifier.Private,
                IsReadOnly = true,
            });

            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSUIntPtrType.Instance),
                "",
                [new("handle", new CSTypeInstance(newStruct))]
            )
            {
                AccessModifier = CSAccessModifier.Public,
                IsStatic = true,
                OperatorModifier = CSMethodOperatorModifier.Explicit,
                Body = new("return handle._ptr;"),
            });

            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSPrimitiveType.Instances.Bool),
                "==",
                [new("left", new CSTypeInstance(newStruct)), new("right", new CSTypeInstance(newStruct))]
            )
            {
                AccessModifier = CSAccessModifier.Public,
                IsStatic = true,
                OperatorModifier = CSMethodOperatorModifier.Operator,
                Body = new(" return left._ptr == right._ptr;"),
            });

            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSPrimitiveType.Instances.Bool),
                "!=",
                [new("left", new CSTypeInstance(newStruct)), new("right", new CSTypeInstance(newStruct))]
            )
            {
                AccessModifier = CSAccessModifier.Public,
                IsStatic = true,
                OperatorModifier = CSMethodOperatorModifier.Operator,
                Body = new(" return left._ptr != right._ptr;"),
            });

            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSPrimitiveType.Instances.Bool),
                "==",
                [
                    new("left", new CSTypeInstance(newStruct)),
                    new("right", new CSTypeInstance(newStruct) { IsNullable = true })
                ]
            )
            {
                AccessModifier = CSAccessModifier.Public,
                IsStatic = true,
                OperatorModifier = CSMethodOperatorModifier.Operator,
                Body = new(" return left._ptr == right.GetValueOrDefault()._ptr;"),
            });

            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSPrimitiveType.Instances.Bool),
                "!=",
                [
                    new("left", new CSTypeInstance(newStruct)),
                    new("right", new CSTypeInstance(newStruct) { IsNullable = true })
                ]
            )
            {
                AccessModifier = CSAccessModifier.Public,
                IsStatic = true,
                OperatorModifier = CSMethodOperatorModifier.Operator,
                Body = new(" return left._ptr == right.GetValueOrDefault()._ptr;"),
            });


            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSPrimitiveType.Instances.Bool),
                "==",
                [new("left", new CSTypeInstance(newStruct)), new("right", new CSTypeInstance(CSUIntPtrType.Instance))]
            )
            {
                AccessModifier = CSAccessModifier.Public,
                IsStatic = true,
                OperatorModifier = CSMethodOperatorModifier.Operator,
                Body = new(" return left._ptr == right;"),
            });

            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSPrimitiveType.Instances.Bool),
                "!=",
                [new("left", new CSTypeInstance(newStruct)), new("right", new CSTypeInstance(CSUIntPtrType.Instance))]
            )
            {
                AccessModifier = CSAccessModifier.Public,
                IsStatic = true,
                OperatorModifier = CSMethodOperatorModifier.Operator,
                Body = new(" return left._ptr != right;"),
            });


            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSPrimitiveType.Instances.Bool),
                "Equals",
                [new("other", new CSTypeInstance(newStruct))]
            )
            {
                AccessModifier = CSAccessModifier.Public,
                Body = new(" return Equals(other._ptr);"),
            });

            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSPrimitiveType.Instances.Bool),
                "Equals",
                [new("other", new CSTypeInstance(CSPrimitiveType.Instances.Object) { IsNullable = true })]
            )
            {
                AccessModifier = CSAccessModifier.Public,
                IsOverride = true,
                Body = new(" return (other is {0} h && Equals(h)) || (other is null && _ptr == UIntPtr.Zero);", () => newStruct.Name),
            });

            newStruct.Methods.Add(new CSMethod(
                new CSTypeInstance(CSPrimitiveType.Instances.Int),
                "GetHashCode",
                []
            )
            {
                AccessModifier = CSAccessModifier.Public,
                IsOverride = true,
                Body = new(" return _ptr.GetHashCode();"),
            });


            // public static bool operator ==(AdapterHandle left, AdapterHandle? right) => left._ptr == right.GetValueOrDefault()._ptr;
            // public static bool operator !=(AdapterHandle left, AdapterHandle? right) => left._ptr != right.GetValueOrDefault()._ptr;
            // public static bool operator ==(AdapterHandle left, UIntPtr right) => left._ptr == right;
            // public static bool operator !=(AdapterHandle left, UIntPtr right) => left._ptr != right;
            // public UIntPtr GetAddress() => _ptr;
            // public bool Equals(AdapterHandle h) => _ptr == h._ptr;
            // public override bool Equals(object? o) => (o is AdapterHandle h && Equals(h)) || (o is null && _ptr == UIntPtr.Zero);
            // public override int GetHashCode() => _ptr.GetHashCode();

            structToRemove.Add(item);
            structToRemove.Add(structType);
            structsToAdd.Add(newStruct);
            structsToReplace.Add((item, newStruct));
        }

        structs.RemoveAll(structToRemove.Contains);

        structs.AddRange(structsToAdd);

        foreach (var (structToReplace, newStruct) in structsToReplace)
        {
            ITypeReplace.ReplaceTypes([.. structs, .. staticClasses, .. enums], (ICSType type, [NotNullWhen(true)] out ICSType? newType) =>
            {
                if (type is CSStruct structType && structType == structToReplace)
                {
                    newType = newStruct;
                    return true;
                }

                newType = default;
                return false;
            });
        }

        return Task.CompletedTask;
    }
}
