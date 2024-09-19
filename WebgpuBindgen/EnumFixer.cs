using System.Diagnostics.CodeAnalysis;
using CapiGenerator.CSModel;

namespace WebgpuBindgen;


public static class EnumFixer
{

    public static Task FixEnums(IEnumerable<CSEnum> enums)
    {
        foreach (var item in enums)
        {
            string name = item.Name;
            var force32BitItem = item.Values.FirstOrDefault(i => i.Name.EndsWith("_Force32"));
            if (force32BitItem != null)
            {
                item.Values.Remove(force32BitItem);
            }
            foreach (var value in item.Values)
            {
                string newName = value.Name;
                if (value.Name.StartsWith($"{name}_"))
                {
                    newName = value.Name[(name.Length + 1)..];
                }

                if (newName.Length > 1 && char.IsDigit(newName[0]) && newName[1] == 'D')
                {
                    char digit = newName[0];
                    newName = $"D{digit}{newName[2..]}";
                }

                value.Name = newName;
            }

            if (name.StartsWith("WGPU"))
            {
                item.Name = name[4..];
            }
        }

        return Task.CompletedTask;
    }

    public static Task FixFlagEnums(List<CSEnum> enums, List<CSStruct> structs, List<CSStaticClass> staticClasses)
    {
        HashSet<CSStruct> flagStructsToRemove = [];

        List<CSStruct> flagStructTypedefs = structs.Where(i =>
        {
            if (i.Fields.Count != 1)
            {
                return false;
            }

            var field = i.Fields.First();
            if (field.Type.Type is null)
            {
                return false;
            }

            var type = field.Type.Type;
            if (type is CSStruct structType && structType.Name.EndsWith("Flags"))
            {
                flagStructsToRemove.Add(structType);
                return true;
            }

            return false;
        }).ToList();

        Dictionary<CSStruct, List<CSField>> flagStructsToConstFelids = [];

        List<(CSStaticClass, CSField)> staticClassFieldsToRemove = [];

        foreach (var staticClass in staticClasses)
        {
            foreach (var field in staticClass.Fields)
            {
                foreach (var structTypedefs in flagStructTypedefs)
                {
                    if (field.Name.Contains($"{structTypedefs.Name}_"))
                    {
                        if (!flagStructsToConstFelids.TryGetValue(structTypedefs, out var list))
                        {
                            list = [];
                            flagStructsToConstFelids.Add(structTypedefs, list);
                        }
                        list.Add(field);
                        staticClassFieldsToRemove.Add((staticClass, field));
                        break;
                    }
                }
            }
        }

        Dictionary<CSStruct, CSEnum> structsToReplace = [];

        foreach (var (flagStruct, fields) in flagStructsToConstFelids)
        {
            if (!TryGetInnerPrimitiveType(flagStruct, out var innerType))
            {
                continue;
            }

            CSEnum newEnumType = new()
            {
                Name = flagStruct.Name,
                Type = innerType,
                Namespace = flagStruct.Namespace,
            };
            newEnumType.Attributes.Add(CSAttribute<FlagsAttribute>.Create(
                [],
                []
            ));

            enums.Add(newEnumType);
            foreach (var field in fields)
            {
                newEnumType.Values.Add(new()
                {
                    Name = field.Name,
                    Expression = field.DefaultValue.ToCSConstantExpression()!
                });
            }

            structsToReplace.Add(flagStruct, newEnumType);
            enums.Add(newEnumType);
        }

        ITypeReplace.ReplaceTypes([structs, staticClasses, enums], type =>
        {
            if (type is CSStruct structType && structsToReplace.TryGetValue(structType, out var newEnumType))
            {
                return (true, newEnumType);
            }

            return (false, default);
        });

        foreach (var (staticClass, field) in staticClassFieldsToRemove)
        {
            staticClass.Fields.Remove(field);
        }

        structs.RemoveAll(flagStructsToRemove.Contains);
        structs.RemoveAll(flagStructTypedefs.Contains);

        return Task.CompletedTask;
    }

    public static bool TryGetInnerPrimitiveType(CSStruct csStruct, [NotNullWhen(true)] out CSPrimitiveType? innerType)
    {
        if (csStruct.Fields.Count != 1)
        {
            innerType = null;
            return false;
        }

        var field = csStruct.Fields.First();
        if (field.Type.Type is CSPrimitiveType primitiveType)
        {
            innerType = primitiveType;
            return true;
        }

        if (field.Type.Type is CSStruct structType)
        {
            return TryGetInnerPrimitiveType(structType, out innerType);
        }

        innerType = null;
        return false;
    }
}