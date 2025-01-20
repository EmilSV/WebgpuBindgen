using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WebgpuBindgen.XmlComments;


public static partial class XmlCommentParser
{
    public static async Task<IEnumerable<SubCommentElementBase>> Parse(Stream xmlCommentStream)
    {
        var doc = await XDocument.LoadAsync(xmlCommentStream, LoadOptions.None, CancellationToken.None);
        return Parse(doc);
    }

    private static IEnumerable<SubCommentElementBase> Parse(XDocument doc)
    {
        var root = doc.Root;
        var localName = root?.Name.LocalName;
        if (localName is not "group" or "Group")
        {
            throw new Exception("Root element must be group or Group");
        }
        return ParseGroup(root!);
    }

    private static IEnumerable<SubCommentElementBase> ParseGroup(XElement element, GroupElement? parentGroup = null)
    {
        var prefix = element.Attribute("prefix")?.Value;

        prefix = parentGroup == null ? prefix : parentGroup.Prefix + prefix;

        GroupElement groupElement = new()
        {
            Parent = parentGroup,
            Prefix = prefix ?? ""
        };


        foreach (var childComments in element.Descendants(".//Comment|.//comment"))
        {
            foreach (var commentElement in ParseCommentElement(childComments, prefix))
            {
                yield return commentElement;
            }
        }

        foreach (var childGroup in element.Descendants(".//Group|.//group"))
        {
            foreach (var commentElement in ParseGroup(childGroup))
            {
                yield return commentElement;
            }
        }
    }

    private static IEnumerable<SubCommentElementBase> ParseCommentElement(XElement element, GroupElement parentGroup)
    {
        var priority = element.Attribute("priority")?.Value;
        var applyToLocation = element.Attribute("location")?.Value;
        var cloneFromLocation = element.Attribute("cloneFrom")?.Value;

        applyToLocation = parentGroup.Prefix + applyToLocation;
        var commentElement = new CommentElement()
        {
            Parent = parentGroup,
            Priority = string.IsNullOrEmpty(priority) ? 0 : int.Parse(priority),
            ApplyToLocation = RemoveWhitespace(applyToLocation),
            CloneFromLocation = RemoveWhitespace(cloneFromLocation),
        };

        foreach (var childComments in element.Descendants(".//Value|.//value"))
        {
            yield return ParseValueElement(childComments, commentElement);
        }

        foreach (var childComments in element.Descendants(".//Summary|.//summary"))
        {
            yield return ParseSummaryElement(childComments, commentElement);
        }

        foreach (var childComments in element.Descendants(".//Return|.//return"))
        {
            yield return ParseValueElement(childComments, commentElement);
        }

        foreach (var childComments in element.Descendants(".//Remark|.//remark"))
        {
            yield return ParseValueElement(childComments, commentElement);
        }

        foreach (var childComments in element.Descendants(".//Param|.//param"))
        {
            yield return ParseValueElement(childComments, commentElement);
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

        return new RemarkElement()
        {
            Priority = priority ?? 0,
            ApplyToLocation = applyToLocation,
            Description = description,
            CloneFromLocation = cloneFromLocation,
        };
    }

    private static RemarkElement ParseParamElement(XElement element, CommentElement parentComment)
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

        return new RemarkElement()
        {
            Priority = priority ?? 0,
            ApplyToLocation = applyToLocation,
            Description = description,
            CloneFromLocation = cloneFromLocation,
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
        return GetWhiteSpaceRegex().Replace(str, " ");
    }

    private static string ReadInnerXml(XElement element)
    {
        var reader = element.CreateReader();
        reader.MoveToContent();

        return reader.ReadInnerXml();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex GetWhiteSpaceRegex();
    [GeneratedRegex(@"  +")]
    private static partial Regex GetDoubleSpaceRegex();
}
