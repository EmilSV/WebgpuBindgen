using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace WebgpuBindgen.XmlComments;

public static partial class XmlCommentParser
{
    public static async Task<IEnumerable<SubElementBase>> Parse(Stream xmlCommentStream)
    {
        var doc = await XDocument.LoadAsync(xmlCommentStream, LoadOptions.None, CancellationToken.None);
        return Parse(doc);
    }

    private static IEnumerable<SubElementBase> Parse(XDocument doc)
    {
        var root = doc.Root;
        var localName = root?.Name.LocalName;
        if (localName is not ("group" or "Group"))
        {
            throw new Exception("Root element must be group or Group");
        }
        return ParseGroup(root!);
    }

    private static IEnumerable<SubElementBase> ParseGroup(XElement element, GroupElement? parentGroup = null)
    {
        var prefix = element.Attribute("prefix")?.Value;
        var defaultPriority = element.Attribute("defaultPriority")?.Value;

        prefix = parentGroup == null ? prefix : parentGroup.Prefix + prefix;

        GroupElement groupElement = new()
        {
            Parent = parentGroup,
            Prefix = prefix ?? "",
            DefaultPriority = string.IsNullOrEmpty(defaultPriority) ? 0 : int.Parse(defaultPriority),
        };

        foreach (var childComments in element.XPathSelectElements("./Comment|./comment"))
        {
            foreach (var commentElement in ParseCommentElement(childComments, groupElement))
            {
                yield return commentElement;
            }
        }

        foreach (var childGroup in element.XPathSelectElements("./Group|./group"))
        {
            foreach (var commentElement in ParseGroup(childGroup, groupElement))
            {
                yield return commentElement;
            }
        }
    }

    private static IEnumerable<SubElementBase> ParseCommentElement(XElement element, GroupElement parentGroup)
    {
        var priority = element.Attribute("priority")?.Value;
        var applyToLocation = element.Attribute("location")?.Value;
        var cloneFromLocation = element.Attribute("cloneFrom")?.Value;
        var inheritFrom = element.Attribute("inheritFrom")?.Value;
        var inheritPriority = element.Attribute("inheritPriority")?.Value;

        applyToLocation = parentGroup.Prefix + applyToLocation ?? "";
        var commentElement = new CommentElement()
        {
            Parent = parentGroup,
            Priority = string.IsNullOrEmpty(priority) ? parentGroup.DefaultPriority : int.Parse(priority),
            ApplyToLocation = RemoveWhitespace(applyToLocation),
            CloneFromLocation = RemoveWhitespace(cloneFromLocation),
            InheritFrom = RemoveWhitespace(inheritFrom),
            InheritPriority = string.IsNullOrEmpty(inheritPriority) ? 0 : int.Parse(inheritPriority),
        };


        if (!string.IsNullOrWhiteSpace(commentElement.InheritFrom))
        {
            yield return new InheritElement()
            {
                ApplyToLocation = commentElement.ApplyToLocation,
                Priority = commentElement.InheritPriority,
                InheritFrom = commentElement.InheritFrom,
            };
        }


        foreach (var childComments in element.XPathSelectElements("./Value|./value"))
        {
            yield return ParseValueElement(childComments, commentElement);
        }

        foreach (var childComments in element.XPathSelectElements("./Summary|./summary"))
        {
            yield return ParseSummaryElement(childComments, commentElement);
        }

        foreach (var childComments in element.XPathSelectElements("./Returns|./returns"))
        {
            yield return ParseReturnElement(childComments, commentElement);
        }

        foreach (var childComments in element.XPathSelectElements("./Remark|./remark"))
        {
            yield return ParseRemarkElement(childComments, commentElement);
        }

        foreach (var childComments in element.XPathSelectElements("./Param|./param"))
        {
            yield return ParseParamElement(childComments, commentElement);
        }

        foreach (var childComments in element.XPathSelectElements("./Obsolete|./obsolete"))
        {
            yield return ParseObsoleteElement(childComments, commentElement);
        }
    }

