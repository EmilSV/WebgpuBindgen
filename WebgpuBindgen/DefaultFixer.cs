using System.Xml.Schema;
using CapiGenerator.CSModel;
using CapiGenerator.CSModel.ConstantToken;
using WebgpuBindgen.SpecDocRepresentation;
using WebgpuBindgen.SpecDocRepresentation.Defaults;
using WebgpuBindgen.SpecDocRepresentation.Members;
using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen;

public static class DefaultFixer
{
    public static Task FixDefaults(List<CSStruct> structs, SpecDocLookup specDocLookup)
    {
        foreach (var item in structs)
        {
            var type = specDocLookup.GetStructLikeType(item.Name);
            if (type is null)
            {
                continue;
            }

            FieldMember[] fieldMembers = type switch
            {
                DictionaryWebidlType dictionary => GetFieldMembers(dictionary),
                InterfaceMixinWebidlType interfaceMixin => GetFieldMembers(interfaceMixin),
                InterfaceWebidlType interfaceMixin => GetFieldMembers(interfaceMixin),
                _ => [],
            };

            if (fieldMembers.Length == 0)
            {
                continue;
            }

            List<(string name, FieldMember member)> fieldMembersWithName = fieldMembers.Select(i => (ConvertName(i.Name), i)).ToList();

            foreach (var field in item.Fields)
            {
                var fieldMember = fieldMembersWithName.FirstOrDefault(
                    i => i.name.Equals(field.Name, StringComparison.OrdinalIgnoreCase)
                ).member;
                if (fieldMember is null)
                {
                    continue;
                }

                if (fieldMember.Required)
                {
                    field.IsRequired = true;
                }

                var newDefaultValue = GetDefaultValue(field.Type.Type!, fieldMember);
                if (newDefaultValue is null)
                {
                    continue;
                }
                field.DefaultValue = newDefaultValue.Value;

            }
        }

        return Task.CompletedTask;
    }


    private static FieldMember[] GetFieldMembers(DictionaryWebidlType dictionary) =>
        dictionary.Members.OfType<FieldMember>().ToArray();

    private static FieldMember[] GetFieldMembers(InterfaceMixinWebidlType interfaceMixin) =>
        interfaceMixin.Members.OfType<FieldMember>().ToArray();

    private static FieldMember[] GetFieldMembers(InterfaceWebidlType interfaceMixin) =>
        interfaceMixin.Members.OfType<FieldMember>().ToArray();

    private static CSDefaultValue? GetDefaultValue(ICSType type, FieldMember fieldMember) =>
            fieldMember.Default switch
            {
                DefaultBoolean defaultBoolean => new(defaultBoolean.Value),
                DefaultNumber defaultNumber => GetDefaultValue(type, defaultNumber),
                DefaultString defaultString => GetDefaultValue(type, defaultString),

                _ => null,
            };

    private static CSDefaultValue GetDefaultValue(ICSType type, DefaultString defaultString)
    {
        if (type is CSEnum csEnum)
        {

            var defaultValueStr = defaultString.Value;
            var defaultValueSplit = defaultValueStr.Split('-');
            if (defaultValueSplit.Length > 1)
            {
                defaultValueStr = string.Join("", defaultValueSplit.Select(i => i.FirstCharToUpper()).ToArray());
            }
            else
            {
                defaultValueStr = defaultValueStr.FirstCharToUpper();
            }

            var numberStartStr = string.Join("", defaultValueStr.TakeWhile(char.IsNumber).ToArray());
            if (numberStartStr.Length > 0)
            {
                defaultValueStr = defaultValueStr[numberStartStr.Length..] + numberStartStr;
            }

            CSEnumField enumField = csEnum.Values.First(i => i.Name.Equals(defaultValueStr, StringComparison.OrdinalIgnoreCase));
            return new CSDefaultValue(new CSConstantExpression([new CSConstIdentifierToken(enumField, false)]));
        }

        return new(defaultString.Value);
    }

    private static CSDefaultValue GetDefaultValue(ICSType type, DefaultNumber defaultString)
    {
        long numberValue = GetNumberValue(defaultString.Value);
        if (type is CSEnum csEnum)
        {
            var enumField = csEnum.Values.First(i =>
            {
                if (i.Expression is [CSConstLiteralToken literalToken])
                {
                    return GetNumberValue(literalToken.Value) == numberValue;
                }

                return false;
            });

            return new CSDefaultValue(new CSConstantExpression([new CSConstIdentifierToken(enumField, false)]));
        }

        return new(defaultString.Value);
    }



    private static string ConvertName(string name)
    {
        var nameSplit = name.Split('-');
        if (nameSplit.Length > 1)
        {
            name = string.Join("", nameSplit.Select(i => i.FirstCharToUpper()).ToArray());
        }
        else
        {
            name = name.FirstCharToUpper();
        }

        var numberStartStr = string.Join("", name.TakeWhile(char.IsNumber).ToArray());

        if (numberStartStr.Length > 0)
        {
            name = name[numberStartStr.Length..] + numberStartStr;
        }

        return name;
    }

    private static long GetNumberValue(string value)
    {
        if (value.StartsWith("0x"))
        {
            return Convert.ToInt64(value[2..], 16);
        }
        else
        {
            return Convert.ToInt64(value);
        }
    }
}