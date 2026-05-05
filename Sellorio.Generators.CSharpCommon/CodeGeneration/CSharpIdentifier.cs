using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Sellorio.Generators.CSharpCommon.CodeGeneration
{
    public static class CSharpIdentifier
    {
        private static readonly HashSet<string> _keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
            "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
            "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
            "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new",
            "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
            "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
            "ushort", "using", "virtual", "void", "volatile", "while"
        };

        public static string ToPascalCase(string value)
        {
            return NormalizeIdentifier(value, true);
        }

        public static string ToCamelCase(string value)
        {
            return NormalizeIdentifier(value, false);
        }

        public static string EscapeKeyword(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return _keywords.Contains(value) ? "@" + value : value;
        }

        private static string NormalizeIdentifier(string value, bool capitalizeFirstSegment)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return capitalizeFirstSegment ? "Value" : "value";
            }

            var segments = SplitSegments(value).ToList();
            if (segments.Count == 0)
            {
                return capitalizeFirstSegment ? "Value" : "value";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var normalizedSegment = char.ToUpperInvariant(segment[0]) + segment.Substring(1).ToLowerInvariant();

                if (i == 0 && !capitalizeFirstSegment)
                {
                    normalizedSegment = char.ToLowerInvariant(normalizedSegment[0]) + normalizedSegment.Substring(1);
                }

                builder.Append(normalizedSegment);
            }

            var result = builder.ToString();
            if (!IsIdentifierStart(result[0]))
            {
                result = (capitalizeFirstSegment ? "Value" : "value") + result;
            }

            return EscapeKeyword(result);
        }

        private static IEnumerable<string> SplitSegments(string value)
        {
            var builder = new StringBuilder();
            char? previousCharacter = null;

            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    if (builder.Length > 0 &&
                        previousCharacter.HasValue &&
                        char.IsLower(previousCharacter.Value) &&
                        char.IsUpper(character))
                    {
                        yield return builder.ToString();
                        builder.Clear();
                    }

                    builder.Append(character);
                    previousCharacter = character;
                }
                else if (builder.Length > 0)
                {
                    yield return builder.ToString();
                    builder.Clear();
                    previousCharacter = null;
                }
                else
                {
                    previousCharacter = null;
                }
            }

            if (builder.Length > 0)
            {
                yield return builder.ToString();
            }
        }

        private static bool IsIdentifierStart(char character)
        {
            if (character == '_')
            {
                return true;
            }

            switch (char.GetUnicodeCategory(character))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    return true;
                default:
                    return false;
            }
        }
    }
}
