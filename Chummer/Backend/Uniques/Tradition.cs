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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Chummer.Annotations;
using Chummer.Backend.Attributes;

namespace Chummer.Backend.Uniques
{
    public enum TraditionType
    {
        None,
        MAG,
        RES
    }

    /// <summary>
    /// A Tradition
    /// </summary>
    [HubClassTag("SourceID", true, "Name", "Extra")]
    public sealed class Tradition : IHasInternalId, IHasName, IHasSourceId, IHasXmlDataNode, IHasSource, INotifyMultiplePropertiesChangedAsync, IHasLockObject, IHasCharacterObject
    {
        private Guid _guiID;
        private Guid _guiSourceID;
        private string _strName = string.Empty;
        private string _strExtra = string.Empty;
        private string _strSource = string.Empty;
        private string _strPage = string.Empty;
        private string _strDrainExpression = string.Empty;
        private string _strSpiritForm = "Materialization";
        private string _strSpiritCombat = string.Empty;
        private string _strSpiritDetection = string.Empty;
        private string _strSpiritHealth = string.Empty;
        private string _strSpiritIllusion = string.Empty;
        private string _strSpiritManipulation = string.Empty;
        private string _strNotes = string.Empty;
        private readonly List<string> _lstAvailableSpirits = new List<string>(5);
        private XmlNode _nodBonus;
        private TraditionType _eTraditionType = TraditionType.None;

        private readonly Character _objCharacter;

        public Character CharacterObject => _objCharacter; // readonly member, no locking needed

        #region Constructor, Create, Save, Load, and Print Methods

        public Tradition(Character objCharacter)
        {
            // Create the GUID for the new piece of Cyberware.
            _guiID = Guid.NewGuid();
            _objCharacter = objCharacter ?? throw new ArgumentNullException(nameof(objCharacter));
            LockObject = objCharacter.LockObject;
            objCharacter.MultiplePropertiesChangedAsync += RefreshDrainExpression;
        }

        public override string ToString()
        {
            using (LockObject.EnterReadLock())
                return !string.IsNullOrEmpty(_strName) ? _strName : base.ToString();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync().ConfigureAwait(false);
            try
            {
                if (_objCharacter != null)
                {
                    try
                    {
                        _objCharacter.MultiplePropertiesChangedAsync -= RefreshDrainExpression;
                    }
                    catch (ObjectDisposedException)
                    {
                        //swallow this
                    }
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            using (LockObject.EnterWriteLock())
            {
                if (_objCharacter != null)
                {
                    try
                    {
                        _objCharacter.MultiplePropertiesChangedAsync -= RefreshDrainExpression;
                    }
                    catch (ObjectDisposedException)
                    {
                        //swallow this
                    }
                }
            }
        }

        public void ResetTradition()
        {
            using (LockObject.EnterWriteLock())
            {
                ImprovementManager.RemoveImprovements(_objCharacter, Improvement.ImprovementSource.Tradition,
                                                      InternalId);
                Bonus = null;
                Name = string.Empty;
                Extra = string.Empty;
                Source = string.Empty;
                _strPage = string.Empty;
                DrainExpression = string.Empty;
                SpiritForm = "Materialization";
                _lstAvailableSpirits.Clear();
                Type = TraditionType.None;
                _objCachedSourceDetail = default;
            }
        }

        public async Task ResetTraditionAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                await ImprovementManager
                      .RemoveImprovementsAsync(_objCharacter, Improvement.ImprovementSource.Tradition, InternalId,
                                               token).ConfigureAwait(false);
                Bonus = null;
                Name = string.Empty;
                Extra = string.Empty;
                Source = string.Empty;
                _strPage = string.Empty;
                await SetDrainExpressionAsync(string.Empty, token).ConfigureAwait(false);
                SpiritForm = "Materialization";
                _lstAvailableSpirits.Clear();
                Type = TraditionType.None;
                _objCachedSourceDetail = default;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Create a Tradition from an XmlNode.
        /// </summary>
        /// <param name="xmlTraditionNode">XmlNode to create the object from.</param>
        /// <param name="blnIsTechnomancerTradition">Whether this tradition is for a technomancer.</param>
        /// <param name="strForcedValue">Value to forcefully select for any ImprovementManager prompts.</param>
        public bool Create(XmlNode xmlTraditionNode, bool blnIsTechnomancerTradition = false, string strForcedValue = "")
        {
            using (LockObject.EnterWriteLock())
            {
                ResetTradition();
                Type = blnIsTechnomancerTradition ? TraditionType.RES : TraditionType.MAG;
                if (xmlTraditionNode.TryGetField("id", out _guiSourceID))
                {
                    _xmlCachedMyXmlNode = null;
                    _objCachedMyXPathNode = null;
                }

                xmlTraditionNode.TryGetStringFieldQuickly("name", ref _strName);
                xmlTraditionNode.TryGetStringFieldQuickly("source", ref _strSource);
                xmlTraditionNode.TryGetStringFieldQuickly("page", ref _strPage);
                string strTemp = string.Empty;
                if (xmlTraditionNode.TryGetStringFieldQuickly("drain", ref strTemp))
                    DrainExpression = strTemp;
                if (xmlTraditionNode.TryGetStringFieldQuickly("spiritform", ref strTemp))
                    SpiritForm = strTemp;
                _nodBonus = xmlTraditionNode["bonus"];
                if (_nodBonus != null)
                {
                    string strOldFocedValue = ImprovementManager.ForcedValue;
                    string strOldSelectedValue = ImprovementManager.SelectedValue;
                    ImprovementManager.ForcedValue = strForcedValue;
                    if (!ImprovementManager.CreateImprovements(_objCharacter, Improvement.ImprovementSource.Tradition,
                            InternalId, _nodBonus,
                            strFriendlyName: CurrentDisplayNameShort))
                    {
                        ImprovementManager.ForcedValue = strOldFocedValue;
                        return false;
                    }

                    if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue))
                    {
                        _strExtra = ImprovementManager.SelectedValue;
                    }

                    ImprovementManager.ForcedValue = strOldFocedValue;
                    ImprovementManager.SelectedValue = strOldSelectedValue;
                }

                if (GlobalSettings.InsertPdfNotesIfAvailable && string.IsNullOrEmpty(Notes))
                {
                    Notes = CommonFunctions.GetBookNotes(xmlTraditionNode, Name, CurrentDisplayName, Source, Page,
                        DisplayPage(GlobalSettings.Language), _objCharacter);
                }

                RebuildSpiritList(false);
                this.OnMultiplePropertyChanged(nameof(Name), nameof(Extra), nameof(Source), nameof(Page), nameof(Bonus),
                    nameof(AvailableSpirits), nameof(SpiritCombat), nameof(SpiritDetection), nameof(SpiritHealth),
                    nameof(SpiritIllusion), nameof(SpiritManipulation));
                return true;
            }
        }

        /// <summary>
        /// Create a Tradition from an XmlNode.
        /// </summary>
        /// <param name="xmlTraditionNode">XmlNode to create the object from.</param>
        /// <param name="blnIsTechnomancerTradition">Whether this tradition is for a technomancer.</param>
        /// <param name="strForcedValue">Value to forcefully select for any ImprovementManager prompts.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task<bool> CreateAsync(XmlNode xmlTraditionNode, bool blnIsTechnomancerTradition = false, string strForcedValue = "", CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                await ResetTraditionAsync(token).ConfigureAwait(false);
                Type = blnIsTechnomancerTradition ? TraditionType.RES : TraditionType.MAG;
                if (xmlTraditionNode.TryGetField("id", out _guiSourceID))
                {
                    _xmlCachedMyXmlNode = null;
                    _objCachedMyXPathNode = null;
                }

                xmlTraditionNode.TryGetStringFieldQuickly("name", ref _strName);
                xmlTraditionNode.TryGetStringFieldQuickly("source", ref _strSource);
                xmlTraditionNode.TryGetStringFieldQuickly("page", ref _strPage);
                string strTemp = string.Empty;
                if (xmlTraditionNode.TryGetStringFieldQuickly("drain", ref strTemp))
                    await SetDrainExpressionAsync(strTemp, token).ConfigureAwait(false);
                if (xmlTraditionNode.TryGetStringFieldQuickly("spiritform", ref strTemp))
                    SpiritForm = strTemp;
                _nodBonus = xmlTraditionNode["bonus"];
                if (_nodBonus != null)
                {
                    string strOldFocedValue = ImprovementManager.ForcedValue;
                    string strOldSelectedValue = ImprovementManager.SelectedValue;
                    ImprovementManager.ForcedValue = strForcedValue;
                    if (!await ImprovementManager.CreateImprovementsAsync(_objCharacter,
                            Improvement.ImprovementSource.Tradition,
                            InternalId, _nodBonus,
                            strFriendlyName: await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false), token: token).ConfigureAwait(false))
                    {
                        ImprovementManager.ForcedValue = strOldFocedValue;
                        return false;
                    }

