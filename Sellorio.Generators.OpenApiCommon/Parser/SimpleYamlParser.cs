using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sellorio.Generators.OpenApiCommon.Parser
{
    internal static class SimpleYamlParser
    {
        public static IDictionary<object, object> Parse(string yaml)
        {
            if (yaml == null)
            {
                throw new ArgumentNullException(nameof(yaml));
            }

            var parser = new Parser(yaml);
            return parser.ParseDocument();
        }

        private sealed class Parser
        {
            private readonly List<LineInfo> _lines;

            private int _index;

            public Parser(string yaml)
            {
                _lines =
                    yaml
                        .Replace("\r\n", "\n")
                        .Replace('\r', '\n')
                        .Split('\n')
                        .Select((text, lineNumber) => new LineInfo(text, lineNumber + 1))
                        .ToList();
            }

            public IDictionary<object, object> ParseDocument()
            {
                SkipEmptyLines();
                var result = ParseNode(0) as IDictionary<object, object>;
                if (result == null)
                {
                    throw new InvalidOperationException("The OpenAPI YAML document root must be a mapping.");
                }

                return result;
            }

            private object ParseNode(int indent)
            {
                SkipEmptyLines();
                if (_index >= _lines.Count)
                {
                    return null;
                }

                var line = _lines[_index];
                if (line.Indent < indent)
                {
                    return null;
                }

                if (line.IsSequenceItem)
                {
                    return ParseSequence(indent);
                }

                return ParseMapping(indent);
            }

            private IDictionary<object, object> ParseMapping(int indent)
            {
                var mapping = new Dictionary<object, object>();

                while (true)
                {
                    SkipEmptyLines();
                    if (_index >= _lines.Count)
                    {
                        break;
                    }

                    var line = _lines[_index];
                    if (line.Indent < indent || line.IsSequenceItem || line.Indent != indent)
                    {
                        break;
                    }

                    _index++;
                    AddMappingEntry(mapping, indent, line.Content);
                }

                return mapping;
            }

            private IList<object> ParseSequence(int indent)
            {
                var sequence = new List<object>();

                while (true)
                {
                    SkipEmptyLines();
                    if (_index >= _lines.Count)
                    {
                        break;
                    }

                    var line = _lines[_index];
                    if (line.Indent < indent || !line.IsSequenceItem || line.Indent != indent)
                    {
                        break;
                    }

                    _index++;
                    sequence.Add(ParseSequenceItem(indent, line.SequenceContent));
                }

                return sequence;
            }

            private object ParseSequenceItem(int indent, string content)
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return ParseIndentedChild(indent);
                }

                var separatorIndex = FindKeySeparator(content);
                if (separatorIndex >= 0)
                {
                    var mapping = new Dictionary<object, object>();
                    AddMappingEntry(mapping, indent + 2, content);
                    MergeNestedMappingEntries(mapping, indent + 2);
                    return mapping;
                }

                return ParseScalarOrInlineCollection(content);
            }

            private void AddMappingEntry(IDictionary<object, object> mapping, int childIndent, string content)
            {
                var separatorIndex = FindKeySeparator(content);
                if (separatorIndex < 0)
                {
                    throw new InvalidOperationException("Invalid YAML mapping entry: '" + content + "'.");
                }

                var rawKey = content.Substring(0, separatorIndex).Trim();
                var key = ParseKey(rawKey);
                var rawValue = content.Substring(separatorIndex + 1).TrimStart();

                if (rawValue == "|" || rawValue == ">")
                {
                    mapping[key] = ParseBlockScalar(childIndent, rawValue == ">");
                    return;
                }

                if (rawValue.Length == 0)
                {
                    mapping[key] = ParseIndentedChild(childIndent);
                    return;
                }

                mapping[key] = ParseScalarOrInlineCollection(rawValue);
            }

            private void MergeNestedMappingEntries(IDictionary<object, object> mapping, int indent)
            {
                while (true)
                {
                    SkipEmptyLines();
                    if (_index >= _lines.Count)
                    {
                        return;
                    }

                    var line = _lines[_index];
                    if (line.Indent < indent)
                    {
                        return;
                    }

                    if (line.Indent > indent)
                    {
                        throw new InvalidOperationException("Unexpected indentation in YAML at line " + line.LineNumber.ToString(CultureInfo.InvariantCulture) + ".");
                    }

                    if (line.IsSequenceItem)
                    {
                        return;
                    }

                    _index++;
                    AddMappingEntry(mapping, indent + 2, line.Content);
                }
            }

            private object ParseIndentedChild(int indent)
            {
                SkipEmptyLines();
                if (_index >= _lines.Count)
                {
                    return null;
                }

                var nextLine = _lines[_index];
                if (nextLine.Indent < indent)
                {
                    return null;
                }

                return ParseNode(nextLine.Indent);
            }

            private string ParseBlockScalar(int indent, bool folded)
            {
                SkipEmptyLines();
                if (_index >= _lines.Count || _lines[_index].Indent < indent)
                {
                    return string.Empty;
                }

                var lines = new List<string>();
                while (_index < _lines.Count)
                {
                    var line = _lines[_index];
                    if (!line.IsEmpty && line.Indent < indent)
                    {
                        break;
                    }

                    _index++;

                    if (line.IsEmpty)
                    {
                        lines.Add(string.Empty);
                        continue;
                    }

                    var trimLength = Math.Min(indent, line.RawText.Length);
                    lines.Add(line.RawText.Substring(trimLength));
                }

                return folded
                    ? string.Join(" ", lines.Where(value => value.Length > 0))
                    : string.Join("\n", lines);
            }

            private object ParseScalarOrInlineCollection(string text)
            {
                var trimmed = text.Trim();
                if (trimmed.Length == 0)
                {
                    return string.Empty;
                }

                if (trimmed[0] == '[' && trimmed[trimmed.Length - 1] == ']')
                {
                    return ParseInlineSequence(trimmed);
                }

                if (trimmed[0] == '{' && trimmed[trimmed.Length - 1] == '}')
                {
                    return ParseInlineMapping(trimmed);
                }

                return ParseScalar(trimmed);
            }

            private IList<object> ParseInlineSequence(string text)
            {
                var content = text.Substring(1, text.Length - 2);
                var parts = SplitInlineItems(content);
                return parts.Select(ParseScalarOrInlineCollection).ToList();
            }

            private IDictionary<object, object> ParseInlineMapping(string text)
            {
                var content = text.Substring(1, text.Length - 2);
                var result = new Dictionary<object, object>();
                foreach (var part in SplitInlineItems(content))
                {
                    var separatorIndex = FindKeySeparator(part);
                    if (separatorIndex < 0)
                    {
                        continue;
                    }

                    var key = ParseKey(part.Substring(0, separatorIndex).Trim());
                    var value = ParseScalarOrInlineCollection(part.Substring(separatorIndex + 1).Trim());
                    result[key] = value;
                }

                return result;
            }

            private static IList<string> SplitInlineItems(string content)
            {
                var result = new List<string>();
                var start = 0;
                var depth = 0;
                var inSingleQuote = false;
                var inDoubleQuote = false;

                for (var i = 0; i < content.Length; i++)
                {
                    var character = content[i];
                    if (character == '\'' && !inDoubleQuote)
                    {
                        inSingleQuote = !inSingleQuote;
                    }
                    else if (character == '"' && !inSingleQuote && (i == 0 || content[i - 1] != '\\'))
                    {
                        inDoubleQuote = !inDoubleQuote;
                    }
                    else if (!inSingleQuote && !inDoubleQuote)
                    {
                        if (character == '[' || character == '{')
                        {
                            depth++;
                        }
                        else if (character == ']' || character == '}')
                        {
                            depth--;
                        }
                        else if (character == ',' && depth == 0)
                        {
                            result.Add(content.Substring(start, i - start).Trim());
                            start = i + 1;
                        }
                    }
                }

                var remaining = content.Substring(start).Trim();
                if (remaining.Length > 0)
                {
                    result.Add(remaining);
                }

                return result;
            }

            private static object ParseScalar(string text)
            {
                if (text == "null" || text == "~")
                {
                    return null;
                }

                if (text == "true")
                {
                    return true;
                }

                if (text == "false")
                {
                    return false;
                }

                if (text.Length >= 2 && text[0] == '\'' && text[text.Length - 1] == '\'')
                {
                    return text.Substring(1, text.Length - 2).Replace("''", "'");
                }

                if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
                {
                    return ParseDoubleQuotedString(text.Substring(1, text.Length - 2));
                }

                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                {
                    return intValue;
                }

                if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
                {
                    return decimalValue;
                }

                return text;
            }

            private static string ParseDoubleQuotedString(string text)
            {
                return text
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }

            private static object ParseKey(string text)
            {
                return ParseScalar(text);
            }

            private static int FindKeySeparator(string content)
            {
                var inSingleQuote = false;
                var inDoubleQuote = false;
                var bracketDepth = 0;
                var braceDepth = 0;

                for (var i = 0; i < content.Length; i++)
                {
                    var character = content[i];
                    if (character == '\'' && !inDoubleQuote)
                    {
                        inSingleQuote = !inSingleQuote;
                        continue;
                    }

                    if (character == '"' && !inSingleQuote && (i == 0 || content[i - 1] != '\\'))
                    {
                        inDoubleQuote = !inDoubleQuote;
                        continue;
                    }

                    if (inSingleQuote || inDoubleQuote)
                    {
                        continue;
                    }

                    switch (character)
                    {
                        case '[':
                            bracketDepth++;
                            break;
                        case ']':
                            bracketDepth--;
                            break;
                        case '{':
                            braceDepth++;
                            break;
                        case '}':
                            braceDepth--;
                            break;
                        case ':':
                            if (bracketDepth == 0 && braceDepth == 0 && (i == content.Length - 1 || char.IsWhiteSpace(content[i + 1])))
                            {
                                return i;
                            }

                            break;
                    }
                }

                return -1;
            }

            private void SkipEmptyLines()
            {
                while (_index < _lines.Count && _lines[_index].IsEmpty)
                {
                    _index++;
                }
            }
        }

        private sealed class LineInfo
        {
            public LineInfo(string rawText, int lineNumber)
            {
                RawText = rawText ?? string.Empty;
                LineNumber = lineNumber;
                Indent = GetIndent(RawText);
                var trimmed = RawText.Trim();
                IsEmpty = trimmed.Length == 0;
                if (!IsEmpty && RawText.Length > Indent && RawText[Indent] == '-')
                {
                    IsSequenceItem = RawText.Length == Indent + 1 || char.IsWhiteSpace(RawText[Indent + 1]);
                }

                Content = IsEmpty
                    ? string.Empty
                    : RawText.Substring(Indent);
                SequenceContent = IsSequenceItem
                    ? Content.Substring(1).TrimStart()
                    : string.Empty;
            }

            public string RawText { get; }

            public int LineNumber { get; }

            public int Indent { get; }

            public bool IsEmpty { get; }

            public bool IsSequenceItem { get; }

            public string Content { get; }

            public string SequenceContent { get; }

            private static int GetIndent(string text)
            {
                var index = 0;
                while (index < text.Length && text[index] == ' ')
                {
                    index++;
                }

                return index;
            }
        }
    }
}
