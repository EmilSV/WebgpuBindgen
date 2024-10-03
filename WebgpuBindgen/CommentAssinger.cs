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
                    AssignCommentToEnumMembers(enumType, members);
                    break;
                case CSStaticClass staticClass:
                    AssignCommentToStaticClassMembers(staticClass, members);
                    break;
            }
        }
    }

    static void AssignCommentToEnumMembers(CSEnum type, WebidlMemberBase[] specMembers)
    {
        return; // TODO
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

            method.Comments = commentConvert.Convert(operationMember.Comment);
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

            field.Comments = commentConvert.Convert(fieldMember.Comment);
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

            field.Comments = commentConvert.Convert(attributeMember.Comment);
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

            method.Comments = commentConvert.Convert(operationMember.Comment);

            if (method.EnrichingDataStore.TryGetValue<FromStaticFFIMethodData>(out var data))
            {
                data.Method.Comments = commentConvert.Convert(operationMember.Comment);
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

            field.Comments = commentConvert.Convert(fieldMember.Comment);
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

            field.Comments = commentConvert.Convert(attributeMember.Comment);
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