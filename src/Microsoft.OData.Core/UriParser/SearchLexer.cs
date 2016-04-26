//---------------------------------------------------------------------
// <copyright file="SearchLexer.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.UriParser
{
    #region Namespaces
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
        #endregion Namespaces

    /// <summary>
    /// Lexer used for search query, note this is a little different ExpressionLexer, that it use double quote as string indicator.
    /// TODO: Extend the expression lexer.
    /// The result generated by this lexer:
    /// AND, OR, NOT        Identifier
    /// (                   OpenParen
    /// )                   CloseParen
    /// (others)            StringLiteral
    /// </summary>
    [DebuggerDisplay("SearchLexer ({text} @ {textPos} [{token}])")]
    internal sealed class SearchLexer : ExpressionLexer
    {
        /// <summary>
        /// Pattern for searchWord
        /// From ABNF rule:
        /// searchWord   = 1*ALPHA ; Actually: any character from the Unicode categories L or Nl, 
        ///               ; but not the words AND, OR, and NOT
        /// 
        /// \p{L} means any kind of letter from any language, include [Lo] such as CJK single character.
        /// </summary>
        internal static readonly Regex InvalidWordPattern = new Regex(@"([^\p{L}\p{Nl}])");

        /// <summary>
        /// Escape character used in search query
        /// </summary>
        private const char EscapeChar = '\\';

        /// <summary>
        /// Characters that could be escaped
        /// </summary>
        private const string EscapeSequenceSet = "\\\"";

        /// <summary>
        /// Keeps all keywords can be used in search query.
        /// </summary>
        private static readonly HashSet<string> KeyWords = new HashSet<string>(StringComparer.Ordinal) { ExpressionConstants.SearchKeywordAnd, ExpressionConstants.SearchKeywordOr, ExpressionConstants.SearchKeywordNot };

        /// <summary>
        /// Indicate whether current char is escaped.
        /// </summary>
        private bool isEscape;

        /// <summary>Initializes a new <see cref="SearchLexer"/>.</summary>
        /// <param name="expression">Expression to parse.</param>
        internal SearchLexer(string expression)
            : base(expression, true /*moveToFirstToken*/, false /*useSemicolonDelimeter*/)
        {
        }

        /// <summary>Reads the next token, skipping whitespace as necessary.</summary> 
        /// <param name="error">Error that occurred while trying to process the next token.</param>
        /// <returns>The next token, which may be 'bad' if an error occurs.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This parser method is all about the switch statement and would be harder to maintain if it were broken up.")]
        protected override ExpressionToken NextTokenImplementation(out Exception error)
        {
            error = null;

            this.ParseWhitespace();

            ExpressionTokenKind t;
            int tokenPos = this.textPos;
            switch (this.ch)
            {
                case '(':
                    this.NextChar();
                    t = ExpressionTokenKind.OpenParen;
                    break;
                case ')':
                    this.NextChar();
                    t = ExpressionTokenKind.CloseParen;
                    break;
                case '"':
                    char quote = this.ch.Value;

                    this.AdvanceToNextOccuranceOfWithEscape(quote);

                    if (this.textPos == this.TextLen)
                    {
                        throw ParseError(Strings.ExpressionLexer_UnterminatedStringLiteral(this.textPos, this.Text));
                    }

                    this.NextChar();

                    t = ExpressionTokenKind.StringLiteral;
                    break;
                default:
                    if (this.textPos == this.TextLen)
                    {
                        t = ExpressionTokenKind.End;
                    }
                    else
                    {
                        t = ExpressionTokenKind.Identifier;
                        do
                        {
                            this.NextChar();
                        } while (this.ch.HasValue && IsValidSearchTermChar(this.ch.Value));
                    }

                    break;
            }

            this.token.Kind = t;
            this.token.Text = this.Text.Substring(tokenPos, this.textPos - tokenPos);
            this.token.Position = tokenPos;

            if (this.token.Kind == ExpressionTokenKind.StringLiteral)
            {
                this.token.Text = this.token.Text.Substring(1, this.token.Text.Length - 2).Replace("\\\\", "\\").Replace("\\\"", "\"");
                if (string.IsNullOrEmpty(this.token.Text))
                {
                    throw ParseError(Strings.ExpressionToken_IdentifierExpected(this.token.Position));
                }
            }

            if ((this.token.Kind == ExpressionTokenKind.Identifier) && !KeyWords.Contains(this.token.Text))
            {
                Match match = InvalidWordPattern.Match(this.token.Text);
                if (match.Success)
                {
                    int index = match.Groups[0].Index;
                    throw ParseError(Strings.ExpressionLexer_InvalidCharacter(this.token.Text[index], this.token.Position + index, this.Text));
                }

                this.token.Kind = ExpressionTokenKind.StringLiteral;
            }

            return this.token;
        }

        /// <summary>
        /// Evaluate whether the given char is valid for a SearchTerm
        /// </summary>
        /// <param name="val">The char to be evaluated on.</param>
        /// <returns>Whether the given char is valid for a SearchTerm</returns>
        private static bool IsValidSearchTermChar(char val)
        {
            return !Char.IsWhiteSpace(val) && val != ')';
        }

        /// <summary>
        /// Move to next char, with escape char support.
        /// </summary>
        private void NextCharWithEscape()
        {
            this.isEscape = false;
            this.NextChar();
            if (this.ch == EscapeChar)
            {
                this.isEscape = true;
                this.NextChar();

                if (!this.ch.HasValue || EscapeSequenceSet.IndexOf(this.ch.Value) < 0)
                {
                    throw ParseError(Strings.ExpressionLexer_InvalidEscapeSequence(this.ch, this.textPos, this.Text));
                }
            }
        }

        /// <summary>
        /// Advance to certain char, with escpae char support.
        /// </summary>
        /// <param name="endingValue">the ending delimiter.</param>
        private void AdvanceToNextOccuranceOfWithEscape(char endingValue)
        {
            this.NextCharWithEscape();
            while (this.ch.HasValue && !(this.ch == endingValue && !this.isEscape))
            {
                this.NextCharWithEscape();
            }
        }
    }
}
