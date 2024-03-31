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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace Chummer.Xml
{
    public static class XPathNavigatorExtensions
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
        public static bool TryGetField<T>(this XPathNavigator? node, string field, [NotNullWhen(true)] out T? value)
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
        public static bool TryGetFieldUninitialized<T>(this XPathNavigator? node, string field, [NotNullWhen(true)] ref T? value)
            where T : IParsable<T>
        {
            string? text = node?.SelectSingleNode(field)?.Value;
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
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>True if the parent node passes the conditions set in the operation node/nodelist, false otherwise.</returns>
        public static async Task<bool> ProcessFilterOperationNodeAsync(this XPathNavigator xmlParentNode,
                                                                 XPathNavigator xmlOperationNode, bool blnIsOrNode, CancellationToken token = default)
        {
            return ProcessFilterOperationNode(xmlParentNode, xmlOperationNode, blnIsOrNode);
        }

        /// <summary>
        /// Processes a single operation node with children that are either nodes to check whether the parent has a node that fulfills a condition, or they are nodes that are parents to further operation nodes
        /// </summary>
        /// <param name="blnIsOrNode">Whether this is an OR node (true) or an AND node (false). Default is AND (false).</param>
        /// <param name="xmlOperationNode">The node containing the filter operation or a list of filter operations. Every element here is checked against corresponding elements in the parent node, using an operation specified in the element's attributes.</param>
        /// <param name="xmlParentNode">The parent node against which the filter operations are checked.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>True if the parent node passes the conditions set in the operation node/nodelist, false otherwise.</returns>
        public static bool ProcessFilterOperationNode(this XPathNavigator xmlParentNode, XPathNavigator xmlOperationNode, bool blnIsOrNode)
        {
            if (xmlOperationNode == null)
                return false;

            foreach (XPathNavigator xmlOperationChildNode in xmlOperationNode.SelectChildren(XPathNodeType.Element))
            {
                bool blnInvert
                    = xmlOperationChildNode.SelectSingleNodeAndCacheExpression("@NOT") != null;

                bool blnOperationChildNodeResult = blnInvert;
                string strNodeName = xmlOperationChildNode.Name;
                switch (strNodeName)
                {
                    case "OR":
                        blnOperationChildNodeResult = ProcessFilterOperationNode(xmlParentNode, xmlOperationChildNode, true)
                            != blnInvert;
                        break;

                    case "NOR":
                        blnOperationChildNodeResult = ProcessFilterOperationNode(xmlParentNode, xmlOperationChildNode, true)
                            == blnInvert;
                        break;

                    case "AND":
                        blnOperationChildNodeResult = ProcessFilterOperationNode(xmlParentNode, xmlOperationChildNode, false)
                            != blnInvert;
                        break;

                    case "NAND":
                        blnOperationChildNodeResult = ProcessFilterOperationNode(xmlParentNode, xmlOperationChildNode, false)
                            == blnInvert;
                        break;

                    case "NONE":
                        blnOperationChildNodeResult = (xmlParentNode == null) != blnInvert;
                        break;

                    default:
                        {
                            if (xmlParentNode != null)
                            {
                                XPathNavigator objOperationAttribute = xmlOperationChildNode.SelectSingleNodeAndCacheExpression("@operation");
                                string strOperationType = objOperationAttribute?.Value ?? "==";
                                XPathNodeIterator objXmlTargetNodeList = xmlParentNode.Select(strNodeName);
                                // If we're just checking for existence of a node, no need for more processing
                                if (strOperationType == "exists")
                                {
                                    blnOperationChildNodeResult = (objXmlTargetNodeList.Count > 0) != blnInvert;
                                }
                                else
                                {
                                    bool blnOperationChildNodeAttributeOr = xmlOperationChildNode.SelectSingleNodeAndCacheExpression("@OR") != null;
                                    // default is "any", replace with switch() if more check modes are necessary
                                    XPathNavigator objCheckTypeAttribute = xmlOperationChildNode.SelectSingleNodeAndCacheExpression("@checktype");
                                    bool blnCheckAll = objCheckTypeAttribute?.Value == "all";
                                    blnOperationChildNodeResult = blnCheckAll;
                                    string strOperationChildNodeText = xmlOperationChildNode.Value;
                                    bool blnOperationChildNodeEmpty = string.IsNullOrWhiteSpace(strOperationChildNodeText);

                                    foreach (XPathNavigator xmlTargetNode in objXmlTargetNodeList)
                                    {
                                        bool boolSubNodeResult = blnInvert;
                                        if (xmlTargetNode.SelectChildren(XPathNodeType.Element).Count > 0)
                                        {
                                            if (xmlOperationChildNode.SelectChildren(XPathNodeType.Element).Count > 0)
                                                boolSubNodeResult = ProcessFilterOperationNode(xmlTargetNode,
                                                                            xmlOperationChildNode,
                                                                            blnOperationChildNodeAttributeOr)
                                                                    != blnInvert;
                                        }
                                        else
                                        {
                                            string strTargetNodeText = xmlTargetNode.Value;
                                            bool blnTargetNodeEmpty = string.IsNullOrWhiteSpace(strTargetNodeText);
                                            if (blnTargetNodeEmpty || blnOperationChildNodeEmpty)
                                            {
                                                if (blnTargetNodeEmpty == blnOperationChildNodeEmpty
                                                    && (strOperationType == "=="
                                                        || strOperationType == "equals"))
                                                {
                                                    boolSubNodeResult = !blnInvert;
                                                }
                                                else
                                                {
                                                    boolSubNodeResult = blnInvert;
                                                }
                                            }
                                            // Note when adding more operation cases: XML does not like the "<" symbol as part of an attribute value
                                            else
                                                switch (strOperationType)
                                                {
                                                    case "doesnotequal":
                                                    case "notequals":
                                                    case "!=":
                                                        blnInvert = !blnInvert;
                                                        goto default;
                                                    case "lessthan":
                                                        blnInvert = !blnInvert;
                                                        goto case ">=";
                                                    case "lessthanequals":
                                                        blnInvert = !blnInvert;
                                                        goto case ">";

                                                    case "like":
                                                    case "contains":
                                                        {
                                                            boolSubNodeResult =
                                                                strTargetNodeText.Contains(strOperationChildNodeText, StringComparison.OrdinalIgnoreCase)
                                                                != blnInvert;
                                                            break;
                                                        }
                                                    case "greaterthan":
                                                    case ">":
                                                        {
                                                            boolSubNodeResult =
                                                                (int.TryParse(strTargetNodeText, out int intTargetNodeValue)
                                                                 && int.TryParse(strOperationChildNodeText, out int intChildNodeValue)
                                                                 && intTargetNodeValue > intChildNodeValue)
                                                                != blnInvert;
                                                            break;
                                                        }
                                                    case "greaterthanequals":
                                                    case ">=":
                                                        {
                                                            boolSubNodeResult =
                                                                (int.TryParse(strTargetNodeText, out int intTargetNodeValue)
                                                                 && int.TryParse(strOperationChildNodeText, out int intChildNodeValue)
                                                                 && intTargetNodeValue >= intChildNodeValue)
                                                                != blnInvert;
                                                            break;
                                                        }
                                                    default:
                                                        boolSubNodeResult =
                                                            (strTargetNodeText.Trim() == strOperationChildNodeText.Trim())
                                                            != blnInvert;
                                                        break;
                                                }
                                        }

                                        if (blnCheckAll)
                                        {
                                            if (!boolSubNodeResult)
                                            {
                                                blnOperationChildNodeResult = false;
                                                break;
                                            }
                                        }
                                        // default is "any", replace above with a switch() should more than two check types be required
                                        else if (boolSubNodeResult)
                                        {
                                            blnOperationChildNodeResult = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            break;
                        }
                }

                switch (blnIsOrNode)
                {
                    case true when blnOperationChildNodeResult:
                        return true;

                    case false when !blnOperationChildNodeResult:
                        return false;
                }
            }

            return !blnIsOrNode;
        }

        /// <summary>
        /// Like TryGetField for strings, only with as little overhead as possible.
        /// </summary>
        public static bool TryGetStringFieldQuickly(this XPathNavigator node, string field, [NotNullWhen(true)] ref string? read)
        {
            if (node == null)
            {
                return false;
            }
            XPathNavigator objField = node.SelectSingleNode(XmlUtilities.CacheExpression(field));
            if (objField == null && !field.StartsWith('@'))
            {
                field = '@' + field;
                objField = node.SelectSingleNode(XmlUtilities.CacheExpression(field));
            }
            if (objField != null)
            {
                read = objField.Value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Like TryGetField for strings, only with as little overhead as possible and with an extra line ending normalization thrown in.
        /// </summary>
        public static bool TryGetMultiLineStringFieldQuickly(this XPathNavigator node, string field, ref string read)
        {
            string? strReturn = null;
            if (node.TryGetStringFieldQuickly(field, ref strReturn))
            {
                read = strReturn.NormalizeLineEndings();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determine whether an XPathNavigator with the specified name exists within an XPathNavigator.
        /// </summary>
        /// <param name="xmlNode">XPathNavigator to examine.</param>
        /// <param name="strName">Name of the XPathNavigator to look for.</param>
        public static bool NodeExists(this XPathNavigator xmlNode, string strName)
        {
            if (string.IsNullOrEmpty(strName) || xmlNode == null)
                return false;
            XPathNavigator? objField = xmlNode.SelectSingleNode(XmlUtilities.CacheExpression(strName));
            return objField != null;
        }

        /// <summary>
        /// Query the XPathNavigator for a given node with an id or name element. Includes ToUpperInvariant processing to handle uppercase ids.
        /// </summary>
        /// <param name="node">XPathNavigator to examine.</param>
        /// <param name="strPath">Name of the XPathNavigator to look for.</param>
        /// <param name="strId">Element to search for. If it parses as a guid or f it fails to parse as a guid AND blnIdIsGuid is set, it will still search for id, otherwise it will search for a node with a name element that matches.</param>
        /// <param name="strExtraXPath">'Extra' value to append to the search.</param>
        /// <param name="blnIdIsGuid">Whether to evaluate the ID as a GUID or a string. Use false to pass strId as a string.</param>
        public static XPathNavigator TryGetNodeByNameOrId(this XPathNavigator node, string strPath, string strId, string strExtraXPath = "", bool blnIdIsGuid = true)
        {
            if (node == null || string.IsNullOrEmpty(strPath) || string.IsNullOrEmpty(strId))
                return null;

            if (Guid.TryParse(strId, out Guid guidId))
            {
                XPathNavigator objReturn = node.TryGetNodeById(strPath, guidId, strExtraXPath);
                if (objReturn != null)
                    return objReturn;
            }
            // This is mostly for improvements.xml, which uses the improvement id (such as addecho) as the id rather than a guid.
            if (!blnIdIsGuid)
            {
                return node.SelectSingleNode(strPath + "[id = " + strId.CleanXPath()
                                             + (string.IsNullOrEmpty(strExtraXPath)
                                                 ? "]"
                                                 : " and (" + strExtraXPath + ") ]"));
            }

            return node.SelectSingleNode(strPath + "[name = " + strId.CleanXPath()
                                         + (string.IsNullOrEmpty(strExtraXPath)
                                             ? "]"
                                             : " and (" + strExtraXPath + ") ]"));
        }

        /// <summary>
        /// Query the XPathNavigator for a given node with an id. Includes ToUpperInvariant processing to handle uppercase ids.
        /// </summary>
        public static XPathNavigator TryGetNodeById(this XPathNavigator node, string strPath, Guid guidId, string strExtraXPath = "")
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
        /// Create a new XmlNode in an XmlDocument based on the contents of an XPathNavigator
        /// </summary>
        /// <param name="xmlNode">XPathNavigator to examine.</param>
        /// <param name="xmlParentDocument">Document to house the XmlNode</param>
        public static XmlNode ToXmlNode(this XPathNavigator xmlNode, XmlDocument xmlParentDocument)
        {
            if (xmlNode == null || xmlParentDocument == null)
                return null;
            XmlNodeType eNodeType;
            switch (xmlNode.NodeType)
            {
                case XPathNodeType.Root:
                    eNodeType = XmlNodeType.Document;
                    break;

                case XPathNodeType.Element:
                    eNodeType = XmlNodeType.Element;
                    break;

                case XPathNodeType.Attribute:
                    eNodeType = XmlNodeType.Attribute;
                    break;

                case XPathNodeType.Namespace:
                    eNodeType = XmlNodeType.XmlDeclaration;
                    break;

                case XPathNodeType.Text:
                    eNodeType = XmlNodeType.Text;
                    break;

                case XPathNodeType.SignificantWhitespace:
                    eNodeType = XmlNodeType.SignificantWhitespace;
                    break;

                case XPathNodeType.Whitespace:
                    eNodeType = XmlNodeType.Whitespace;
                    break;

                case XPathNodeType.ProcessingInstruction:
                    eNodeType = XmlNodeType.ProcessingInstruction;
                    break;

                case XPathNodeType.Comment:
                    eNodeType = XmlNodeType.Comment;
                    break;

                case XPathNodeType.All:
                    eNodeType = XmlNodeType.None;
                    Debug.Assert(false);
                    break;

                default:
                    throw new InvalidOperationException(nameof(xmlNode.NodeType));
            }
            XmlNode xmlReturn = xmlParentDocument.CreateNode(eNodeType, xmlNode.Prefix, xmlNode.Name, xmlNode.NamespaceURI);
            xmlReturn.InnerXml = xmlNode.InnerXml;
            return xmlReturn;
        }

        /// <summary>
        /// Selects a single node using the specified XPath expression, but also caches that expression in case the same expression is used over and over.
        /// Effectively a version of SelectSingleNode(string xpath) that is slower on the first run (and consumes some memory), but faster on subsequent runs.
        /// Only use this if there's a particular XPath expression that keeps being used over and over.
        /// </summary>
        public static XPathNavigator SelectSingleNodeAndCacheExpression(this XPathNavigator xmlNode, string xpath, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            XPathExpression objExpression = XmlUtilities.CacheExpression(xpath);
            token.ThrowIfCancellationRequested();
            return xmlNode.SelectSingleNode(objExpression);
        }

        /// <summary>
        /// Selects a node set using the specified XPath expression, but also caches that expression in case the same expression is used over and over.
        /// Effectively a version of Select(string xpath) that is slower on the first run (and consumes some memory), but faster on subsequent runs.
        /// Only use this if there's a particular XPath expression that keeps being used over and over.
        /// </summary>
        public static XPathNodeIterator SelectAndCacheExpression(this XPathNavigator xmlNode, string xpath, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            XPathExpression objExpression = XmlUtilities.CacheExpression(xpath);
            token.ThrowIfCancellationRequested();
            return xmlNode.Select(objExpression);
        }
    }
}