                    if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue))
                    {
                        _strExtra = ImprovementManager.SelectedValue;
                    }

                    ImprovementManager.ForcedValue = strOldFocedValue;
                    ImprovementManager.SelectedValue = strOldSelectedValue;
                }

                if (GlobalSettings.InsertPdfNotesIfAvailable && string.IsNullOrEmpty(Notes))
                {
                    Notes = await CommonFunctions.GetBookNotesAsync(xmlTraditionNode, Name,
                        await GetCurrentDisplayNameAsync(token).ConfigureAwait(false), Source, Page,
                        await DisplayPageAsync(GlobalSettings.Language, token).ConfigureAwait(false), _objCharacter, token).ConfigureAwait(false);
                }

                await RebuildSpiritListAsync(false, token).ConfigureAwait(false);
                await this.OnMultiplePropertyChangedAsync(token, nameof(Name), nameof(Extra), nameof(Source),
                    nameof(Page), nameof(Bonus), nameof(AvailableSpirits), nameof(SpiritCombat), nameof(SpiritDetection),
                    nameof(SpiritHealth), nameof(SpiritIllusion), nameof(SpiritManipulation)).ConfigureAwait(false);
                return true;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        public void RebuildSpiritList(bool blnDoOnPropertyChanged = true)
        {
            using (LockObject.EnterWriteLock())
            {
                _lstAvailableSpirits.Clear();
                _strSpiritCombat = string.Empty;
                _strSpiritDetection = string.Empty;
                _strSpiritHealth = string.Empty;
                _strSpiritIllusion = string.Empty;
                _strSpiritManipulation = string.Empty;
                if (Type != TraditionType.None)
                {
                    XPathNavigator xmlSpiritListNode
                        = this.GetNodeXPath()?.SelectSingleNodeAndCacheExpression("spirits");
                    if (xmlSpiritListNode != null)
                    {
                        foreach (XPathNavigator xmlSpiritNode in xmlSpiritListNode.SelectAndCacheExpression("spirit"))
                        {
                            _lstAvailableSpirits.Add(xmlSpiritNode.Value);
                        }

                        XPathNavigator xmlCombatSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spiritcombat");
                        if (xmlCombatSpiritNode != null)
                            _strSpiritCombat = xmlCombatSpiritNode.Value;
                        XPathNavigator xmlDetectionSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spiritdetection");
                        if (xmlDetectionSpiritNode != null)
                            _strSpiritDetection = xmlDetectionSpiritNode.Value;
                        XPathNavigator xmlHealthSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spirithealth");
                        if (xmlHealthSpiritNode != null)
                            _strSpiritHealth = xmlHealthSpiritNode.Value;
                        XPathNavigator xmlIllusionSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spiritillusion");
                        if (xmlIllusionSpiritNode != null)
                            _strSpiritIllusion = xmlIllusionSpiritNode.Value;
                        XPathNavigator xmlManipulationSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spiritmanipulation");
                        if (xmlManipulationSpiritNode != null)
                            _strSpiritManipulation = xmlManipulationSpiritNode.Value;
                    }
                }

                if (blnDoOnPropertyChanged)
                {
                    this.OnMultiplePropertyChanged(nameof(AvailableSpirits), nameof(SpiritCombat),
                                                   nameof(SpiritDetection), nameof(SpiritHealth),
                                                   nameof(SpiritIllusion), nameof(SpiritManipulation));
                }
            }
        }

        public async Task RebuildSpiritListAsync(bool blnDoOnPropertyChanged = true, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                _lstAvailableSpirits.Clear();
                _strSpiritCombat = string.Empty;
                _strSpiritDetection = string.Empty;
                _strSpiritHealth = string.Empty;
                _strSpiritIllusion = string.Empty;
                _strSpiritManipulation = string.Empty;
                if (Type != TraditionType.None)
                {
                    XPathNavigator xmlSpiritListNode
                        = (await this.GetNodeXPathAsync(token: token).ConfigureAwait(false))?.SelectSingleNodeAndCacheExpression("spirits", token);
                    if (xmlSpiritListNode != null)
                    {
                        foreach (XPathNavigator xmlSpiritNode in xmlSpiritListNode.SelectAndCacheExpression("spirit",
                                     token))
                        {
                            _lstAvailableSpirits.Add(xmlSpiritNode.Value);
                        }

                        XPathNavigator xmlCombatSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spiritcombat", token);
                        if (xmlCombatSpiritNode != null)
                            _strSpiritCombat = xmlCombatSpiritNode.Value;
                        XPathNavigator xmlDetectionSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spiritdetection", token);
                        if (xmlDetectionSpiritNode != null)
                            _strSpiritDetection = xmlDetectionSpiritNode.Value;
                        XPathNavigator xmlHealthSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spirithealth", token);
                        if (xmlHealthSpiritNode != null)
                            _strSpiritHealth = xmlHealthSpiritNode.Value;
                        XPathNavigator xmlIllusionSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spiritillusion", token);
                        if (xmlIllusionSpiritNode != null)
                            _strSpiritIllusion = xmlIllusionSpiritNode.Value;
                        XPathNavigator xmlManipulationSpiritNode
                            = xmlSpiritListNode.SelectSingleNodeAndCacheExpression("spiritmanipulation", token);
                        if (xmlManipulationSpiritNode != null)
                            _strSpiritManipulation = xmlManipulationSpiritNode.Value;
                    }
                }

                if (blnDoOnPropertyChanged)
                {
                    await this.OnMultiplePropertyChangedAsync(token, nameof(AvailableSpirits), nameof(SpiritCombat),
                        nameof(SpiritDetection), nameof(SpiritHealth),
                        nameof(SpiritIllusion), nameof(SpiritManipulation)).ConfigureAwait(false);
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlWriter objWriter)
        {
            if (objWriter == null)
                return;
            using (LockObject.EnterReadLock())
            {
                if (_eTraditionType == TraditionType.None)
                    return;
                objWriter.WriteStartElement("tradition");
                objWriter.WriteElementString("sourceid", SourceIDString);
                objWriter.WriteElementString("guid", InternalId);
                objWriter.WriteElementString("traditiontype", _eTraditionType.ToString());
                objWriter.WriteElementString("name", _strName);
                objWriter.WriteElementString("extra", _strExtra);
                objWriter.WriteElementString("spiritform", _strSpiritForm);
                objWriter.WriteElementString("drain", _strDrainExpression);
                objWriter.WriteElementString("source", _strSource);
                objWriter.WriteElementString("page", _strPage);
                objWriter.WriteElementString("spiritcombat", _strSpiritCombat);
                objWriter.WriteElementString("spiritdetection", _strSpiritDetection);
                objWriter.WriteElementString("spirithealth", _strSpiritHealth);
                objWriter.WriteElementString("spiritillusion", _strSpiritIllusion);
                objWriter.WriteElementString("spiritmanipulation", _strSpiritManipulation);
                objWriter.WriteStartElement("spirits");
                foreach (string strSpirit in _lstAvailableSpirits)
                {
                    objWriter.WriteElementString("spirit", strSpirit);
                }

                objWriter.WriteEndElement();
                if (_nodBonus != null)
                    objWriter.WriteRaw(_nodBonus.OuterXml);
                else
                    objWriter.WriteElementString("bonus", string.Empty);
                objWriter.WriteEndElement();
            }
        }

        /// <summary>
        /// Load the Tradition from the XmlNode.
        /// </summary>
        /// <param name="xmlNode">XmlNode to load.</param>
        public void Load(XmlNode xmlNode)
        {
            using (LockObject.EnterWriteLock())
            {
                string strTemp = string.Empty;
                if (!xmlNode.TryGetStringFieldQuickly("traditiontype", ref strTemp)
                    || !Enum.TryParse(strTemp, out _eTraditionType))
                {
                    _eTraditionType = TraditionType.None;
                    return;
                }

                if (!xmlNode.TryGetField("guid", out _guiID))
                {
                    _guiID = Guid.NewGuid();
                }

                xmlNode.TryGetStringFieldQuickly("name", ref _strName);
                Lazy<XPathNavigator> objMyNode = new Lazy<XPathNavigator>(() => this.GetNodeXPath());
                if (!xmlNode.TryGetFieldUninitialized("sourceid", ref _guiSourceID)
                    && !xmlNode.TryGetFieldUninitialized("id", ref _guiSourceID))
                {
                    objMyNode.Value?.TryGetFieldUninitialized("id", ref _guiSourceID);
                }

                xmlNode.TryGetStringFieldQuickly("extra", ref _strExtra);
                xmlNode.TryGetStringFieldQuickly("spiritform", ref _strSpiritForm);
                if (!xmlNode.TryGetStringFieldQuickly("drain", ref _strDrainExpression))
                    objMyNode.Value?.TryGetStringFieldQuickly("drain", ref _strDrainExpression);
                // Legacy catch for if a drain expression is not empty but has no attributes associated with it.
                if (_objCharacter.LastSavedVersion < new Version(5, 214, 77) &&
                    !string.IsNullOrEmpty(_strDrainExpression) && !_strDrainExpression.Contains('{') &&
                    AttributeSection.AttributeStrings.Any(x => _strDrainExpression.Contains(x)))
                {
                    if (IsCustomTradition)
                    {
                        foreach (string strAttribute in AttributeSection.AttributeStrings)
                            _strDrainExpression = _strDrainExpression.Replace(strAttribute, '{' + strAttribute + '}');
                        _strDrainExpression = _strDrainExpression.Replace("{MAG}Adept", "{MAGAdept}");
                    }
                    else
                        objMyNode.Value?.TryGetStringFieldQuickly("drain", ref _strDrainExpression);
                }

                xmlNode.TryGetStringFieldQuickly("source", ref _strSource);
                xmlNode.TryGetStringFieldQuickly("page", ref _strPage);
                xmlNode.TryGetStringFieldQuickly("spiritcombat", ref _strSpiritCombat);
                xmlNode.TryGetStringFieldQuickly("spiritdetection", ref _strSpiritDetection);
                xmlNode.TryGetStringFieldQuickly("spirithealth", ref _strSpiritHealth);
                xmlNode.TryGetStringFieldQuickly("spiritillusion", ref _strSpiritIllusion);
                xmlNode.TryGetStringFieldQuickly("spiritmanipulation", ref _strSpiritManipulation);
                using (XmlNodeList xmlSpiritList = xmlNode.SelectNodes("spirits/spirit"))
                {
                    if (xmlSpiritList?.Count > 0)
                    {
                        foreach (XmlNode xmlSpiritNode in xmlSpiritList)
                        {
                            _lstAvailableSpirits.Add(xmlSpiritNode.InnerText);
                        }
                    }
                }

                _nodBonus = xmlNode["bonus"];
            }
        }

        /// <summary>
        /// Load the Tradition from the XmlNode using old data saved before traditions had their own class.
        /// </summary>
        /// <param name="xpathCharacterNode">XPathNavigator of the Character from which to load.</param>
        public void LegacyLoad(XPathNavigator xpathCharacterNode)
        {
            using (LockObject.EnterWriteLock())
            {
                bool blnDoDrainSweep;
                if (_eTraditionType == TraditionType.RES)
                {
                    xpathCharacterNode.TryGetStringFieldQuickly("stream", ref _strName);
                    blnDoDrainSweep
                        = xpathCharacterNode.TryGetStringFieldQuickly("streamfading", ref _strDrainExpression);
                }
                else
                {
                    if (IsCustomTradition)
                    {
                        xpathCharacterNode.TryGetStringFieldQuickly("traditionname", ref _strName);
                        xpathCharacterNode.TryGetStringFieldQuickly("spiritcombat", ref _strSpiritCombat);
                        xpathCharacterNode.TryGetStringFieldQuickly("spiritdetection", ref _strSpiritDetection);
                        xpathCharacterNode.TryGetStringFieldQuickly("spirithealth", ref _strSpiritHealth);
                        xpathCharacterNode.TryGetStringFieldQuickly("spiritillusion", ref _strSpiritIllusion);
                        xpathCharacterNode.TryGetStringFieldQuickly("spiritmanipulation", ref _strSpiritManipulation);
                    }
                    else
                        xpathCharacterNode.TryGetStringFieldQuickly("tradition", ref _strName);

                    blnDoDrainSweep
                        = xpathCharacterNode.TryGetStringFieldQuickly("traditiondrain", ref _strDrainExpression);
                }

                if (blnDoDrainSweep)
                {
                    foreach (string strAttribute in AttributeSection.AttributeStrings)
                        _strDrainExpression = _strDrainExpression.Replace(strAttribute, '{' + strAttribute + '}');
                    _strDrainExpression = _strDrainExpression.Replace("{MAG}Adept", "{MAGAdept}");
                }
            }
        }

        /// <summary>
        /// Load the Tradition from the XmlNode using old data saved before traditions had their own class.
        /// </summary>
        /// <param name="xpathCharacterNode">XPathNavigator of the Character from which to load.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task LegacyLoadAsync(XPathNavigator xpathCharacterNode, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                bool blnDoDrainSweep;
                if (_eTraditionType == TraditionType.RES)
                {
                    xpathCharacterNode.TryGetStringFieldQuickly("stream", ref _strName);
                    blnDoDrainSweep
                        = xpathCharacterNode.TryGetStringFieldQuickly("streamfading", ref _strDrainExpression);
                }
                else
                {
                    if (await GetIsCustomTraditionAsync(token).ConfigureAwait(false))
                    {
                        xpathCharacterNode.TryGetStringFieldQuickly("traditionname", ref _strName);
                        xpathCharacterNode.TryGetStringFieldQuickly("spiritcombat", ref _strSpiritCombat);
                        xpathCharacterNode.TryGetStringFieldQuickly("spiritdetection", ref _strSpiritDetection);
                        xpathCharacterNode.TryGetStringFieldQuickly("spirithealth", ref _strSpiritHealth);
                        xpathCharacterNode.TryGetStringFieldQuickly("spiritillusion", ref _strSpiritIllusion);
                        xpathCharacterNode.TryGetStringFieldQuickly("spiritmanipulation", ref _strSpiritManipulation);
                    }
                    else
                        xpathCharacterNode.TryGetStringFieldQuickly("tradition", ref _strName);

                    blnDoDrainSweep
                        = xpathCharacterNode.TryGetStringFieldQuickly("traditiondrain", ref _strDrainExpression);
                }

                if (blnDoDrainSweep)
                {
                    foreach (string strAttribute in AttributeSection.AttributeStrings)
                        _strDrainExpression = _strDrainExpression.Replace(strAttribute, '{' + strAttribute + '}');
                    _strDrainExpression = _strDrainExpression.Replace("{MAG}Adept", "{MAGAdept}");
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        public void LoadFromHeroLab(XPathNavigator xmlHeroLabNode)
        {
            if (xmlHeroLabNode == null)
                return;
            using (LockObject.EnterWriteLock())
            {
                _eTraditionType = TraditionType.MAG;
                _strName = xmlHeroLabNode.SelectSingleNodeAndCacheExpression("@name")?.Value;
                XmlNode xmlTraditionDataNode = !string.IsNullOrEmpty(_strName)
                    ? _objCharacter.LoadData("traditions.xml")
                                   .TryGetNodeByNameOrId("/chummer/traditions/tradition", _strName)
                    : null;
                if (xmlTraditionDataNode?.TryGetField("id", out _guiSourceID) != true)
                {
                    _guiSourceID = new Guid(CustomMagicalTraditionGuid);
                    xmlTraditionDataNode = this.GetNode();
                }

                Create(xmlTraditionDataNode);
                if (IsCustomTradition)
                {
                    _strSpiritCombat = xmlHeroLabNode.SelectSingleNodeAndCacheExpression("@combatspirits")?.Value;
                    _strSpiritDetection = xmlHeroLabNode.SelectSingleNodeAndCacheExpression("@detectionspirits")?.Value;
                    _strSpiritHealth = xmlHeroLabNode.SelectSingleNodeAndCacheExpression("@healthspirits")?.Value;
                    _strSpiritIllusion = xmlHeroLabNode.SelectSingleNodeAndCacheExpression("@illusionspirits")?.Value;
                    _strSpiritManipulation = xmlHeroLabNode.SelectSingleNodeAndCacheExpression("@manipulationspirits")
                                                           ?.Value;
                }
            }
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        /// <param name="objCulture">Culture in which to print.</param>
        /// <param name="strLanguageToPrint">Language in which to print</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task Print(XmlWriter objWriter, CultureInfo objCulture, string strLanguageToPrint, CancellationToken token = default)
        {
            if (objWriter == null)
                return;
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                // <tradition>
                XmlElementWriteHelper objBaseElement
                    = await objWriter.StartElementAsync("tradition", token).ConfigureAwait(false);
                try
                {
                    await objWriter.WriteElementStringAsync("guid", InternalId, token).ConfigureAwait(false);
                    await objWriter.WriteElementStringAsync("sourceid", SourceIDString, token).ConfigureAwait(false);
                    await objWriter.WriteElementStringAsync("istechnomancertradition",
                            (Type == TraditionType.RES).ToString(
                                GlobalSettings.InvariantCultureInfo), token)
                        .ConfigureAwait(false);
                    await objWriter
                        .WriteElementStringAsync(
                            "name", await DisplayNameShortAsync(strLanguageToPrint, token).ConfigureAwait(false),
                            token)
                        .ConfigureAwait(false);
                    await objWriter
                        .WriteElementStringAsync(
                            "fullname", await DisplayNameAsync(strLanguageToPrint, token).ConfigureAwait(false),
                            token)
                        .ConfigureAwait(false);
                    await objWriter.WriteElementStringAsync("name_english", Name, token).ConfigureAwait(false);
                    await objWriter
                        .WriteElementStringAsync(
                            "extra",
                            await _objCharacter.TranslateExtraAsync(Extra, strLanguageToPrint, token: token)
                                .ConfigureAwait(false), token).ConfigureAwait(false);
                    if (Type == TraditionType.MAG)
                    {
                        await objWriter
                            .WriteElementStringAsync("spiritcombat",
                                await DisplaySpiritCombatMethodAsync(strLanguageToPrint, token)
                                    .ConfigureAwait(false), token).ConfigureAwait(false);
                        await objWriter
                            .WriteElementStringAsync("spiritdetection",
                                await DisplaySpiritDetectionMethodAsync(
                                        strLanguageToPrint, token)
                                    .ConfigureAwait(false), token).ConfigureAwait(false);
                        await objWriter
                            .WriteElementStringAsync("spirithealth",
                                await DisplaySpiritHealthMethodAsync(strLanguageToPrint, token)
                                    .ConfigureAwait(false), token).ConfigureAwait(false);
                        await objWriter
                            .WriteElementStringAsync("spiritillusion",
                                await DisplaySpiritIllusionMethodAsync(strLanguageToPrint, token)
                                    .ConfigureAwait(false), token).ConfigureAwait(false);
                        await objWriter
                            .WriteElementStringAsync("spiritmanipulation",
                                await DisplaySpiritManipulationMethodAsync(
                                        strLanguageToPrint, token)
                                    .ConfigureAwait(false), token).ConfigureAwait(false);
                        await objWriter
                            .WriteElementStringAsync("spiritform",
                                await DisplaySpiritFormAsync(strLanguageToPrint, token)
                                    .ConfigureAwait(false), token).ConfigureAwait(false);
                    }

                    await objWriter
                        .WriteElementStringAsync("drainattributes",
                            await DisplayDrainExpressionMethodAsync(
                                objCulture, strLanguageToPrint, token).ConfigureAwait(false),
                            token)
                        .ConfigureAwait(false);
                    await objWriter.WriteElementStringAsync("drainvalue", DrainValue.ToString(objCulture), token)
                        .ConfigureAwait(false);
                    await objWriter
                        .WriteElementStringAsync(
                            "source",
                            await _objCharacter.LanguageBookShortAsync(Source, strLanguageToPrint, token)
                                .ConfigureAwait(false), token).ConfigureAwait(false);
                    await objWriter
                        .WriteElementStringAsync(
                            "page", await DisplayPageAsync(strLanguageToPrint, token).ConfigureAwait(false), token)
                        .ConfigureAwait(false);
                }
                finally
                {
                    // </tradition>
                    await objBaseElement.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Constructor, Create, Save, Load, and Print Methods

        #region Properties

        /// <summary>
        /// Identifier of the object within data files.
        /// </summary>
        public Guid SourceID
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _guiSourceID;
            }
        }

        /// <summary>
        /// String-formatted identifier of the <inheritdoc cref="SourceID"/> from the data files.
        /// </summary>
        public string SourceIDString
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return Type == TraditionType.None
                        ? string.Empty
                        : _guiSourceID.ToString("D", GlobalSettings.InvariantCultureInfo);
            }
        }

        /// <summary>
        /// String-formatted identifier of the <inheritdoc cref="SourceID"/> from the data files.
        /// </summary>
        public async Task<string> GetSourceIDStringAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return await GetTypeAsync(token).ConfigureAwait(false) == TraditionType.None
                    ? string.Empty
                    : _guiSourceID.ToString("D", GlobalSettings.InvariantCultureInfo);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Internal identifier which will be used to identify this Tradition in the Improvement system.
        /// </summary>
        public string InternalId
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _guiID.ToString("D", GlobalSettings.InvariantCultureInfo);
            }
        }

        private SourceString _objCachedSourceDetail;

        public SourceString SourceDetail
        {
            get
            {
                using (LockObject.EnterReadLock())
                {
                    return _objCachedSourceDetail == default
                        ? _objCachedSourceDetail = SourceString.GetSourceString(Source,
                            DisplayPage(GlobalSettings.Language),
                            GlobalSettings.Language,
                            GlobalSettings.CultureInfo,
                            _objCharacter)
                        : _objCachedSourceDetail;
                }
            }
        }

        public async Task<SourceString> GetSourceDetailAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return _objCachedSourceDetail == default
                    ? _objCachedSourceDetail = await SourceString.GetSourceStringAsync(Source,
                        await DisplayPageAsync(GlobalSettings.Language, token).ConfigureAwait(false),
                        GlobalSettings.Language,
                        GlobalSettings.CultureInfo,
                        _objCharacter, token).ConfigureAwait(false)
                    : _objCachedSourceDetail;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Bonus node from the XML file.
        /// </summary>
        public XmlNode Bonus
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _nodBonus;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _nodBonus, value) == value)
                        return;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ImprovementSource Type.
        /// </summary>
        public TraditionType Type
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _eTraditionType;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (InterlockedExtensions.Exchange(ref _eTraditionType, value) == value)
                        return;
                    using (LockObject.EnterWriteLock())
                    {
                        _xmlCachedMyXmlNode = null;
                        _objCachedMyXPathNode = null;
                    }
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ImprovementSource Type.
        /// </summary>
        public async Task<TraditionType> GetTypeAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return _eTraditionType;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The GUID of the Custom entry in the Magical Tradition file
        /// </summary>
        public const string CustomMagicalTraditionGuid = "616ba093-306c-45fc-8f41-0b98c8cccb46";

        /// <summary>
        /// Whether a Tradition is a custom one (i.e. it has a custom name and custom spirit settings)
        /// </summary>
        public bool IsCustomTradition
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return string.Equals(SourceIDString, CustomMagicalTraditionGuid, StringComparison.OrdinalIgnoreCase);
                // TODO: If Custom Technomancer Tradition added to streams.xml, check for that GUID as well
            }
        }

        /// <summary>
        /// Whether a Tradition is a custom one (i.e. it has a custom name and custom spirit settings)
        /// </summary>
        public async Task<bool> GetIsCustomTraditionAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return string.Equals(await GetSourceIDStringAsync(token).ConfigureAwait(false),
                    CustomMagicalTraditionGuid, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        public bool CanChooseDrainAttribute
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return IsCustomTradition || string.IsNullOrEmpty(_strDrainExpression);
            }
        }

        public async Task<bool> GetCanChooseDrainAttributeAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return await GetIsCustomTraditionAsync(token).ConfigureAwait(false) || string.IsNullOrEmpty(_strDrainExpression);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Tradition name.
        /// </summary>
        public string Name
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _strName;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _strName, value) == value)
                        return;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Tradition name.
        /// </summary>
        public async Task<string> GetNameAsync(CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return _strName;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Tradition name.
        /// </summary>
        public async Task SetNameAsync(string value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (Interlocked.Exchange(ref _strName, value) != value)
                    await OnPropertyChangedAsync(nameof(Name), token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public string DisplayNameShort(string strLanguage)
        {
            using (LockObject.EnterReadLock())
            {
                if (IsCustomTradition)
                {
                    if (GlobalSettings.Language != strLanguage)
                    {
                        string strFile = string.Empty;
                        switch (Type)
                        {
                            case TraditionType.MAG:
                                strFile = "traditions.xml";
                                break;

                            case TraditionType.RES:
                                strFile = "streams.xml";
                                break;
                        }

                        string strReturnEnglish
                            = strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase)
                                ? Name
                                : _objCharacter.ReverseTranslateExtra(Name, GlobalSettings.DefaultLanguage, strFile);
                        return _objCharacter.TranslateExtra(strReturnEnglish, strLanguage);
                    }

                    return _objCharacter.TranslateExtra(Name, strLanguage);
                }

                // Get the translated name if applicable.
                if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                    return Name;

                return this.GetNodeXPath(strLanguage)?.SelectSingleNodeAndCacheExpression("translate")?.Value ?? Name;
            }
        }

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public async Task<string> DisplayNameShortAsync(string strLanguage, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (IsCustomTradition)
                {
                    if (GlobalSettings.Language != strLanguage)
                    {
                        string strFile = string.Empty;
                        switch (Type)
                        {
                            case TraditionType.MAG:
                                strFile = "traditions.xml";
                                break;

                            case TraditionType.RES:
                                strFile = "streams.xml";
                                break;
                        }

                        string strReturnEnglish
                            = strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase)
                                ? Name
                                : await _objCharacter.ReverseTranslateExtraAsync(
                                    Name, GlobalSettings.DefaultLanguage, strFile, token).ConfigureAwait(false);
                        return await _objCharacter.TranslateExtraAsync(strReturnEnglish, strLanguage, token: token)
                                                  .ConfigureAwait(false);
                    }

                    return await _objCharacter.TranslateExtraAsync(Name, strLanguage, token: token)
                                              .ConfigureAwait(false);
                }

                // Get the translated name if applicable.
                if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                    return Name;

                XPathNavigator objNode = await this.GetNodeXPathAsync(strLanguage, token: token).ConfigureAwait(false);
                return objNode != null
                    ? objNode.SelectSingleNodeAndCacheExpression("translate", token: token)?.Value ?? Name
                    : Name;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Name (Extra).
        /// </summary>
        public string DisplayName(string strLanguage)
        {
            using (LockObject.EnterReadLock())
            {
                string strReturn = DisplayNameShort(strLanguage);

                if (!string.IsNullOrEmpty(Extra))
                    strReturn += LanguageManager.GetString("String_Space", strLanguage) + '('
                        + _objCharacter.TranslateExtra(Extra, strLanguage) + ')';

                return strReturn;
            }
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Name (Extra).
        /// </summary>
        public async Task<string> DisplayNameAsync(string strLanguage, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                string strReturn = await DisplayNameShortAsync(strLanguage, token).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(Extra))
                    strReturn
                        += await LanguageManager.GetStringAsync("String_Space", strLanguage, token: token)
                                                .ConfigureAwait(false) + '(' + await _objCharacter
                            .TranslateExtraAsync(Extra, strLanguage, token: token).ConfigureAwait(false) + ')';

                return strReturn;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        public string CurrentDisplayName => DisplayName(GlobalSettings.Language);

        public Task<string> GetCurrentDisplayNameAsync(CancellationToken token = default) => DisplayNameAsync(GlobalSettings.Language, token);

        public string CurrentDisplayNameShort => DisplayNameShort(GlobalSettings.Language);

        public Task<string> GetCurrentDisplayNameShortAsync(CancellationToken token = default) => DisplayNameShortAsync(GlobalSettings.Language, token);

        /// <summary>
        /// What type of forms do spirits of these traditions come in? Defaults to Materialization.
        /// </summary>
        public string SpiritForm
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _strSpiritForm;
            }
            set
            {
                value = _objCharacter.ReverseTranslateExtra(value);
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _strSpiritForm, value) == value)
                        return;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The spirit form of the tradition as it should be displayed in printouts and the UI.
        /// </summary>
        public string DisplaySpiritForm(string strLanguage)
        {
            return _objCharacter.TranslateExtra(SpiritForm, strLanguage, "critterpowers.xml");
        }

        /// <summary>
        /// The spirit form of the tradition as it should be displayed in printouts and the UI.
        /// </summary>
        public Task<string> DisplaySpiritFormAsync(string strLanguage, CancellationToken token = default)
        {
            return _objCharacter.TranslateExtraAsync(SpiritForm, strLanguage, "critterpowers.xml", token);
        }

        /// <summary>
        /// Value that was selected during an ImprovementManager dialogue.
        /// </summary>
        public string Extra
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _strExtra;
            }
            set
            {
                value = _objCharacter.ReverseTranslateExtra(value);
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _strExtra, value) == value)
                        return;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Magician's Tradition Drain Attributes.
        /// </summary>
        public string DrainExpression
        {
            get
            {
                using (LockObject.EnterReadLock())
                {
                    if (_objCharacter.AdeptEnabled && !_objCharacter.MagicianEnabled)
                    {
                        return "{BOD} + {WIL}";
                    }

                    return _strDrainExpression;
                }
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    string strOldExpression = Interlocked.Exchange(ref _strDrainExpression, value);
                    if (strOldExpression == value)
                        return;
                    using (LockObject.EnterWriteLock())
                    {
                        foreach (string strAttribute in AttributeSection.AttributeStrings)
                        {
                            if (strOldExpression.Contains(strAttribute))
                            {
                                if (!value.Contains(strAttribute))
                                {
                                    CharacterAttrib objAttrib = _objCharacter.GetAttribute(strAttribute);
                                    objAttrib.PropertyChangedAsync -= RefreshDrainValue;
                                }
                            }
                            else if (value.Contains(strAttribute))
                            {
                                CharacterAttrib objAttrib = _objCharacter.GetAttribute(strAttribute);
                                objAttrib.PropertyChangedAsync += RefreshDrainValue;
                            }
                        }
                    }

                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Magician's Tradition Drain Attributes.
        /// </summary>
        public async Task<string> GetDrainExpressionAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (await _objCharacter.GetAdeptEnabledAsync(token).ConfigureAwait(false) &&
                    !await _objCharacter.GetMagicianEnabledAsync(token).ConfigureAwait(false))
                {
                    return "{BOD} + {WIL}";
                }

                return _strDrainExpression;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Magician's Tradition Drain Attributes.
        /// </summary>
        public async Task SetDrainExpressionAsync(string value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                string strOldExpression = Interlocked.Exchange(ref _strDrainExpression, value);
                if (strOldExpression == value)
                    return;
                IAsyncDisposable objLocker2 = await LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    foreach (string strAttribute in AttributeSection.AttributeStrings)
                    {
                        if (strOldExpression.Contains(strAttribute))
                        {
                            if (!value.Contains(strAttribute))
                            {
                                CharacterAttrib objAttrib = await _objCharacter
                                    .GetAttributeAsync(strAttribute, token: token).ConfigureAwait(false);
                                objAttrib.PropertyChangedAsync -= RefreshDrainValue;
                            }
                        }
                        else if (value.Contains(strAttribute))
                        {
                            CharacterAttrib objAttrib = await _objCharacter
                                .GetAttributeAsync(strAttribute, token: token).ConfigureAwait(false);
                            objAttrib.PropertyChangedAsync += RefreshDrainValue;
                        }
                    }
                }
                finally
                {
                    await objLocker2.DisposeAsync().ConfigureAwait(false);
                }

                await OnPropertyChangedAsync(nameof(DisplayDrainExpression), token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Magician's Tradition Drain Attributes for display purposes.
        /// </summary>
        public string DisplayDrainExpression => DisplayDrainExpressionMethod(GlobalSettings.CultureInfo, GlobalSettings.Language);

        /// <summary>
        /// Magician's Tradition Drain Attributes for display purposes.
        /// </summary>
        public Task<string> GetDisplayDrainExpressionAsync(CancellationToken token = default) =>
            DisplayDrainExpressionMethodAsync(GlobalSettings.CultureInfo, GlobalSettings.Language, token);

        /// <summary>
        /// Magician's Tradition Drain Attributes for display purposes.
        /// </summary>
        public string DisplayDrainExpressionMethod(CultureInfo objCultureInfo, string strLanguage)
        {
            return _objCharacter.AttributeSection.ProcessAttributesInXPathForTooltip(DrainExpression, objCultureInfo, strLanguage, false);
        }

        /// <summary>
        /// Magician's Tradition Drain Attributes for display purposes.
        /// </summary>
        public Task<string> DisplayDrainExpressionMethodAsync(CultureInfo objCultureInfo, string strLanguage, CancellationToken token = default)
        {
            return _objCharacter.AttributeSection.ProcessAttributesInXPathForTooltipAsync(DrainExpression, objCultureInfo, strLanguage, false, token: token);
        }

        /// <summary>
        /// Magician's total amount of dice for resisting drain.
        /// </summary>
        public int DrainValue
        {
            get
            {
                using (LockObject.EnterReadLock())
                {
                    if (Type == TraditionType.None)
                        return 0;
                    string strDrainAttributes = DrainExpression;
                    string strDrain;
                    using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                                                                  out StringBuilder sbdDrain))
                    {
                        sbdDrain.Append(strDrainAttributes);
                        _objCharacter.AttributeSection.ProcessAttributesInXPath(sbdDrain, strDrainAttributes);
                        strDrain = sbdDrain.ToString();
                    }

                    if (!decimal.TryParse(strDrain, out decimal decDrain))
                    {
                        (bool blnIsSuccess, object objProcess) = CommonFunctions.EvaluateInvariantXPath(strDrain);
                        if (blnIsSuccess)
                            decDrain = Convert.ToDecimal(objProcess, GlobalSettings.InvariantCultureInfo);
                    }

                    // Add any Improvements for Drain Resistance.
                    if (Type == TraditionType.RES)
                        decDrain += ImprovementManager.ValueOf(_objCharacter,
                                                               Improvement.ImprovementType.FadingResistance);
                    else
                        decDrain += ImprovementManager.ValueOf(_objCharacter,
                                                               Improvement.ImprovementType.DrainResistance);

                    return decDrain.StandardRound();
                }
            }
        }

        /// <summary>
        /// Magician's total amount of dice for resisting drain.
        /// </summary>
        public async Task<int> GetDrainValueAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                TraditionType eType = await GetTypeAsync(token).ConfigureAwait(false);
                if (eType == TraditionType.None)
                    return 0;
                string strDrainAttributes = await GetDrainExpressionAsync(token).ConfigureAwait(false);
                string strDrain;
                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                           out StringBuilder sbdDrain))
                {
                    sbdDrain.Append(strDrainAttributes);
                    await _objCharacter.AttributeSection
                        .ProcessAttributesInXPathAsync(sbdDrain, strDrainAttributes, token: token)
                        .ConfigureAwait(false);
                    strDrain = sbdDrain.ToString();
                }

                if (!decimal.TryParse(strDrain, out decimal decDrain))
                {
                    (bool blnIsSuccess, object objProcess) = await CommonFunctions
                        .EvaluateInvariantXPathAsync(strDrain, token).ConfigureAwait(false);
                    if (blnIsSuccess)
                        decDrain = Convert.ToDecimal(objProcess, GlobalSettings.InvariantCultureInfo);
                }

                // Add any Improvements for Drain Resistance.
                if (eType == TraditionType.RES)
                    decDrain += await ImprovementManager.ValueOfAsync(_objCharacter,
                        Improvement.ImprovementType.FadingResistance, token: token).ConfigureAwait(false);
                else
                    decDrain += await ImprovementManager.ValueOfAsync(_objCharacter,
                        Improvement.ImprovementType.DrainResistance, token: token).ConfigureAwait(false);

                return decDrain.StandardRound();
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        public string DrainValueToolTip
        {
            get
            {
                using (LockObject.EnterReadLock())
                {
                    if (Type == TraditionType.None)
                        return string.Empty;
                    string strSpace = LanguageManager.GetString("String_Space");
                    using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                                                                  out StringBuilder sbdToolTip))
                    {
                        sbdToolTip.Append(DrainExpression);
                        // Update the Fading CharacterAttribute Value.
                        _objCharacter.AttributeSection.ProcessAttributesInXPathForTooltip(sbdToolTip, DrainExpression);

                        List<Improvement> lstUsedImprovements
                            = ImprovementManager.GetCachedImprovementListForValueOf(
                                _objCharacter,
                                Type == TraditionType.RES
                                    ? Improvement.ImprovementType.FadingResistance
                                    : Improvement.ImprovementType.DrainResistance);
                        foreach (Improvement objLoopImprovement in lstUsedImprovements)
                        {
                            sbdToolTip.Append(strSpace).Append('+').Append(strSpace)
                                      .Append(_objCharacter.GetObjectName(objLoopImprovement)).Append(strSpace)
                                      .Append('(')
                                      .Append(objLoopImprovement.Value.ToString(GlobalSettings.CultureInfo))
                                      .Append(')');
                        }

                        return sbdToolTip.ToString();
                    }
                }
            }
        }

        public async Task<string> GetDrainValueToolTipAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                TraditionType eType = await GetTypeAsync(token).ConfigureAwait(false);
                if (eType == TraditionType.None)
                    return string.Empty;
                string strSpace = await LanguageManager.GetStringAsync("String_Space", token: token)
                    .ConfigureAwait(false);
                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                           out StringBuilder sbdToolTip))
                {
                    sbdToolTip.Append(DrainExpression);
                    // Update the Fading CharacterAttribute Value.
                    await _objCharacter.AttributeSection
                        .ProcessAttributesInXPathForTooltipAsync(sbdToolTip, DrainExpression, token: token)
                        .ConfigureAwait(false);

                    List<Improvement> lstUsedImprovements
                        = await ImprovementManager.GetCachedImprovementListForValueOfAsync(
                            _objCharacter,
                            eType == TraditionType.RES
                                ? Improvement.ImprovementType.FadingResistance
                                : Improvement.ImprovementType.DrainResistance, token: token).ConfigureAwait(false);
                    foreach (Improvement objLoopImprovement in lstUsedImprovements)
                    {
                        sbdToolTip.Append(strSpace).Append('+').Append(strSpace)
                            .Append(await _objCharacter.GetObjectNameAsync(objLoopImprovement, token: token)
                                .ConfigureAwait(false)).Append(strSpace)
                            .Append('(')
                            .Append(objLoopImprovement.Value.ToString(GlobalSettings.CultureInfo))
                            .Append(')');
                    }

                    return sbdToolTip.ToString();
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        public async Task RefreshDrainExpression(object sender, MultiplePropertiesChangedEventArgs e,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if ((e.PropertyNames.Contains(nameof(Character.AdeptEnabled)) ||
                 e.PropertyNames.Contains(nameof(Character.MagicianEnabled))) &&
                await GetTypeAsync(token).ConfigureAwait(false) == TraditionType.MAG)
                await OnPropertyChangedAsync(nameof(DrainExpression), token).ConfigureAwait(false);
        }

        public async Task RefreshDrainValue(object sender, PropertyChangedEventArgs e,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (e?.PropertyName == nameof(CharacterAttrib.TotalValue) &&
                await GetTypeAsync(token).ConfigureAwait(false) != TraditionType.None)
                await OnPropertyChangedAsync(nameof(DrainValue), token).ConfigureAwait(false);
        }

        public IReadOnlyList<string> AvailableSpirits => _lstAvailableSpirits;

        /// <summary>
        /// Magician's Combat Spirit (for Custom Traditions) in English.
        /// </summary>
        public string SpiritCombat
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return Type == TraditionType.None ? string.Empty : _strSpiritCombat;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None && Interlocked.Exchange(ref _strSpiritCombat, value) != value)
                        OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Magician's Combat Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task<string> GetSpiritCombatAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return await GetTypeAsync(token).ConfigureAwait(false) == TraditionType.None
                    ? string.Empty
                    : _strSpiritCombat;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Magician's Combat Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task SetSpiritCombatAsync(string value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (await GetTypeAsync(token).ConfigureAwait(false) != TraditionType.None &&
                    Interlocked.Exchange(ref _strSpiritCombat, value) != value)
                    await OnPropertyChangedAsync(nameof(SpiritCombat), token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Method to get Magician's Combat Spirit (for Custom Traditions) in a language.
        /// </summary>
        public string DisplaySpiritCombatMethod(string strLanguage)
        {
            string strSpirit = SpiritCombat;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetString("String_None", strLanguage)
                : _objCharacter.TranslateExtra(strSpirit, strLanguage, "critters.xml");
        }

        /// <summary>
        /// Method to get Magician's Combat Spirit (for Custom Traditions) in a language.
        /// </summary>
        public Task<string> DisplaySpiritCombatMethodAsync(string strLanguage, CancellationToken token = default)
        {
            string strSpirit = SpiritCombat;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetStringAsync("String_None", strLanguage, token: token)
                : _objCharacter.TranslateExtraAsync(strSpirit, strLanguage, "critters.xml", token);
        }

        /// <summary>
        /// Magician's Combat Spirit (for Custom Traditions) in the language of the current UI.
        /// </summary>
        public string DisplaySpiritCombat
        {
            get => DisplaySpiritCombatMethod(GlobalSettings.Language);
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None)
                    {
                        SpiritCombat = _objCharacter.ReverseTranslateExtra(value, GlobalSettings.Language, "critters.xml");
                    }
                }
            }
        }

        /// <summary>
        /// Magician's Detection Spirit (for Custom Traditions).
        /// </summary>
        public string SpiritDetection
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return Type == TraditionType.None ? string.Empty : _strSpiritDetection;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None && Interlocked.Exchange(ref _strSpiritDetection, value) != value)
                        OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Magician's Detection Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task<string> GetSpiritDetectionAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return await GetTypeAsync(token).ConfigureAwait(false) == TraditionType.None
                    ? string.Empty
                    : _strSpiritDetection;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Magician's Detection Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task SetSpiritDetectionAsync(string value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (await GetTypeAsync(token).ConfigureAwait(false) != TraditionType.None &&
                    Interlocked.Exchange(ref _strSpiritDetection, value) != value)
                    await OnPropertyChangedAsync(nameof(SpiritDetection), token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Method to get Magician's Detection Spirit (for Custom Traditions) in a language.
        /// </summary>
        public string DisplaySpiritDetectionMethod(string strLanguage)
        {
            string strSpirit = SpiritDetection;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetString("String_None", strLanguage)
                : _objCharacter.TranslateExtra(strSpirit, strLanguage, "critters.xml");
        }

        /// <summary>
        /// Method to get Magician's Detection Spirit (for Custom Traditions) in a language.
        /// </summary>
        public Task<string> DisplaySpiritDetectionMethodAsync(string strLanguage, CancellationToken token = default)
        {
            string strSpirit = SpiritDetection;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetStringAsync("String_None", strLanguage, token: token)
                : _objCharacter.TranslateExtraAsync(strSpirit, strLanguage, "critters.xml", token);
        }

        /// <summary>
        /// Magician's Detection Spirit (for Custom Traditions) in the language of the current UI.
        /// </summary>
        public string DisplaySpiritDetection
        {
            get => DisplaySpiritDetectionMethod(GlobalSettings.Language);
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None)
                        SpiritDetection
                            = _objCharacter.ReverseTranslateExtra(value, GlobalSettings.Language, "critters.xml");
                }
            }
        }

        /// <summary>
        /// Magician's Health Spirit (for Custom Traditions).
        /// </summary>
        public string SpiritHealth
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return Type == TraditionType.None ? string.Empty : _strSpiritHealth;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None && Interlocked.Exchange(ref _strSpiritHealth, value) != value)
                        OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Magician's Health Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task<string> GetSpiritHealthAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return await GetTypeAsync(token).ConfigureAwait(false) == TraditionType.None
                    ? string.Empty
                    : _strSpiritHealth;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Magician's Health Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task SetSpiritHealthAsync(string value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (await GetTypeAsync(token).ConfigureAwait(false) != TraditionType.None &&
                    Interlocked.Exchange(ref _strSpiritHealth, value) != value)
                    await OnPropertyChangedAsync(nameof(SpiritHealth), token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Method to get Magician's Health Spirit (for Custom Traditions) in a language.
        /// </summary>
        public string DisplaySpiritHealthMethod(string strLanguage)
        {
            string strSpirit = SpiritHealth;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetString("String_None", strLanguage)
                : _objCharacter.TranslateExtra(strSpirit, strLanguage, "critters.xml");
        }

        /// <summary>
        /// Method to get Magician's Health Spirit (for Custom Traditions) in a language.
        /// </summary>
        public Task<string> DisplaySpiritHealthMethodAsync(string strLanguage, CancellationToken token = default)
        {
            string strSpirit = SpiritHealth;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetStringAsync("String_None", strLanguage, token: token)
                : _objCharacter.TranslateExtraAsync(strSpirit, strLanguage, "critters.xml", token);
        }

        /// <summary>
        /// Magician's Health Spirit (for Custom Traditions) in the language of the current UI.
        /// </summary>
        public string DisplaySpiritHealth
        {
            get => DisplaySpiritHealthMethod(GlobalSettings.Language);
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None)
                        SpiritHealth
                            = _objCharacter.ReverseTranslateExtra(value, GlobalSettings.Language, "critters.xml");
                }
            }
        }

        /// <summary>
        /// Magician's Illusion Spirit (for Custom Traditions).
        /// </summary>
        public string SpiritIllusion
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return Type == TraditionType.None ? string.Empty : _strSpiritIllusion;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None && Interlocked.Exchange(ref _strSpiritIllusion, value) != value)
                        OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Magician's Illusion Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task<string> GetSpiritIllusionAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return await GetTypeAsync(token).ConfigureAwait(false) == TraditionType.None
                    ? string.Empty
                    : _strSpiritIllusion;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Magician's Illusion Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task SetSpiritIllusionAsync(string value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (await GetTypeAsync(token).ConfigureAwait(false) != TraditionType.None &&
                    Interlocked.Exchange(ref _strSpiritIllusion, value) != value)
                    await OnPropertyChangedAsync(nameof(SpiritIllusion), token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Method to get Magician's Illusion Spirit (for Custom Traditions) in a language.
        /// </summary>
        public string DisplaySpiritIllusionMethod(string strLanguage)
        {
            string strSpirit = SpiritIllusion;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetString("String_None", strLanguage)
                : _objCharacter.TranslateExtra(strSpirit, strLanguage, "critters.xml");
        }

        /// <summary>
        /// Method to get Magician's Illusion Spirit (for Custom Traditions) in a language.
        /// </summary>
        public Task<string> DisplaySpiritIllusionMethodAsync(string strLanguage, CancellationToken token = default)
        {
            string strSpirit = SpiritIllusion;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetStringAsync("String_None", strLanguage, token: token)
                : _objCharacter.TranslateExtraAsync(strSpirit, strLanguage, "critters.xml", token);
        }

        /// <summary>
        /// Magician's Illusion Spirit (for Custom Traditions) in the language of the current UI.
        /// </summary>
        public string DisplaySpiritIllusion
        {
            get => DisplaySpiritIllusionMethod(GlobalSettings.Language);
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None)
                        SpiritIllusion
                            = _objCharacter.ReverseTranslateExtra(value, GlobalSettings.Language, "critters.xml");
                }
            }
        }

        /// <summary>
        /// Magician's Manipulation Spirit (for Custom Traditions).
        /// </summary>
        public string SpiritManipulation
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return Type == TraditionType.None ? string.Empty : _strSpiritManipulation;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None && Interlocked.Exchange(ref _strSpiritManipulation, value) != value)
                        OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Magician's Manipulation Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task<string> GetSpiritManipulationAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                return await GetTypeAsync(token).ConfigureAwait(false) == TraditionType.None
                    ? string.Empty
                    : _strSpiritManipulation;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Magician's Manipulation Spirit (for Custom Traditions) in English.
        /// </summary>
        public async Task SetSpiritManipulationAsync(string value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (await GetTypeAsync(token).ConfigureAwait(false) != TraditionType.None &&
                    Interlocked.Exchange(ref _strSpiritManipulation, value) != value)
                    await OnPropertyChangedAsync(nameof(SpiritManipulation), token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Method to get Magician's Manipulation Spirit (for Custom Traditions) in a language.
        /// </summary>
        public string DisplaySpiritManipulationMethod(string strLanguage)
        {
            string strSpirit = SpiritManipulation;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetString("String_None", strLanguage)
                : _objCharacter.TranslateExtra(strSpirit, strLanguage, "critters.xml");
        }

        /// <summary>
        /// Method to get Magician's Manipulation Spirit (for Custom Traditions) in a language.
        /// </summary>
        public Task<string> DisplaySpiritManipulationMethodAsync(string strLanguage, CancellationToken token = default)
        {
            string strSpirit = SpiritManipulation;
            return string.IsNullOrEmpty(strSpirit)
                ? LanguageManager.GetStringAsync("String_None", strLanguage, token: token)
                : _objCharacter.TranslateExtraAsync(strSpirit, strLanguage, "critters.xml", token);
        }

        /// <summary>
        /// Magician's Manipulation Spirit (for Custom Traditions) in the language of the current UI.
        /// </summary>
        public string DisplaySpiritManipulation
        {
            get => DisplaySpiritManipulationMethod(GlobalSettings.Language);
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Type != TraditionType.None)
                        SpiritManipulation
                            = _objCharacter.ReverseTranslateExtra(value, GlobalSettings.Language, "critters.xml");
                }
            }
        }

        /// <summary>
        /// Sourcebook.
        /// </summary>
        public string Source
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _strSource;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _strSource, value) == value)
                        return;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Sourcebook Page Number.
        /// </summary>
        public string Page
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _strPage;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _strPage, value) == value)
                        return;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Description of the object.
        /// </summary>
        public string Notes
        {
            get
            {
                using (LockObject.EnterReadLock())
                    return _strNotes;
            }
            set
            {
                using (LockObject.EnterUpgradeableReadLock())
                {
                    if (Interlocked.Exchange(ref _strNotes, value) == value)
                        return;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Sourcebook Page Number using a given language file.
        /// Returns Page if not found or the string is empty.
        /// </summary>
        /// <param name="strLanguage">Language file keyword to use.</param>
        /// <returns></returns>
        public string DisplayPage(string strLanguage)
        {
            using (LockObject.EnterReadLock())
            {
                if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                    return Page;
                string s = this.GetNodeXPath(strLanguage)?.SelectSingleNodeAndCacheExpression("altpage")?.Value ?? Page;
                return !string.IsNullOrWhiteSpace(s) ? s : Page;
            }
        }

        /// <summary>
        /// Sourcebook Page Number using a given language file.
        /// Returns Page if not found or the string is empty.
        /// </summary>
        /// <param name="strLanguage">Language file keyword to use.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns></returns>
        public async Task<string> DisplayPageAsync(string strLanguage, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                    return Page;
                XPathNavigator objNode = await this.GetNodeXPathAsync(strLanguage, token: token).ConfigureAwait(false);
                string strReturn = objNode?.SelectSingleNodeAndCacheExpression("altpage", token: token)?.Value ?? Page;
                return !string.IsNullOrWhiteSpace(strReturn) ? strReturn : Page;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        private XmlNode _xmlCachedMyXmlNode;
        private string _strCachedXmlNodeLanguage = string.Empty;

        public async Task<XmlNode> GetNodeCoreAsync(bool blnSync, string strLanguage, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IDisposable objLocker = null;
            IAsyncDisposable objLockerAsync = null;
            if (blnSync)
                // ReSharper disable once MethodHasAsyncOverload
                objLocker = LockObject.EnterReadLock(token);
            else
                objLockerAsync = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (Type == TraditionType.None)
                    return null;
                XmlNode objReturn = _xmlCachedMyXmlNode;
                if (objReturn != null && strLanguage == _strCachedXmlNodeLanguage
                                      && !GlobalSettings.LiveCustomData)
                    return objReturn;
                XmlDocument objDoc = null;
                switch (Type)
                {
                    case TraditionType.MAG:
                        objDoc = blnSync
                            // ReSharper disable once MethodHasAsyncOverload
                            ? _objCharacter.LoadData("traditions.xml", strLanguage, token: token)
                            : await _objCharacter.LoadDataAsync("traditions.xml", strLanguage, token: token)
                                                 .ConfigureAwait(false);
                        break;

                    case TraditionType.RES:
                        objDoc = blnSync
                            // ReSharper disable once MethodHasAsyncOverload
                            ? _objCharacter.LoadData("traditions.xml", strLanguage, token: token)
                            : await _objCharacter.LoadDataAsync("streams.xml", strLanguage, token: token)
                                                 .ConfigureAwait(false);
                        break;
                }

                objReturn = objDoc?.TryGetNodeById("/chummer/traditions/tradition", SourceID);
                _xmlCachedMyXmlNode = objReturn;
                _strCachedXmlNodeLanguage = strLanguage;
                return objReturn;
            }
            finally
            {
                objLocker?.Dispose();
                if (objLockerAsync != null)
                    await objLockerAsync.DisposeAsync().ConfigureAwait(false);
            }
        }

        private XPathNavigator _objCachedMyXPathNode;
        private string _strCachedXPathNodeLanguage = string.Empty;

        public async Task<XPathNavigator> GetNodeXPathCoreAsync(bool blnSync, string strLanguage, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IDisposable objLocker = null;
            IAsyncDisposable objLockerAsync = null;
            if (blnSync)
                // ReSharper disable once MethodHasAsyncOverload
                objLocker = LockObject.EnterReadLock(token);
            else
                objLockerAsync = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                XPathNavigator objReturn = _objCachedMyXPathNode;
                if (objReturn != null && strLanguage == _strCachedXPathNodeLanguage
                                      && !GlobalSettings.LiveCustomData)
                    return objReturn;
                XPathNavigator objDoc = null;
                switch (Type)
                {
                    case TraditionType.MAG:
                        objDoc = blnSync
                            // ReSharper disable once MethodHasAsyncOverload
                            ? _objCharacter.LoadDataXPath("traditions.xml", strLanguage, token: token)
                            : await _objCharacter.LoadDataXPathAsync("traditions.xml", strLanguage, token: token)
                                                 .ConfigureAwait(false);
                        break;

                    case TraditionType.RES:
                        objDoc = blnSync
                            // ReSharper disable once MethodHasAsyncOverload
                            ? _objCharacter.LoadDataXPath("streams.xml", strLanguage, token: token)
                            : await _objCharacter.LoadDataXPathAsync("streams.xml", strLanguage, token: token)
                                                 .ConfigureAwait(false);
                        break;
                }

                objReturn = objDoc?.TryGetNodeById("/chummer/traditions/tradition", SourceID);
                _objCachedMyXPathNode = objReturn;
                _strCachedXPathNodeLanguage = strLanguage;
                return objReturn;
            }
            finally
            {
                objLocker?.Dispose();
                if (objLockerAsync != null)
                    await objLockerAsync.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Properties

        #region static

        //A tree of dependencies. Once some of the properties are changed,
        //anything they depend on, also needs to raise OnChanged
        //This tree keeps track of dependencies
        private static readonly PropertyDependencyGraph<Tradition> s_AttributeDependencyGraph =
            new PropertyDependencyGraph<Tradition>(
                new DependencyGraphNode<string, Tradition>(nameof(CurrentDisplayName),
                    new DependencyGraphNode<string, Tradition>(nameof(DisplayName),
                        new DependencyGraphNode<string, Tradition>(nameof(DisplayNameShort),
                            new DependencyGraphNode<string, Tradition>(nameof(Name))
                        ),
                        new DependencyGraphNode<string, Tradition>(nameof(Extra))
                    )
                ),
                new DependencyGraphNode<string, Tradition>(nameof(DrainValueToolTip),
                    new DependencyGraphNode<string, Tradition>(nameof(DrainValue),
                        new DependencyGraphNode<string, Tradition>(nameof(DrainExpression))
                    )
                ),
                new DependencyGraphNode<string, Tradition>(nameof(DisplayDrainExpression),
                    new DependencyGraphNode<string, Tradition>(nameof(DrainExpression))
                ),
                new DependencyGraphNode<string, Tradition>(nameof(AvailableSpirits),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritCombat)),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritDetection)),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritHealth)),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritIllusion)),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritManipulation))
                ),
                new DependencyGraphNode<string, Tradition>(nameof(DisplaySpiritCombat),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritCombat))
                ),
                new DependencyGraphNode<string, Tradition>(nameof(DisplaySpiritDetection),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritDetection))
                ),
                new DependencyGraphNode<string, Tradition>(nameof(DisplaySpiritHealth),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritHealth))
                ),
                new DependencyGraphNode<string, Tradition>(nameof(DisplaySpiritIllusion),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritIllusion))
                ),
                new DependencyGraphNode<string, Tradition>(nameof(DisplaySpiritManipulation),
                    new DependencyGraphNode<string, Tradition>(nameof(SpiritManipulation))
                )
            );

        #endregion static

        public void SetSourceDetail(Control sourceControl)
        {
            using (LockObject.EnterReadLock())
            {
                if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                    _objCachedSourceDetail = default;
                SourceDetail.SetControl(sourceControl);
            }
        }

        public async Task SetSourceDetailAsync(Control sourceControl, CancellationToken token = default)
        {
            IAsyncDisposable objLocker = await LockObject.EnterReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                    _objCachedSourceDetail = default;
                await (await GetSourceDetailAsync(token).ConfigureAwait(false)).SetControlAsync(sourceControl, token).ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly ConcurrentHashSet<PropertyChangedAsyncEventHandler> _setPropertyChangedAsync =
            new ConcurrentHashSet<PropertyChangedAsyncEventHandler>();

        public event PropertyChangedAsyncEventHandler PropertyChangedAsync
        {
            add => _setPropertyChangedAsync.TryAdd(value);
            remove => _setPropertyChangedAsync.Remove(value);
        }

        public event MultiplePropertiesChangedEventHandler MultiplePropertiesChanged;

        private readonly ConcurrentHashSet<MultiplePropertiesChangedAsyncEventHandler> _setMultiplePropertiesChangedAsync =
            new ConcurrentHashSet<MultiplePropertiesChangedAsyncEventHandler>();

        public event MultiplePropertiesChangedAsyncEventHandler MultiplePropertiesChangedAsync
        {
            add => _setMultiplePropertiesChangedAsync.TryAdd(value);
            remove => _setMultiplePropertiesChangedAsync.Remove(value);
        }

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string strPropertyName = null)
        {
            this.OnMultiplePropertyChanged(strPropertyName);
        }

        public Task OnPropertyChangedAsync(string strPropertyName, CancellationToken token = default)
        {
            return this.OnMultiplePropertyChangedAsync(token, strPropertyName);
        }

        public void OnMultiplePropertiesChanged(IReadOnlyCollection<string> lstPropertyNames)
        {
            using (LockObject.EnterUpgradeableReadLock())
            {
                HashSet<string> setNamesOfChangedProperties = null;
                try
                {
                    foreach (string strPropertyName in lstPropertyNames)
                    {
                        if (setNamesOfChangedProperties == null)
                            setNamesOfChangedProperties
                                = s_AttributeDependencyGraph.GetWithAllDependents(this, strPropertyName, true);
                        else
                        {
                            foreach (string strLoopChangedProperty in s_AttributeDependencyGraph
                                         .GetWithAllDependentsEnumerable(this, strPropertyName))
                                setNamesOfChangedProperties.Add(strLoopChangedProperty);
                        }
                    }

                    if (setNamesOfChangedProperties == null || setNamesOfChangedProperties.Count == 0)
                        return;

                    if (_setMultiplePropertiesChangedAsync.Count > 0)
                    {
                        MultiplePropertiesChangedEventArgs objArgs =
                            new MultiplePropertiesChangedEventArgs(setNamesOfChangedProperties.ToArray());
                        List<Func<Task>> lstFuncs = new List<Func<Task>>(_setMultiplePropertiesChangedAsync.Count);
                        foreach (MultiplePropertiesChangedAsyncEventHandler objEvent in _setMultiplePropertiesChangedAsync)
                        {
                            lstFuncs.Add(() => objEvent.Invoke(this, objArgs));
                        }

                        Utils.RunWithoutThreadLock(lstFuncs);
                        if (MultiplePropertiesChanged != null)
                        {
                            Utils.RunOnMainThread(() =>
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                MultiplePropertiesChanged?.Invoke(this, objArgs);
                            });
                        }
                    }
                    else if (MultiplePropertiesChanged != null)
                    {
                        MultiplePropertiesChangedEventArgs objArgs =
                            new MultiplePropertiesChangedEventArgs(setNamesOfChangedProperties.ToArray());
                        Utils.RunOnMainThread(() =>
                        {
                            // ReSharper disable once AccessToModifiedClosure
                            MultiplePropertiesChanged?.Invoke(this, objArgs);
                        });
                    }

                    if (_setPropertyChangedAsync.Count > 0)
                    {
                        List<PropertyChangedEventArgs> lstArgsList = setNamesOfChangedProperties.Select(x => new PropertyChangedEventArgs(x)).ToList();
                        List<Func<Task>> lstFuncs = new List<Func<Task>>(lstArgsList.Count * _setPropertyChangedAsync.Count);
                        foreach (PropertyChangedAsyncEventHandler objEvent in _setPropertyChangedAsync)
                        {
                            foreach (PropertyChangedEventArgs objArg in lstArgsList)
                                lstFuncs.Add(() => objEvent.Invoke(this, objArg));
                        }

                        Utils.RunWithoutThreadLock(lstFuncs);
                        if (PropertyChanged != null)
                        {
                            Utils.RunOnMainThread(() =>
                            {
                                if (PropertyChanged != null)
                                {
                                    // ReSharper disable once AccessToModifiedClosure
                                    foreach (PropertyChangedEventArgs objArgs in lstArgsList)
                                    {
                                        PropertyChanged.Invoke(this, objArgs);
                                    }
                                }
                            });
                        }
                    }
                    else if (PropertyChanged != null)
                    {
                        Utils.RunOnMainThread(() =>
                        {
                            if (PropertyChanged != null)
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                foreach (string strPropertyToChange in setNamesOfChangedProperties)
                                {
                                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(strPropertyToChange));
                                }
                            }
                        });
                    }
                }
                finally
                {
                    if (setNamesOfChangedProperties != null)
                        Utils.StringHashSetPool.Return(ref setNamesOfChangedProperties);
                }

                _objCharacter?.OnPropertyChanged(nameof(Character.MagicTradition));
            }
        }

        public async Task OnMultiplePropertiesChangedAsync(IReadOnlyCollection<string> lstPropertyNames, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                HashSet<string> setNamesOfChangedProperties = null;
                try
                {
                    foreach (string strPropertyName in lstPropertyNames)
                    {
                        if (setNamesOfChangedProperties == null)
                            setNamesOfChangedProperties
                                = s_AttributeDependencyGraph.GetWithAllDependents(this, strPropertyName, true);
                        else
                        {
                            foreach (string strLoopChangedProperty in s_AttributeDependencyGraph
                                         .GetWithAllDependentsEnumerable(this, strPropertyName))
                                setNamesOfChangedProperties.Add(strLoopChangedProperty);
                        }
                    }

                    if (setNamesOfChangedProperties == null || setNamesOfChangedProperties.Count == 0)
                        return;

                    if (_setMultiplePropertiesChangedAsync.Count > 0)
                    {
                        MultiplePropertiesChangedEventArgs objArgs =
                            new MultiplePropertiesChangedEventArgs(setNamesOfChangedProperties.ToArray());
                        List<Task> lstTasks = new List<Task>(Utils.MaxParallelBatchSize);
                        int i = 0;
                        foreach (MultiplePropertiesChangedAsyncEventHandler objEvent in _setMultiplePropertiesChangedAsync)
                        {
                            lstTasks.Add(objEvent.Invoke(this, objArgs, token));
                            if (++i < Utils.MaxParallelBatchSize)
                                continue;
                            await Task.WhenAll(lstTasks).ConfigureAwait(false);
                            lstTasks.Clear();
                            i = 0;
                        }

                        await Task.WhenAll(lstTasks).ConfigureAwait(false);
                        if (MultiplePropertiesChanged != null)
                        {
                            await Utils.RunOnMainThreadAsync(() =>
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                MultiplePropertiesChanged?.Invoke(this, objArgs);
                            }, token: token).ConfigureAwait(false);
                        }
                    }
                    else if (MultiplePropertiesChanged != null)
                    {
                        MultiplePropertiesChangedEventArgs objArgs =
                            new MultiplePropertiesChangedEventArgs(setNamesOfChangedProperties.ToArray());
                        await Utils.RunOnMainThreadAsync(() =>
                        {
                            // ReSharper disable once AccessToModifiedClosure
                            MultiplePropertiesChanged?.Invoke(this, objArgs);
                        }, token: token).ConfigureAwait(false);
                    }

                    if (_setPropertyChangedAsync.Count > 0)
                    {
                        List<PropertyChangedEventArgs> lstArgsList = setNamesOfChangedProperties
                            .Select(x => new PropertyChangedEventArgs(x)).ToList();
                        List<Task> lstTasks =
                            new List<Task>(Math.Min(lstArgsList.Count * _setPropertyChangedAsync.Count,
                                Utils.MaxParallelBatchSize));
                        int i = 0;
                        foreach (PropertyChangedAsyncEventHandler objEvent in _setPropertyChangedAsync)
                        {
                            foreach (PropertyChangedEventArgs objArg in lstArgsList)
                            {
                                lstTasks.Add(objEvent.Invoke(this, objArg, token));
                                if (++i < Utils.MaxParallelBatchSize)
                                    continue;
                                await Task.WhenAll(lstTasks).ConfigureAwait(false);
                                lstTasks.Clear();
                                i = 0;
                            }
                        }

                        await Task.WhenAll(lstTasks).ConfigureAwait(false);

                        if (PropertyChanged != null)
                        {
                            await Utils.RunOnMainThreadAsync(() =>
                            {
                                if (PropertyChanged != null)
                                {
                                    // ReSharper disable once AccessToModifiedClosure
                                    foreach (PropertyChangedEventArgs objArgs in lstArgsList)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        PropertyChanged.Invoke(this, objArgs);
                                    }
                                }
                            }, token).ConfigureAwait(false);
                        }
                    }
                    else if (PropertyChanged != null)
                    {
                        await Utils.RunOnMainThreadAsync(() =>
                        {
                            if (PropertyChanged != null)
                            {
                                // ReSharper disable once AccessToModifiedClosure
                                foreach (string strPropertyToChange in setNamesOfChangedProperties)
                                {
                                    token.ThrowIfCancellationRequested();
                                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(strPropertyToChange));
                                }
                            }
                        }, token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (setNamesOfChangedProperties != null)
                        Utils.StringHashSetPool.Return(ref setNamesOfChangedProperties);
                }

                if (_objCharacter != null)
                    await _objCharacter.OnPropertyChangedAsync(nameof(Character.MagicTradition), token)
                        .ConfigureAwait(false);
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public AsyncFriendlyReaderWriterLock LockObject { get; }
    }
}