    private static ValueElement ParseValueElement(XElement element, CommentElement parentComment)
    {
        var applyToLocation = element.Attribute("location")?.Value;
        if (string.IsNullOrEmpty(applyToLocation))
        {
            applyToLocation = parentComment.ApplyToLocation;
        }
        else
        {
            applyToLocation = parentComment.Parent.Prefix + applyToLocation;
        }

        var priorityStr = element.Attribute("priority")?.Value;
        var priority = string.IsNullOrEmpty(priorityStr) ? parentComment.Priority : int.Parse(priorityStr);
        var cloneFromLocation = element.Attribute("cloneFrom")?.Value ?? parentComment.CloneFromLocation;

        var description = ReadInnerXml(element);
        if (string.IsNullOrEmpty(applyToLocation))
        {
            throw new Exception("Value element must have location attribute");
        }

        applyToLocation = RemoveWhitespace(applyToLocation);
        cloneFromLocation = RemoveWhitespace(cloneFromLocation);

        return new ValueElement()
        {
            Priority = priority ?? 0,
            ApplyToLocation = applyToLocation,
            Description = description,
            CloneFromLocation = cloneFromLocation,
        };
    }

    private static SummaryElement ParseSummaryElement(XElement element, CommentElement parentComment)
    {
        var applyToLocation = element.Attribute("location")?.Value;
        if (string.IsNullOrEmpty(applyToLocation))
        {
            applyToLocation = parentComment.ApplyToLocation;
        }
        else
        {
            applyToLocation = parentComment.Parent.Prefix + applyToLocation;
        }

        var priorityStr = element.Attribute("priority")?.Value;
        var priority = string.IsNullOrEmpty(priorityStr) ? parentComment.Priority : int.Parse(priorityStr);
        var cloneFromLocation = element.Attribute("cloneFrom")?.Value ?? parentComment.CloneFromLocation;

        var description = ReadInnerXml(element);
        if (string.IsNullOrEmpty(applyToLocation))
        {
            throw new Exception("Value element must have location attribute");
        }

        applyToLocation = RemoveWhitespace(applyToLocation);
        cloneFromLocation = RemoveWhitespace(cloneFromLocation);

        return new SummaryElement()
        {
            Priority = priority ?? 0,
            ApplyToLocation = applyToLocation,
            Description = description,
            CloneFromLocation = cloneFromLocation,
        };
    }

    private static ReturnElement ParseReturnElement(XElement element, CommentElement parentComment)
    {
        var applyToLocation = element.Attribute("location")?.Value;
        if (string.IsNullOrEmpty(applyToLocation))
        {
            applyToLocation = parentComment.ApplyToLocation;
        }
        else
        {
            applyToLocation = parentComment.Parent.Prefix + applyToLocation;
        }

        var priorityStr = element.Attribute("priority")?.Value;
        var priority = string.IsNullOrEmpty(priorityStr) ? parentComment.Priority : int.Parse(priorityStr);
        var cloneFromLocation = element.Attribute("cloneFrom")?.Value ?? parentComment.CloneFromLocation;

        var description = ReadInnerXml(element);
        if (string.IsNullOrEmpty(applyToLocation))
        {
            throw new Exception("Value element must have location attribute");
        }

        applyToLocation = RemoveWhitespace(applyToLocation);
        cloneFromLocation = RemoveWhitespace(cloneFromLocation);

        return new ReturnElement()
        {
            Priority = priority ?? 0,
            ApplyToLocation = applyToLocation,
            Description = description,
            CloneFromLocation = cloneFromLocation,
        };
    }

    private static RemarkElement ParseRemarkElement(XElement element, CommentElement parentComment)
    {
        var applyToLocation = element.Attribute("location")?.Value;
        if (string.IsNullOrEmpty(applyToLocation))
        {
            applyToLocation = parentComment.ApplyToLocation;
        }
        else
        {
            applyToLocation = parentComment.Parent.Prefix + applyToLocation;
        }

        var priorityStr = element.Attribute("priority")?.Value;
        var priority = string.IsNullOrEmpty(priorityStr) ? parentComment.Priority : int.Parse(priorityStr);
        var cloneFromLocation = element.Attribute("cloneFrom")?.Value ?? parentComment.CloneFromLocation;

        var description = ReadInnerXml(element);
        if (string.IsNullOrEmpty(applyToLocation))
        {
            throw new Exception("Value element must have location attribute");
        }

        applyToLocation = RemoveWhitespace(applyToLocation);
        cloneFromLocation = RemoveWhitespace(cloneFromLocation);

        return new RemarkElement()
        {
            Priority = priority ?? 0,
            ApplyToLocation = applyToLocation,
            Description = description,
            CloneFromLocation = cloneFromLocation,
        };
    }

