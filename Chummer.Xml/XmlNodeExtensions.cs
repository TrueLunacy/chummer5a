/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

using System;

#if DEBUG

using System.Diagnostics;

#endif

using System.Globalization;
using System.Xml;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.XPath;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Chummer.Xml
{
    public static class XmlNodeExtensions
    {
        /// <summary>
        /// Uses ISpanParseable to read a data field. The value is guaranteed to be the default
        /// value for the given type (0 for numeric types, null for reference types, 0-init for all other struct types)
        /// if the field cannot be read or parsed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetField<T>(this XmlNode? node, string field, [NotNullWhen(true)] out T? value)
            where T : IParsable<T>
        {
            value = default;
            return node.TryGetFieldUninitialized(field, ref value);
        }

        /// <summary>
        /// Uses ISpanParseable to read a data field. The value is guaranteed to be the default
        /// value for the given type (0 for numeric types, null for reference types, 0-init for all other struct types)
        /// if the field cannot be read or parsed.
        ///
        /// The difference between this and TryGetField is that this does not set the value upon failure.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetFieldUninitialized<T>(this XmlNode? node, string field, [NotNullWhen(true)] ref T? value)
            where T : IParsable<T>
        {
            string? text = node?[field]?.InnerText;
            bool result = T.TryParse(text, CultureInfo.InvariantCulture, out T? temp);
            if (result)
                value = temp;
            return result;
        }

        /// <summary>
        /// Processes a single operation node with children that are either nodes to check whether the parent has a node that fulfills a condition, or they are nodes that are parents to further operation nodes
        /// </summary>
        /// <param name="blnIsOrNode">Whether this is an OR node (true) or an AND node (false). Default is AND (false).</param>
        /// <param name="xmlOperationNode">The node containing the filter operation or a list of filter operations. Every element here is checked against corresponding elements in the parent node, using an operation specified in the element's attributes.</param>
        /// <param name="xmlParentNode">The parent node against which the filter operations are checked.</param>
        /// <returns>True if the parent node passes the conditions set in the operation node/nodelist, false otherwise.</returns>
        public static bool ProcessFilterOperationNode(this XmlNode xmlParentNode, XPathNavigator xmlOperationNode,
                                                      bool blnIsOrNode)
        {
            XPathNavigator xmlParentNavigator = xmlParentNode?.CreateNavigator();
            return xmlParentNavigator.ProcessFilterOperationNode(xmlOperationNode, blnIsOrNode);
        }

        /// <summary>
        /// Processes a single operation node with children that are either nodes to check whether the parent has a node that fulfills a condition, or they are nodes that are parents to further operation nodes
        /// </summary>
        /// <param name="blnIsOrNode">Whether this is an OR node (true) or an AND node (false). Default is AND (false).</param>
        /// <param name="xmlOperationNode">The node containing the filter operation or a list of filter operations. Every element here is checked against corresponding elements in the parent node, using an operation specified in the element's attributes.</param>
        /// <param name="xmlParentNode">The parent node against which the filter operations are checked.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>True if the parent node passes the conditions set in the operation node/nodelist, false otherwise.</returns>
        public static async Task<bool> ProcessFilterOperationNodeAsync(this XmlNode xmlParentNode, XPathNavigator xmlOperationNode, bool blnIsOrNode, CancellationToken token = default)
        {
            return ProcessFilterOperationNode(xmlParentNode, xmlOperationNode, blnIsOrNode);
        }

        /// <summary>
        /// Like TryGetField for strings, only with as little overhead as possible.
        /// </summary>
        public static bool TryGetStringFieldQuickly(this XmlNode node, string field, ref string read)
        {
            XmlElement objField = node?[field];
            if (objField != null)
            {
                read = objField.InnerText;
                return true;
            }
            XmlAttribute objAttribute = node?.Attributes?[field];
            if (objAttribute == null)
                return false;
            read = objAttribute.InnerText;
            return true;
        }

        /// <summary>
        /// Like TryGetField for strings, only with as little overhead as possible.
        /// </summary>
        public static bool TryGetMultiLineStringFieldQuickly(this XmlNode node, string field, ref string read)
        {
            string strReturn = string.Empty;
            if (node.TryGetStringFieldQuickly(field, ref strReturn))
            {
                read = strReturn.NormalizeLineEndings();
                return true;
            }
            return false;
        }


        /// <summary>
        /// Like TryGetField for guids, but taking advantage of guid.TryParse. Allows for returning false if the guid is Empty.
        /// </summary>
        /// <param name="node">XPathNavigator node of the object.</param>
        /// <param name="field">Field name of the InnerXML element we're looking for.</param>
        /// <param name="read">Guid that will be returned.</param>
        /// <param name="falseIfEmpty">Defaults to true. If false, will return an empty Guid if the returned Guid field is empty.</param>
        public static bool TryGetFieldUninitialized(this XmlNode node, string field, ref Guid read, bool falseIfEmpty = true)
        {
            XmlNode objField = node.SelectSingleNode(field);
            if (objField == null)
                return false;
            if (!Guid.TryParse(objField.InnerText, out Guid guidTmp))
                return false;
            if (guidTmp == Guid.Empty && falseIfEmpty)
                return false;
            read = guidTmp;
            return true;
        }

        /// <summary>
        /// Query the XmlNode for a given node with an id or name element. Includes ToUpperInvariant processing to handle uppercase ids.
        /// </summary>
        public static XmlNode TryGetNodeByNameOrId(this XmlNode node, string strPath, string strId, string strExtraXPath = "")
        {
            if (node == null || string.IsNullOrEmpty(strPath) || string.IsNullOrEmpty(strId))
                return null;
            if (Guid.TryParse(strId, out Guid guidId))
            {
                XmlNode objReturn = node.TryGetNodeById(strPath, guidId, strExtraXPath);
                if (objReturn != null)
                    return objReturn;
            }

            return node.SelectSingleNode(strPath + "[name = " + strId.CleanXPath()
                                         + (string.IsNullOrEmpty(strExtraXPath)
                                             ? "]"
                                             : " and (" + strExtraXPath + ")]"));
        }

        /// <summary>
        /// Query the XmlNode for a given node with an id. Includes ToUpperInvariant processing to handle uppercase ids.
        /// </summary>
        public static XmlNode TryGetNodeById(this XmlNode node, string strPath, Guid guidId, string strExtraXPath = "")
        {
            if (node == null || string.IsNullOrEmpty(strPath))
                return null;
            string strId = guidId.ToString("D", CultureInfo.InvariantCulture);
            return node.SelectSingleNode(strPath + "[id = " + strId.CleanXPath()
                                         + (string.IsNullOrEmpty(strExtraXPath)
                                             ? "]"
                                             : " and (" + strExtraXPath + ")]"))
                   // Split into two separate queries because the case-insensitive search here can be expensive if we're doing it a lot
                   ?? node.SelectSingleNode(strPath + "[translate(id, 'abcdef', 'ABCDEF') = "
                                                    + strId.ToUpperInvariant().CleanXPath()
                                                    + (string.IsNullOrEmpty(strExtraXPath)
                                                        ? "]"
                                                        : " and (" + strExtraXPath + ")]"));
        }

        /// <summary>
        /// Determine whether an XmlNode with the specified name exists within an XmlNode.
        /// </summary>
        /// <param name="xmlNode">XmlNode to examine.</param>
        /// <param name="strName">Name of the XmlNode to look for.</param>
        public static bool NodeExists(this XmlNode xmlNode, string strName)
        {
            if (string.IsNullOrEmpty(strName))
                return false;
            return xmlNode?.SelectSingleNode(strName) != null;
        }

        /// <summary>
        /// Selects a single node using the specified XPath expression, but also caches that expression in case the same expression is used over and over.
        /// Effectively a version of SelectSingleNode(string xpath) that is slower on the first run (and consumes some memory), but faster on subsequent runs.
        /// Only use this if there's a particular XPath expression that keeps being used over and over.
        /// </summary>
        public static XPathNavigator SelectSingleNodeAndCacheExpressionAsNavigator(this XmlNode xmlNode, string xpath, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            XPathNavigator xmlNavigator = xmlNode.CreateNavigator();
            return xmlNavigator.SelectSingleNodeAndCacheExpression(xpath, token);
        }

        /// <summary>
        /// Selects a node set using the specified XPath expression, but also caches that expression in case the same expression is used over and over.
        /// Effectively a version of Select(string xpath) that is slower on the first run (and consumes some memory), but faster on subsequent runs.
        /// Only use this if there's a particular XPath expression that keeps being used over and over.
        /// </summary>
        public static XPathNodeIterator SelectAndCacheExpressionAsNavigator(this XmlNode xmlNode, string xpath, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            XPathNavigator xmlNavigator = xmlNode.CreateNavigator();
            return xmlNavigator.SelectAndCacheExpression(xpath, token);
        }
    }
}
