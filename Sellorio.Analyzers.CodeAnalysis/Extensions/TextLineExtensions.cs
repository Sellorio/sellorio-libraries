using Microsoft.CodeAnalysis.Text;

namespace Sellorio.Analyzers.CodeAnalysis.Extensions
{
    internal static class TextLineExtensions
    {
        public static int GetIndentationWidth(this TextLine line)
        {
            var text = line.ToString();
            int indentationLevel = 0;

            foreach (var c in text)
            {
                if (c == ' ')
                {
                    indentationLevel++;
                }
                else if (c == '\t')
                {
                    indentationLevel += 4; // Assuming a tab is equivalent to 4 spaces
                }
                else
                {
                    break; // Stop counting once we hit a non-whitespace character
                }
            }

            return indentationLevel;
        }
    }
}