    private static ParamElement ParseParamElement(XElement element, CommentElement parentComment)
    {
        var applyToLocation = element.Attribute("location")?.Value;
        if (string.IsNullOrEmpty(applyToLocation))
        {
            applyToLocation = parentComment.ApplyToLocation;
        }
        else
        {
            applyToLocation = parentComment.Parent.Prefix + applyToLocation;
        }

        var priorityStr = element.Attribute("priority")?.Value;
        var priority = string.IsNullOrEmpty(priorityStr) ? parentComment.Priority : int.Parse(priorityStr);
        var cloneFromLocation = element.Attribute("cloneFrom")?.Value ?? parentComment.CloneFromLocation;

        var description = ReadInnerXml(element);
        if (string.IsNullOrEmpty(applyToLocation))
        {
            throw new Exception("Value element must have location attribute");
        }

        applyToLocation = RemoveWhitespace(applyToLocation);
        cloneFromLocation = RemoveWhitespace(cloneFromLocation);
        var name = element.Attribute("name")?.Value;

        if (name == null)
        {
            throw new Exception("Param element must have name attribute");
        }

        return new ParamElement()
        {
            Name = name,
            Priority = priority ?? 0,
            ApplyToLocation = applyToLocation,
            Description = description,
            CloneFromLocation = cloneFromLocation,
        };
    }

    private static ObsoleteElement ParseObsoleteElement(XElement element, CommentElement parentComment)
    {
        var applyToLocation = element.Attribute("location")?.Value;
        if (string.IsNullOrEmpty(applyToLocation))
        {
            applyToLocation = parentComment.ApplyToLocation;
        }
        else
        {
            applyToLocation = parentComment.Parent.Prefix + applyToLocation;
        }

        var priorityStr = element.Attribute("priority")?.Value;
        var priority = string.IsNullOrEmpty(priorityStr) ? parentComment.Priority : int.Parse(priorityStr);

        var isErrorStr = element.Attribute("isError")?.Value;
        var isError = bool.TryParse(isErrorStr, out var isErrorValue) && isErrorValue;

        var message = ReadInnerXml(element);
        if (string.IsNullOrEmpty(applyToLocation))
        {
            throw new Exception("Value element must have location attribute");
        }

        applyToLocation = RemoveWhitespace(applyToLocation);

        return new ObsoleteElement()
        {
            Priority = priority ?? 0,
            ApplyToLocation = applyToLocation,
            Message = message,
            IsError = isError,
        };
    }

    [return: NotNullIfNotNull("xml")]
    private static string? TrimXml(string? xml)
    {
        if (xml == null)
        {
            return null;
        }

        xml = xml.Trim();
        //Remove double space or more with single space
        xml = GetDoubleSpaceRegex().Replace(xml, " ");

        xml = string.Join("\n", xml.Split("\n").Select(i =>
        {
            var trimmed = i.Trim();
            if (trimmed.Length == 0)
            {
                return "";
            }
            return trimmed;
        }));
        return xml;
    }

    [return: NotNullIfNotNull("str")]
    private static string? RemoveWhitespace(string? str)
    {
        if (str == null)
        {
            return str;
        }
        return GetWhiteSpaceRegex().Replace(str, "");
    }

    private static string ReadInnerXml(XElement element)
    {
        var reader = element.CreateReader();
        reader.MoveToContent();

        return TrimXml(reader.ReadInnerXml());
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex GetWhiteSpaceRegex();
    [GeneratedRegex(@"  +")]
    private static partial Regex GetDoubleSpaceRegex();
}
