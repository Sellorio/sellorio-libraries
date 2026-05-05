using System;
using System.Text;

namespace Sellorio.Generators.CSharpCommon.CodeGeneration
{
    public sealed class CSharpSourceBuilder
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private int _indentLevel;

        public void AppendLine()
        {
            _builder.AppendLine();
        }

        public void AppendLine(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.Length != 0)
            {
                _builder.Append(new string(' ', _indentLevel * 4));
            }

            _builder.AppendLine(text);
        }

        public IDisposable BeginBlock(string header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            AppendLine(header);
            AppendLine("{");
            _indentLevel++;

            return new Block(this);
        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        private void EndBlock()
        {
            _indentLevel--;
            AppendLine("}");
        }

        private sealed class Block : IDisposable
        {
            private readonly CSharpSourceBuilder _builder;
            private bool _disposed;

            public Block(CSharpSourceBuilder builder)
            {
                _builder = builder;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _builder.EndBlock();
            }
        }
    }
}
