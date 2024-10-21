using CapiGenerator.CSModel;
using WebgpuBindgen.SpecDocRepresentation;
using WebgpuBindgen.SpecDocRepresentation.Members;
using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen;

public class CommentAssigner(
    CsTypeLookup csTypeLookup,
    CommentConvert commentConvert
)
{
    public void AssignComment(SpecDocLookup specDocLookup)
    {
        foreach (var type in specDocLookup.GetTypes())
        {
            var name = ToWebgpuSharpName(type.Name);

            var csType = csTypeLookup.FindType(name);

            var members = GetMemberBases(type);

            switch (csType)
            {
                case CSStruct structType:
                    AssignCommentToStructMembers(structType, members);
                    break;
                case CSEnum enumType:
                    AssignCommentToEnumMembers(enumType, type);
                    break;
                case CSStaticClass staticClass:
                    AssignCommentToStaticClassMembers(staticClass, members);
                    break;
            }
        }
    }

    void AssignCommentToEnumMembers(CSEnum type, RootWebidlTypeBase specMembers)
    {
        if (specMembers is not EnumWebidlType webidlEnumType)
        {
            return;
        }

        var enumValues = webidlEnumType.Values.ToList();
        foreach (var enumValue in enumValues)
        {
            if (enumValue.Comment is null or [])
            {
                continue;
            }

            var name = ToCsEnumName(enumValue.Value);
            var field = type.Values.FirstOrDefault(i => Compare(i.Name, name));
            if (field == null)
            {
                continue;
            }

            field.Comments = commentConvert.Convert(enumValue.Comment, field);
        }


        return;
    }


    void AssignCommentToStaticClassMembers(CSStaticClass type, WebidlMemberBase[] specMembers)
    {
        var operationMembers = specMembers.OfType<OperationMember>().ToList();
        var fieldMembers = specMembers.OfType<FieldMember>().ToList();
        var attributeMembers = specMembers.OfType<AttributeMember>().ToList();

        foreach (var operationMember in operationMembers)
        {
            if (operationMember.Comment is null or [])
            {
                continue;
            }

            var method = type.Methods.FirstOrDefault(i => Compare(i.Name!, operationMember.Name));
            if (method == null)
            {
                continue;
            }

            method.Comments = commentConvert.Convert(operationMember.Comment, method);
        }

        foreach (var fieldMember in fieldMembers)
        {
            if (fieldMember.Comment is null or [])
            {
                continue;
            }

            var field = type.Fields.FirstOrDefault(i => Compare(i.Name, fieldMember.Name));
            if (field == null)
            {
                continue;
            }

            field.Comments = commentConvert.Convert(fieldMember.Comment, field);
        }

        foreach (var attributeMember in attributeMembers)
        {
            if (attributeMember.Comment is null or [])
            {
                continue;
            }

            var field = type.Fields.FirstOrDefault(i => Compare(i.Name, attributeMember.Name));
            if (field == null)
            {
                continue;
            }

            field.Comments = commentConvert.Convert(attributeMember.Comment, field);
        }

    }

    void AssignCommentToStructMembers(CSStruct type, WebidlMemberBase[] specMembers)
    {
        var operationMembers = specMembers.OfType<OperationMember>().ToList();
        var fieldMembers = specMembers.OfType<FieldMember>().ToList();
        var attributeMembers = specMembers.OfType<AttributeMember>().ToList();

        foreach (var operationMember in operationMembers)
        {
            if (operationMember.Comment is null or [])
            {
                continue;
            }

            var method = type.Methods.FirstOrDefault(i => Compare(i.Name!, operationMember.Name));
            if (method == null)
            {
                continue;
            }

            method.Comments = commentConvert.Convert(operationMember.Comment, method);

            if (method.EnrichingDataStore.TryGetValue<FromStaticFFIMethodData>(out var data))
            {
                data.Method.Comments = commentConvert.Convert(operationMember.Comment, method);
            }
        }

        foreach (var fieldMember in fieldMembers)
        {
            if (fieldMember.Comment is null or [])
            {
                continue;
            }

            var field = type.Fields.FirstOrDefault(i => Compare(i.Name, fieldMember.Name));
            if (field == null)
            {
                continue;
            }

            field.Comments = commentConvert.Convert(fieldMember.Comment, field);
        }

        foreach (var attributeMember in attributeMembers)
        {
            if (attributeMember.Comment is null or [])
            {
                continue;
            }

            var field = type.Fields.FirstOrDefault(i => Compare(i.Name, attributeMember.Name));
            if (field == null)
            {
                continue;
            }

            field.Comments = commentConvert.Convert(attributeMember.Comment, field);
        }
    }

    static WebidlMemberBase[] GetMemberBases(RootWebidlTypeBase type) => type switch
    {
        DictionaryWebidlType dictionary => [.. dictionary.Members],
        InterfaceMixinWebidlType interfaceMixin => [.. interfaceMixin.Members],
        InterfaceWebidlType interfaceMixin => [.. interfaceMixin.Members],
        NamespaceWebidlType namespaceWebidlType => [.. namespaceWebidlType.Members],
        EnumWebidlType or TypedefWebidlType => [],
        _ => []
    };

    static bool Compare(string name, string otherName) =>
     name.Equals(otherName, StringComparison.CurrentCultureIgnoreCase);

    static string ToCsEnumName(string name) =>
        string.Join("", name.Split("-").Select(i => i.FirstCharToUpper()));

    static string ToWebgpuSharpName(string name)
    {
        if (name.StartsWith("GPU"))
        {
            name = name[3..];
        }

        if (name.EndsWith("Dict"))
        {
            name = name[0..^"Dict".Length];
        }

        if (name.Length > 1 && char.IsLower(name[0]))
        {
            name = char.ToUpper(name[0]) + name[1..];
        }

        return name;
    }

}