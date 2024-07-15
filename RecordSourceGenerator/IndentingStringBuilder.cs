using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RecordSourceGenerator
{
    // Credit to CyrusNajmabadi, taken and modified from
    // https://gist.github.com/CyrusNajmabadi/edf9f6059d8fd2ced7a2086a8835be1d
    public sealed class IndentingStringBuilder
    {
        public static readonly char[] newLineChars = ['\r', '\n', '\f', '\u0085', '\u2028', '\u2029'];

        private readonly StringBuilder builder;
        private readonly string indentation;

        private int currentIndentationLevel = 0;
        private string currentIndentationString = "";

        private string[] indentationStrings = new string[4];

        /// <param name="indentation">What each indent level should be written into the final <paramref name="builder"/> as.</param>
        public IndentingStringBuilder(StringBuilder? builder = null, string indentation = "    ")
        {
            if (indentation is null) throw new ArgumentNullException(nameof(indentation));

            this.builder = builder ?? new StringBuilder(); ;
            this.indentation = indentation;

            indentationStrings[0] = "";
            for (int i = 1, n = indentationStrings.Length; i < n; i++)
                indentationStrings[i] = indentationStrings[i - 1] + indentation;
        }



        /// <summary>
        /// Increases the current indentation level, increasing the amount of indentation written at the start of a
        /// new line when content is written to it.
        /// </summary>
        private void IncreaseIndent()
        {
            currentIndentationLevel++;

            if (currentIndentationLevel == indentationStrings.Length)
                Array.Resize(ref indentationStrings, indentationStrings.Length * 2);

            indentationStrings[currentIndentationLevel] ??= indentationStrings[currentIndentationLevel - 1] + indentation;
            currentIndentationString = indentationStrings[currentIndentationLevel];
        }

        /// <summary>
        /// Decreases the current indentation level, decreasing the amount of indentation written at the start of a
        /// new line when content is written to it.
        /// </summary>
        private void DecreaseIndent()
        {
            Debug.Assert(currentIndentationLevel > 0);
            currentIndentationLevel--;
            currentIndentationString = indentationStrings[currentIndentationLevel];
        }

        public readonly struct DisposableBrace(IndentingStringBuilder sb) : IDisposable
        {
            public readonly void Dispose()
            {
                sb.DecreaseIndent();
                sb.WriteLine("}");
            }
        }

        public DisposableBrace Brace()
        {
            WriteLine("{");
            IncreaseIndent();
            return new DisposableBrace(this);
        }

        public void WriteLine() => builder.Append('\n');

        public void WriteLine(string content) => WriteLine(content.AsSpan());

        /// <inheritdoc cref="AppendContent(string, bool)"/>
        public void WriteLine(ReadOnlySpan<char> content)
        {
            while (content.Length > 0)
            {
                var newLineIndex = content.IndexOfAny(newLineChars);
                if (newLineIndex < 0)
                {
                    // no new line, append the rest of the content to the buffer.
                    Write(content);
                    builder.Append('\n');
                    return;
                }
                else
                {
                    while (newLineIndex < content.Length && newLineChars.Contains(content[newLineIndex + 1]))
                        newLineIndex++;

                    Write(content[0..newLineIndex]);
                    content = content[(newLineIndex + 1)..];
                }
            }
        }

        public unsafe void Write(ReadOnlySpan<char> line)
        { // we don't have ReadOnlySpan overloads in netstandard2.0, so drop to unsafe
            builder.Append(currentIndentationString);
            fixed (char* ptr = line)
            {
                builder.Append(ptr, line.Length);
            }
        }

        public override string ToString() => Regex.Replace(builder.ToString(), @"\r\n|\n\r|\n|\r", "\r\n");
    }

}
