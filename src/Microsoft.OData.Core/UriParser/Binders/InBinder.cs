﻿//---------------------------------------------------------------------
// <copyright file="InBinder.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.UriParser
{
    using System;
    using System.Buffers;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Microsoft.OData.Core;
    using Microsoft.OData.Edm;

    /// <summary>
    /// Class that knows how to bind the In operator.
    /// </summary>
    internal sealed class InBinder
    {
        private const string NullLiteral = "null";

        /// <summary>
        /// Method to use for binding the parent node, if needed.
        /// </summary>
        private readonly Func<QueryToken, QueryNode> bindMethod;

        /// <summary>
        /// Resolver for parsing
        /// </summary>
        private readonly ODataUriResolver resolver;

        /// <summary>
        /// Constructs a InBinder with the given method to be used binding the parent token if needed.
        /// </summary>
        /// <param name="bindMethod">Method to use for binding the parent token, if needed.</param>
        /// <param name="resolver">Resolver for parsing.</param>
        internal InBinder(Func<QueryToken, QueryNode> bindMethod, ODataUriResolver resolver)
        {
            this.bindMethod = bindMethod;
            this.resolver = resolver;
        }

        /// <summary>
        /// Binds an In operator token.
        /// </summary>
        /// <param name="inToken">The In operator token to bind.</param>
        /// <param name="state">State of the metadata binding.</param>
        /// <returns>The bound In operator token.</returns>
        internal QueryNode BindInOperator(InToken inToken, BindingState state)
        {
            ExceptionUtils.CheckArgumentNotNull(inToken, "inToken");

            SingleValueNode left = this.GetSingleValueOperandFromToken(inToken.Left);
            CollectionNode right = null;
            if (left.TypeReference != null)
            {
                right = this.GetCollectionOperandFromToken(
                    inToken.Right, new EdmCollectionTypeReference(new EdmCollectionType(left.TypeReference)), state.Model);
            }
            else 
            {
                right = this.GetCollectionOperandFromToken(
                    inToken.Right, new EdmCollectionTypeReference(new EdmCollectionType(EdmCoreModel.Instance.GetUntyped())), state.Model);
            }

            // If the left operand is either an integral or a string type and the right operand is a collection of enums,
            // Calls the MetadataBindingUtils.ConvertToTypeIfNeeded() method to convert the left operand to the same enum type as the right operand.
            if ((!(right is CollectionConstantNode) && right.ItemType.IsEnum()) && (left.TypeReference != null && (left.TypeReference.IsString() || left.TypeReference.IsIntegral())))
            {
                left = MetadataBindingUtils.ConvertToTypeIfNeeded(left, right.ItemType);
            }

            MetadataBindingUtils.VerifyCollectionNode(right, this.resolver.EnableCaseInsensitive);

            return new InNode(left, right);
        }

        /// <summary>
        /// Retrieve SingleValueNode bound with given query token.
        /// </summary>
        /// <param name="queryToken">The query token</param>
        /// <returns>The corresponding SingleValueNode</returns>
        private SingleValueNode GetSingleValueOperandFromToken(QueryToken queryToken)
        {
            SingleValueNode operand = this.bindMethod(queryToken) as SingleValueNode;
            if (operand == null)
            {
                throw new ODataException(SRResources.MetadataBinder_LeftOperandNotSingleValue);
            }

            return operand;
        }

        /// <summary>
        /// Retrieve CollectionNode bound with given query token.
        /// </summary>
        /// <param name="queryToken">The query token</param>
        /// <param name="expectedType">The expected type that this collection holds</param>
        /// <param name="model">The Edm model</param>
        /// <returns>The corresponding CollectionNode</returns>
        private CollectionNode GetCollectionOperandFromToken(QueryToken queryToken, IEdmTypeReference expectedType, IEdmModel model)
        {
            CollectionNode operand = null;
            LiteralToken literalToken = queryToken as LiteralToken;
            if (literalToken != null)
            {
                // Parentheses-based collections are not standard JSON but bracket-based ones are.
                // Temporarily switch our collection to bracket-based so that the JSON reader will
                // correctly parse the collection. Then pass the original literal text to the token.
                string bracketLiteralText = literalToken.OriginalText;

                if (bracketLiteralText[0] == '(' || bracketLiteralText[0] == '[')
                {
                    Debug.Assert((bracketLiteralText[0] == '(' && bracketLiteralText[^1] == ')') || (bracketLiteralText[0] == '[' && bracketLiteralText[^1] == ']'),
                        $"Collection with opening '{bracketLiteralText[0]}' should have corresponding '{(bracketLiteralText[0] == '(' ? ')' : ']')}'");

                    if (bracketLiteralText[0] == '(' && bracketLiteralText[^1] == ')')
                    {
                        bracketLiteralText = string.Create(bracketLiteralText.Length, bracketLiteralText, (span, state) =>
                        {
                            state.AsSpan().CopyTo(span);
                            span[0] = '[';
                            span[^1] = ']';
                        });
                    }

                    Debug.Assert(expectedType.IsCollection());
                    string expectedTypeFullName = expectedType.Definition.AsElementType().FullTypeName();

                    if (expectedTypeFullName.Equals("Edm.String", StringComparison.Ordinal) || (expectedTypeFullName.Equals("Edm.Untyped", StringComparison.Ordinal) && IsCollectionEmptyOrWhiteSpace(bracketLiteralText)))
                    {
                        // For collection of strings, need to convert single-quoted string to double-quoted string,
                        // and also, per ABNF, a single quote within a string literal is "encoded" as two consecutive single quotes in either
                        // literal or percent - encoded representation.
                        // Sample: ['a''bc','''def','xyz'''] ==> ["a'bc","'def","xyz'"], which is legitimate Json format.
                        bracketLiteralText = NormalizeStringCollectionItems(bracketLiteralText);
                    }
                    else if (expectedTypeFullName.Equals("Edm.Guid", StringComparison.Ordinal))
                    {
                        // For collection of Guids, need to convert the Guid literals to single-quoted form, so that it is compatible
                        // with the Json reader used for deserialization.
                        // Sample: [D01663CF-EB21-4A0E-88E0-361C10ACE7FD, 492CF54A-84C9-490C-A7A4-B5010FAD8104]
                        //    ==>  ['D01663CF-EB21-4A0E-88E0-361C10ACE7FD', '492CF54A-84C9-490C-A7A4-B5010FAD8104']
                        bracketLiteralText = NormalizeGuidCollectionItems(bracketLiteralText);
                    }
                    else if (expectedTypeFullName.Equals("Edm.DateTimeOffset", StringComparison.Ordinal) ||
                             expectedTypeFullName.Equals("Edm.Date", StringComparison.Ordinal) ||
                             expectedTypeFullName.Equals("Edm.TimeOfDay", StringComparison.Ordinal) ||
                             expectedTypeFullName.Equals("Edm.Duration", StringComparison.Ordinal))
                    {
                        // For collection of Date/Time/Duration items, need to convert the Date/Time/Duration literals to single-quoted form, so that it is compatible
                        // with the Json reader used for deserialization.
                        // Sample: [1970-01-01T00:00:00Z, 1980-01-01T01:01:01+01:00]
                        //    ==>  ['1970-01-01T00:00:00Z', '1980-01-01T01:01:01+01:00']
                        bracketLiteralText = NormalizeDateTimeCollectionItems(bracketLiteralText);
                    }
                }

                object collection = ODataUriConversionUtils.ConvertFromCollectionValue(bracketLiteralText, model, expectedType);
                LiteralToken collectionLiteralToken = new LiteralToken(collection, literalToken.OriginalText, expectedType);
                operand = this.bindMethod(collectionLiteralToken) as CollectionConstantNode;
            }
            else
            {
                var node = this.bindMethod(queryToken);
                if (node is SingleValueOpenPropertyAccessNode openNode)
                {
                    operand = new CollectionOpenPropertyAccessNode(openNode.Source, openNode.Name, expectedType as IEdmCollectionTypeReference);
                }
                else
                {
                    operand = node as CollectionNode;
                }
            }

            if (operand == null)
            {
                throw new ODataException(SRResources.MetadataBinder_RightOperandNotCollectionValue);
            }

            return operand;
        }

        private static string NormalizeStringCollectionItems(string literalText)
        {
            // a comma-separated list of primitive values, enclosed in parentheses, or a single expression that resolves to a collection
            // However, for String collection, we should process:
            // 1) comma could be part of the string value
            // 2) single quote could not be part of string value
            // 3) double quote could be part of string value, double quote also could be the starting and ending character.

            // remove the '[' and ']'
            string normalizedText = literalText.Substring(1, literalText.Length - 2).Trim();
            int length = normalizedText.Length;
            StringBuilder sb = new StringBuilder(length + 2);
            sb.Append('[');
            for (int i = 0; i < length; i++)
            {
                char ch = normalizedText[i];
                switch (ch)
                {
                    case '"':
                        i = ProcessDoubleQuotedStringItem(i, normalizedText, sb);
                        break;

                    case '\'':
                        i = ProcessSingleQuotedStringItem(i, normalizedText, sb);
                        break;

                    case ' ':
                        // ignore all whitespaces between items
                        break;

                    case ',':
                        // for multiple comma(s) between items, for example ('abc',,,'xyz'),
                        // We let it go and let the next layer to identify the problem by design.
                        sb.Append(',');
                        break;

                    case 'n':
                        // it maybe null
                        int index = normalizedText.IndexOf(',', i + 1);
                        string subStr;
                        if (index < 0)
                        {
                            subStr = normalizedText.Substring(i).TrimEnd(' ');
                            i = length - 1;
                        }
                        else
                        {
                            subStr = normalizedText.Substring(i, index - i).TrimEnd(' ');
                            i = index - 1;
                        }

                        if (subStr == NullLiteral)
                        {
                            sb.Append(NullLiteral);
                        }
                        else
                        {
                            throw new ODataException(Error.Format(SRResources.StringItemShouldBeQuoted, subStr));
                        }

                        break;

                    default:
                        // any other character between items is not valid.
                        throw new ODataException(Error.Format(SRResources.StringItemShouldBeQuoted, ch));
                }
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static int ProcessDoubleQuotedStringItem(int start, string input, StringBuilder sb)
        {
            Debug.Assert(input[start] == '"');

            int length = input.Length;
            int k = start + 1;

            // no matter it's single quote or not, just starting it as double quote (JSON).
            sb.Append('"');

            for (; k < length; k++)
            {
                char next = input[k];
                if (next == '"')
                {
                    // If prev and next are both double quotes, then it's an empty string.
                    if (input[k - 1] == '"')
                    {
                        // We append \"\" so as to return "\"\"" instead of "".
                        // This is to avoid passing an empty string to the ConstantNode.
                        sb.Append("\\\"\\\"");
                    }
                    break;
                }
                else if (next == '\\')
                {
                    sb.Append('\\');
                    if (k + 1 >= length)
                    {
                        // if end of string, stop it.
                        break;
                    }
                    else
                    {
                        // otherwise, append "\x" into
                        sb.Append(input[k + 1]);
                        k++;
                    }
                }
                else
                {
                    sb.Append(next);
                }
            }

            // no matter it's single quote or not, just ending it as double quote.
            sb.Append('"');
            return k;
        }

        private static int ProcessSingleQuotedStringItem(int start, string input, StringBuilder sb)
        {
            Debug.Assert(input[start] == '\'');

            int length = input.Length;
            int k = start + 1;

            // no matter it's single quote or not, just starting it as double quote (JSON).
            sb.Append('"');

            for (; k < length; k++)
            {
                char next = input[k];
                if (next == '\'')
                {
                    if (k + 1 >= length || input[k + 1] != '\'')
                    {
                        // If prev and next are both single quotes, then it's an empty string.
                        if (input[k - 1] == '\'')
                        {
                            if(k > 2 && input[k - 2] == '\'')
                            {
                                // We have 3 single quotes e.g 'ghi'''
                                // It means we need to unescape the double single quotes
                                // and escape double quote to return the result "ghi'" and process next items
                                sb.Append('"');
                                return k;
                            }
                            // We append \"\" so as to return "\"\"" instead of "".
                            // This is to avoid passing an empty string to the ConstantNode.
                            sb.Append("\\\"\\\"");
                        }
                        // match with single quote ('), stop it.
                        break;
                    }
                    else
                    {
                        // Unescape the double single quotes as one single quote, and continue
                        sb.Append('\'');
                        k++;
                    }
                }
                else if (next == '"')
                {
                    sb.Append('\\');
                    sb.Append('"');
                }
                else if (next == '\\')
                {
                    sb.Append("\\\\");
                }
                else
                {
                    sb.Append(next);
                }
            }

            // no matter it's single quote or not, just ending it as double quote.
            sb.Append('"');
            return k;
        }

        private static string NormalizeGuidCollectionItems(string bracketLiteralText)
        {
            string normalizedText = bracketLiteralText.Substring(1, bracketLiteralText.Length - 2).Trim();

            // If we have empty brackets ()
            if (normalizedText.Length == 0)
            {
                return "[]";
            }

            string[] items = normalizedText.Split(',')
                .Select(s => s.Trim()).ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != NullLiteral && items[i][0] != '\'' && items[i][0] != '"')
                {
                    items[i] = String.Format(CultureInfo.InvariantCulture, "'{0}'", items[i]);
                }
            }

            return "[" + String.Join(",", items) + "]";
        }

        private static string NormalizeDateTimeCollectionItems(string bracketLiteralText)
        {
            string normalizedText = bracketLiteralText.Substring(1, bracketLiteralText.Length - 2).Trim();

            // If we have empty brackets ()
            if (normalizedText.Length == 0)
            {
                return "[]";
            }

            string[] items = normalizedText.Split(',')
                .Select(s => s.Trim()).ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                const string durationPrefix = "duration";
                if (items[i] == NullLiteral)
                {
                    continue;
                }
                if (items[i].StartsWith(durationPrefix, StringComparison.Ordinal))
                {
                    items[i] = items[i].Remove(0, durationPrefix.Length);
                }
                if (items[i][0] != '\'' && items[i][0] != '"')
                {
                    items[i] = String.Format(CultureInfo.InvariantCulture, "'{0}'", items[i]);
                }
            }

            return "[" + String.Join(",", items) + "]";
        }

        private static bool IsCollectionEmptyOrWhiteSpace(string bracketLiteralText)
        {
            string content = bracketLiteralText[1..^1].Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                return true;
            }

            bool isEmptyOrWhiteSpace = true;
            bool isCharInsideQuotes = false;
            char quoteChar = '\0';
            char[] buffer = ArrayPool<char>.Shared.Rent(content.Length);
            int bufferIndex = 0;

            try
            {
                for (int i = 0; i < content.Length; i++)
                {
                    char c = content[i];

                    if (isCharInsideQuotes)
                    {
                        if (c == quoteChar)
                        {
                            isCharInsideQuotes = false;
                        }
                        buffer[bufferIndex++] = c;
                    }
                    else
                    {
                        if (c == '"' || c == '\'')
                        {
                            isCharInsideQuotes = true;
                            quoteChar = c;
                            buffer[bufferIndex++] = c;
                        }
                        else if (c == ',')
                        {
                            string item = new string(buffer, 0, bufferIndex).Trim().Trim('\'', '"');

                            if (!string.IsNullOrWhiteSpace(item))
                            {
                                isEmptyOrWhiteSpace = false;
                                break;
                            }
                            bufferIndex = 0;
                        }
                        else
                        {
                            buffer[bufferIndex++] = c;
                        }
                    }
                }

                if (bufferIndex > 0)
                {
                    string lastItem = new string(buffer, 0, bufferIndex).Trim().Trim('\'', '"');

                    if (!string.IsNullOrWhiteSpace(lastItem))
                    {
                        isEmptyOrWhiteSpace = false;
                    }
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }

            return isEmptyOrWhiteSpace;
        }
    }
}
