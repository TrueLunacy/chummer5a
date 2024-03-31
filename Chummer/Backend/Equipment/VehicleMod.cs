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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using NLog;

namespace Chummer.Backend.Equipment
{
    /// <summary>
    /// Vehicle Modification.
    /// </summary>
    [DebuggerDisplay("{DisplayName(GlobalSettings.InvariantCultureInfo, GlobalSettings.DefaultLanguage)}")]
    public sealed class VehicleMod : IHasInternalId, IHasName, IHasSourceId, IHasXmlDataNode, IHasNotes, ICanEquip, IHasSource, IHasRating, ICanSort, IHasStolenProperty, ICanPaste, ICanSell, ICanBlackMarketDiscount, IDisposable, IAsyncDisposable, IHasCharacterObject
    {
        private static readonly Lazy<Logger> s_ObjLogger = new Lazy<Logger>(LogManager.GetCurrentClassLogger);
        private static Logger Log => s_ObjLogger.Value;
        private Guid _guiID;
        private Guid _guiSourceID;
        private string _strName = string.Empty;
        private string _strCategory = string.Empty;
        private string _strLimit = string.Empty;
        private string _strSlots = "0";
        private int _intRating;
        private string _strRatingLabel = "String_Rating";
        private string _strMaxRating = string.Empty;
        private string _strCost = string.Empty;
        private decimal _decMarkup;
        private string _strAvail = string.Empty;
        private XmlNode _nodBonus;
        private XmlNode _nodWirelessBonus;
        private bool _blnWirelessOn = true;
        private string _strSource = string.Empty;
        private string _strPage = string.Empty;
        private bool _blnIncludeInVehicle;
        private bool _blnEquipped = true;
        private int _intConditionMonitor;
        private readonly TaggedObservableCollection<Weapon> _lstVehicleWeapons;
        private string _strNotes = string.Empty;
        private Color _colNotes = ColorManager.HasNotesColor;
        private string _strSubsystems = string.Empty;
        private readonly TaggedObservableCollection<Cyberware> _lstCyberware;
        private string _strExtra = string.Empty;
        private string _strWeaponMountCategories = string.Empty;
        private bool _blnDiscountCost;
        private bool _blnDowngrade;
        private string _strCapacity = string.Empty;

        private XmlNode _objCachedMyXmlNode;
        private string _strCachedXmlNodeLanguage = string.Empty;
        private string _strAmmoReplace;
        private int _intAmmoBonus;
        private decimal _decAmmoBonusPercent;
        private int _intSortOrder;
        private bool _blnStolen;
        private readonly Character _objCharacter;
        private Vehicle _objParent;
        private WeaponMount _objWeaponMountParent;

        #region Constructor, Create, Save, Load, and Print Methods

        public VehicleMod(Character objCharacter)
        {
            // Create the GUID for the new VehicleMod.
            _guiID = Guid.NewGuid();
            _objCharacter = objCharacter;
            _lstVehicleWeapons = new TaggedObservableCollection<Weapon>(objCharacter.LockObject);
            _lstVehicleWeapons.AddTaggedCollectionChanged(this, ChildrenWeaponsOnCollectionChanged);
            _lstCyberware = new TaggedObservableCollection<Cyberware>(objCharacter.LockObject);
            _lstCyberware.AddTaggedCollectionChanged(this, ChildrenCyberwareOnCollectionChanged);
        }

        private void ChildrenCyberwareOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Cyberware objNewItem in e.NewItems)
                        objNewItem.ParentVehicle = Parent;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Cyberware objOldItem in e.OldItems)
                        objOldItem.ParentVehicle = null;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (Cyberware objOldItem in e.OldItems)
                        objOldItem.ParentVehicle = null;
                    foreach (Cyberware objNewItem in e.NewItems)
                        objNewItem.ParentVehicle = Parent;
                    break;
            }
        }

        private void ChildrenWeaponsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Weapon objNewItem in e.NewItems)
                        objNewItem.ParentVehicle = Parent;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Weapon objOldItem in e.OldItems)
                        objOldItem.ParentVehicle = null;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (Weapon objOldItem in e.OldItems)
                        objOldItem.ParentVehicle = null;
                    foreach (Weapon objNewItem in e.NewItems)
                        objNewItem.ParentVehicle = Parent;
                    break;
            }
        }

        /// <summary>
        /// Create a Vehicle Modification from an XmlNode and return the TreeNodes for it.
        /// </summary>
        /// <param name="objXmlMod">XmlNode to create the object from.</param>
        /// <param name="intRating">Selected Rating for the Gear.</param>
        /// <param name="objParent">Vehicle that the mod will be attached to.</param>
        /// <param name="decMarkup">Discount or markup that applies to the base cost of the mod.</param>
        /// <param name="strForcedValue">Value to forcefully select for any ImprovementManager prompts.</param>
        /// <param name="blnSkipSelectForms">Whether bonuses should be created.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public void Create(XmlNode objXmlMod, int intRating, Vehicle objParent, decimal decMarkup = 0,
            string strForcedValue = "", bool blnSkipSelectForms = false, CancellationToken token = default)
        {
            Utils.SafelyRunSynchronously(() => CreateCoreAsync(true, objXmlMod, intRating, objParent, decMarkup,
                strForcedValue, blnSkipSelectForms, token), token);
        }

        /// <summary>
        /// Create a Vehicle Modification from an XmlNode and return the TreeNodes for it.
        /// </summary>
        /// <param name="objXmlMod">XmlNode to create the object from.</param>
        /// <param name="intRating">Selected Rating for the Gear.</param>
        /// <param name="objParent">Vehicle that the mod will be attached to.</param>
        /// <param name="decMarkup">Discount or markup that applies to the base cost of the mod.</param>
        /// <param name="strForcedValue">Value to forcefully select for any ImprovementManager prompts.</param>
        /// <param name="blnSkipSelectForms">Whether bonuses should be created.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public Task CreateAsync(XmlNode objXmlMod, int intRating, Vehicle objParent, decimal decMarkup = 0,
            string strForcedValue = "", bool blnSkipSelectForms = false, CancellationToken token = default)
        {
            return CreateCoreAsync(false, objXmlMod, intRating, objParent, decMarkup, strForcedValue,
                blnSkipSelectForms, token);
        }

        private async Task CreateCoreAsync(bool blnSync, XmlNode objXmlMod, int intRating, Vehicle objParent, decimal decMarkup = 0,
            string strForcedValue = "", bool blnSkipSelectForms = false, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Parent = objParent ?? throw new ArgumentNullException(nameof(objParent));
            if (objXmlMod == null) Utils.BreakIfDebug();
            if (!objXmlMod.TryGetField("id", out _guiSourceID))
            {
                Log.Warn(new object[] { "Missing id field for xmlnode", objXmlMod });
                Utils.BreakIfDebug();
            }
            else
            {
                _objCachedMyXmlNode = null;
                _objCachedMyXPathNode = null;
            }

            if (objXmlMod.TryGetStringFieldQuickly("name", ref _strName))
            {
                _objCachedMyXmlNode = null;
                _objCachedMyXPathNode = null;
            }

            objXmlMod.TryGetStringFieldQuickly("category", ref _strCategory);
            objXmlMod.TryGetStringFieldQuickly("limit", ref _strLimit);
            objXmlMod.TryGetStringFieldQuickly("slots", ref _strSlots);
            _blnDowngrade = objXmlMod?["downgrade"] != null;
            if (!objXmlMod.TryGetMultiLineStringFieldQuickly("altnotes", ref _strNotes))
                objXmlMod.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);

            string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
            objXmlMod.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
            _colNotes = ColorTranslator.FromHtml(sNotesColor);
            objXmlMod.TryGetStringFieldQuickly("capacity", ref _strCapacity);
            objXmlMod.TryGetStringFieldQuickly("rating", ref _strMaxRating);
            switch (_strMaxRating.ToUpperInvariant())
            {
                case "QTY":
                    _strRatingLabel = "Label_Qty";
                    break;
                case "SEATS":
                    _strRatingLabel = "Label_Seats";
                    break;
            }
            _intRating = string.IsNullOrEmpty(_strMaxRating) ? 0 : Math.Min(Math.Max(intRating, 1), blnSync ? MaxRating : await GetMaxRatingAsync(token).ConfigureAwait(false));
            objXmlMod.TryGetStringFieldQuickly("ratinglabel", ref _strRatingLabel);
            objXmlMod.TryGetFieldUninitialized("conditionmonitor", ref _intConditionMonitor);
            objXmlMod.TryGetStringFieldQuickly("weaponmountcategories", ref _strWeaponMountCategories);
            objXmlMod.TryGetStringFieldQuickly("ammoreplace", ref _strAmmoReplace);
            objXmlMod.TryGetFieldUninitialized("ammobonus", ref _intAmmoBonus);
            objXmlMod.TryGetFieldUninitialized("ammobonuspercent", ref _decAmmoBonusPercent);
            // Add Subsystem information if applicable.
            XmlElement xmlSubsystemsNode = objXmlMod?["subsystems"];
            if (xmlSubsystemsNode != null)
            {
                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                                                              out StringBuilder sbdSubsystems))
                {
                    using (XmlNodeList xmlSubsystemList = xmlSubsystemsNode.SelectNodes("subsystem"))
                    {
                        if (xmlSubsystemList?.Count > 0)
                        {
                            foreach (XmlNode objXmlSubsystem in xmlSubsystemList)
                            {
                                sbdSubsystems.Append(objXmlSubsystem.InnerText).Append(',');
                            }
                        }
                    }

                    // Remove last ","
                    if (sbdSubsystems.Length > 0)
                        --sbdSubsystems.Length;
                    _strSubsystems = sbdSubsystems.ToString();
                }
            }
            objXmlMod.TryGetStringFieldQuickly("avail", ref _strAvail);

            _strCost = objXmlMod?["cost"]?.InnerText ?? string.Empty;
            // Check for a Variable Cost.
            if (_strCost.StartsWith("Variable(", StringComparison.Ordinal))
            {
                decimal decMin;
                decimal decMax = decimal.MaxValue;
                string strCost = _strCost.TrimStartOnce("Variable(", true).TrimEndOnce(')');
                if (strCost.Contains('-'))
                {
                    string[] strValues = strCost.Split('-');
                    decMin = Convert.ToDecimal(strValues[0], GlobalSettings.InvariantCultureInfo);
                    decMax = Convert.ToDecimal(strValues[1], GlobalSettings.InvariantCultureInfo);
                }
                else
                    decMin = Convert.ToDecimal(strCost.FastEscape('+'), GlobalSettings.InvariantCultureInfo);

                if (decMin != 0 || decMax != decimal.MaxValue)
                {
                    if (decMax > 1000000)
                        decMax = 1000000;
                    if (blnSync)
                    {
                        using (ThreadSafeForm<SelectNumber> frmPickNumber
                               // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                               = ThreadSafeForm<SelectNumber>.Get(() => new SelectNumber(_objCharacter.Settings.MaxNuyenDecimals)
                               {
                                   Minimum = decMin,
                                   Maximum = decMax,
                                   Description = string.Format(
                                       GlobalSettings.CultureInfo,
                                       LanguageManager.GetString("String_SelectVariableCost", token: token),
                                       CurrentDisplayNameShort),
                                   AllowCancel = false
                               }))
                        {
                            // ReSharper disable once MethodHasAsyncOverload
                            if (frmPickNumber.ShowDialogSafe(_objCharacter, token) == DialogResult.Cancel)
                            {
                                _guiID = Guid.Empty;
                                return;
                            }
                            _strCost = frmPickNumber.MyForm.SelectedValue.ToString(GlobalSettings.InvariantCultureInfo);
                        }
                    }
                    else
                    {
                        string strDescription = string.Format(
                            GlobalSettings.CultureInfo,
                            await LanguageManager.GetStringAsync("String_SelectVariableCost", token: token).ConfigureAwait(false),
                            await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false));
                        using (ThreadSafeForm<SelectNumber> frmPickNumber
                               = await ThreadSafeForm<SelectNumber>.GetAsync(() => new SelectNumber(_objCharacter.Settings.MaxNuyenDecimals)
                               {
                                   Minimum = decMin,
                                   Maximum = decMax,
                                   Description = strDescription,
                                   AllowCancel = false
                               }, token).ConfigureAwait(false))
                        {
                            if (await frmPickNumber.ShowDialogSafeAsync(_objCharacter, token).ConfigureAwait(false) == DialogResult.Cancel)
                            {
                                _guiID = Guid.Empty;
                                return;
                            }
                            _strCost = frmPickNumber.MyForm.SelectedValue.ToString(GlobalSettings.InvariantCultureInfo);
                        }
                    }
                }
            }
            _decMarkup = decMarkup;

            objXmlMod.TryGetStringFieldQuickly("source", ref _strSource);
            objXmlMod.TryGetStringFieldQuickly("page", ref _strPage);

            if (GlobalSettings.InsertPdfNotesIfAvailable && string.IsNullOrEmpty(Notes))
            {
                if (blnSync)
                    // ReSharper disable once MethodHasAsyncOverload
                    Notes = CommonFunctions.GetBookNotes(objXmlMod, Name, CurrentDisplayName, Source,
                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                        Page, DisplayPage(GlobalSettings.Language), _objCharacter, token);
                else
                    Notes = await CommonFunctions.GetBookNotesAsync(objXmlMod, Name,
                        await GetCurrentDisplayNameAsync(token).ConfigureAwait(false), Source, Page,
                        await DisplayPageAsync(GlobalSettings.Language, token).ConfigureAwait(false), _objCharacter, token).ConfigureAwait(false);
            }
            _nodBonus = objXmlMod?["bonus"];
            _nodWirelessBonus = objXmlMod?["wirelessbonus"];
            _blnWirelessOn = false;

            if (Bonus != null && !blnSkipSelectForms)
            {
                ImprovementManager.ForcedValue = strForcedValue;
                if (blnSync)
                {
                    // ReSharper disable once MethodHasAsyncOverload
                    if (!ImprovementManager.CreateImprovements(_objCharacter, Improvement.ImprovementSource.VehicleMod,
                            _guiID.ToString("D", GlobalSettings.InvariantCultureInfo), Bonus, intRating,
                            CurrentDisplayNameShort, false, token))
                    {
                        _guiID = Guid.Empty;
                        return;
                    }
                }
                else if (!await ImprovementManager.CreateImprovementsAsync(_objCharacter, Improvement.ImprovementSource.VehicleMod,
                             _guiID.ToString("D", GlobalSettings.InvariantCultureInfo), Bonus, intRating,
                             await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false), false, token).ConfigureAwait(false))
                {
                    _guiID = Guid.Empty;
                    return;
                }
                if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue))
                {
                    _strExtra = ImprovementManager.SelectedValue;
                }
            }
        }

        private SourceString _objCachedSourceDetail;

        public SourceString SourceDetail =>
            _objCachedSourceDetail == default
                ? _objCachedSourceDetail = SourceString.GetSourceString(Source,
                    DisplayPage(GlobalSettings.Language),
                    GlobalSettings.Language,
                    GlobalSettings.CultureInfo,
                    _objCharacter)
                : _objCachedSourceDetail;

        public async Task<SourceString> GetSourceDetailAsync(CancellationToken token = default)
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

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlWriter objWriter)
        {
            if (objWriter == null)
                return;
            objWriter.WriteStartElement("mod");
            objWriter.WriteElementString("sourceid", SourceIDString);
            objWriter.WriteElementString("guid", InternalId);
            objWriter.WriteElementString("name", _strName);
            objWriter.WriteElementString("category", _strCategory);
            objWriter.WriteElementString("limit", _strLimit);
            objWriter.WriteElementString("slots", _strSlots);
            objWriter.WriteElementString("capacity", _strCapacity);
            objWriter.WriteElementString("rating", _intRating.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("maxrating", _strMaxRating);
            objWriter.WriteElementString("ratinglabel", _strRatingLabel);
            objWriter.WriteElementString("conditionmonitor", _intConditionMonitor.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("avail", _strAvail);
            objWriter.WriteElementString("cost", _strCost);
            objWriter.WriteElementString("markup", _decMarkup.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("extra", _strExtra);
            objWriter.WriteElementString("source", _strSource);
            objWriter.WriteElementString("page", _strPage);
            objWriter.WriteElementString("included", _blnIncludeInVehicle.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("equipped", _blnEquipped.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("wirelesson", _blnWirelessOn.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("subsystems", _strSubsystems);
            objWriter.WriteElementString("weaponmountcategories", _strWeaponMountCategories);
            objWriter.WriteElementString("ammobonus", _intAmmoBonus.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("ammobonuspercent", _decAmmoBonusPercent.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("ammoreplace", _strAmmoReplace);
            objWriter.WriteStartElement("weapons");
            foreach (Weapon objWeapon in _lstVehicleWeapons)
                objWeapon.Save(objWriter);
            objWriter.WriteEndElement();
            if (_lstCyberware.Count > 0)
            {
                objWriter.WriteStartElement("cyberwares");
                _lstCyberware.ForEach(x => x.Save(objWriter));
                objWriter.WriteEndElement();
            }
            if (_nodBonus != null)
                objWriter.WriteRaw(_nodBonus.OuterXml);
            if (_nodWirelessBonus != null)
                objWriter.WriteRaw(_nodWirelessBonus.OuterXml);
            objWriter.WriteElementString("notes", _strNotes.CleanOfInvalidUnicodeChars());
            objWriter.WriteElementString("notesColor", ColorTranslator.ToHtml(_colNotes));
            objWriter.WriteElementString("discountedcost", _blnDiscountCost.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("sortorder", _intSortOrder.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("stolen", _blnStolen.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteEndElement();
        }

        /// <summary>
        /// Load the VehicleMod from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        /// <param name="blnCopy">Indicates whether a new item will be created as a copy of this one.</param>
        public void Load(XmlNode objNode, bool blnCopy = false)
        {
            if (objNode == null)
                return;
            if (blnCopy || !objNode.TryGetField("guid", out _guiID))
            {
                _guiID = Guid.NewGuid();
            }

            objNode.TryGetStringFieldQuickly("name", ref _strName);
            _objCachedMyXmlNode = null;
            _objCachedMyXPathNode = null;
            Lazy<XPathNavigator> objMyNode = new Lazy<XPathNavigator>(() => this.GetNodeXPath());
            if (!objNode.TryGetFieldUninitialized("sourceid", ref _guiSourceID))
            {
                objMyNode.Value?.TryGetFieldUninitialized("id", ref _guiSourceID);
            }
            objNode.TryGetStringFieldQuickly("category", ref _strCategory);
            objNode.TryGetStringFieldQuickly("limit", ref _strLimit);
            objNode.TryGetStringFieldQuickly("slots", ref _strSlots);
            objNode.TryGetFieldUninitialized("rating", ref _intRating);
            objNode.TryGetStringFieldQuickly("maxrating", ref _strMaxRating);
            objNode.TryGetStringFieldQuickly("ratinglabel", ref _strRatingLabel);
            objNode.TryGetStringFieldQuickly("capacity", ref _strCapacity);
            objNode.TryGetStringFieldQuickly("weaponmountcategories", ref _strWeaponMountCategories);
            objNode.TryGetStringFieldQuickly("page", ref _strPage);
            objNode.TryGetStringFieldQuickly("avail", ref _strAvail);
            objNode.TryGetFieldUninitialized("conditionmonitor", ref _intConditionMonitor);
            objNode.TryGetStringFieldQuickly("cost", ref _strCost);
            objNode.TryGetFieldUninitialized("markup", ref _decMarkup);
            objNode.TryGetStringFieldQuickly("source", ref _strSource);
            objNode.TryGetFieldUninitialized("included", ref _blnIncludeInVehicle);
            objNode.TryGetFieldUninitialized("equipped", ref _blnEquipped);
            if (!_blnEquipped)
            {
                objNode.TryGetFieldUninitialized("installed", ref _blnEquipped);
            }
            objNode.TryGetFieldUninitialized("ammobonuspercent", ref _decAmmoBonusPercent);
            objNode.TryGetFieldUninitialized("ammobonus", ref _intAmmoBonus);
            objNode.TryGetStringFieldQuickly("ammoreplace", ref _strAmmoReplace);
            objNode.TryGetStringFieldQuickly("subsystems", ref _strSubsystems);
            // Legacy Shims
            if (Name.StartsWith("Gecko Tips (Bod", StringComparison.Ordinal))
            {
                Name = "Gecko Tips";
                XPathNavigator objNewNode = objMyNode.Value;
                if (objNewNode != null)
                {
                    objNewNode.TryGetStringFieldQuickly("cost", ref _strCost);
                    objNewNode.TryGetStringFieldQuickly("slots", ref _strSlots);
                }
            }
            if (Name.StartsWith("Gliding System (Bod", StringComparison.Ordinal))
            {
                Name = "Gliding System";
                XPathNavigator objNewNode = objMyNode.Value;
                if (objNewNode != null)
                {
                    objNewNode.TryGetStringFieldQuickly("cost", ref _strCost);
                    objNewNode.TryGetStringFieldQuickly("slots", ref _strSlots);
                    objNewNode.TryGetStringFieldQuickly("avail", ref _strAvail);
                }
            }

            XmlElement xmlChildrenNode = objNode["weapons"];
            using (XmlNodeList xmlNodeList = xmlChildrenNode?.SelectNodes("weapon"))
            {
                if (xmlNodeList?.Count > 0)
                {
                    foreach (XmlNode nodChild in xmlNodeList)
                    {
                        Weapon objWeapon = new Weapon(_objCharacter)
                        {
                            ParentVehicle = Parent,
                            ParentVehicleMod = this
                        };
                        objWeapon.Load(nodChild, blnCopy);
                        _lstVehicleWeapons.Add(objWeapon);
                    }
                }
            }

            xmlChildrenNode = objNode["cyberwares"];
            using (XmlNodeList xmlNodeList = xmlChildrenNode?.SelectNodes("cyberware"))
            {
                if (xmlNodeList?.Count > 0)
                {
                    foreach (XmlNode nodChild in xmlNodeList)
                    {
                        Cyberware objCyberware = new Cyberware(_objCharacter)
                        {
                            ParentVehicle = Parent
                        };
                        objCyberware.Load(nodChild, blnCopy);
                        _lstCyberware.Add(objCyberware);
                    }
                }
            }

            _nodBonus = objNode["bonus"];
            _nodWirelessBonus = objNode["wirelessbonus"];
            if (!objNode.TryGetFieldUninitialized("wirelesson", ref _blnWirelessOn))
                _blnWirelessOn = false;
            objNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);

            string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
            objNode.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
            _colNotes = ColorTranslator.FromHtml(sNotesColor);

            objNode.TryGetFieldUninitialized("discountedcost", ref _blnDiscountCost);
            objNode.TryGetStringFieldQuickly("extra", ref _strExtra);
            objNode.TryGetFieldUninitialized("sortorder", ref _intSortOrder);
            objNode.TryGetFieldUninitialized("stolen", ref _blnStolen);
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
            objWriter.WriteStartElement("mod");
            objWriter.WriteElementString("guid", InternalId);
            objWriter.WriteElementString("sourceid", SourceIDString);
            objWriter.WriteElementString("name", await DisplayNameShortAsync(strLanguageToPrint, token).ConfigureAwait(false));
            objWriter.WriteElementString("name_english", Name);
            objWriter.WriteElementString("fullname", await DisplayNameAsync(objCulture, strLanguageToPrint, token).ConfigureAwait(false));
            objWriter.WriteElementString("category", await DisplayCategoryAsync(strLanguageToPrint, token).ConfigureAwait(false));
            objWriter.WriteElementString("category_english", Category);
            objWriter.WriteElementString("limit", Limit);
            objWriter.WriteElementString("slots", Slots);
            objWriter.WriteElementString("rating", Rating.ToString(objCulture));
            objWriter.WriteElementString("ratinglabel", RatingLabel);
            objWriter.WriteElementString("avail", await TotalAvailAsync(objCulture, strLanguageToPrint, token).ConfigureAwait(false));
            objWriter.WriteElementString("cost", (await GetTotalCostAsync(token).ConfigureAwait(false)).ToString(_objCharacter.Settings.NuyenFormat, objCulture));
            objWriter.WriteElementString("owncost", (await GetOwnCostAsync(token).ConfigureAwait(false)).ToString(_objCharacter.Settings.NuyenFormat, objCulture));
            objWriter.WriteElementString("source", await _objCharacter.LanguageBookShortAsync(Source, strLanguageToPrint, token).ConfigureAwait(false));
            objWriter.WriteElementString("wirelesson", WirelessOn.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("page", await DisplayPageAsync(strLanguageToPrint, token).ConfigureAwait(false));
            objWriter.WriteElementString("included", IncludedInVehicle.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteStartElement("weapons");
            foreach (Weapon objWeapon in Weapons)
                await objWeapon.Print(objWriter, objCulture, strLanguageToPrint, token).ConfigureAwait(false);
            await objWriter.WriteEndElementAsync().ConfigureAwait(false);
            objWriter.WriteStartElement("cyberwares");
            foreach (Cyberware objCyberware in Cyberware)
                await objCyberware.Print(objWriter, objCulture, strLanguageToPrint, token).ConfigureAwait(false);
            await objWriter.WriteEndElementAsync().ConfigureAwait(false);
            if (GlobalSettings.PrintNotes)
                objWriter.WriteElementString("notes", Notes);
            await objWriter.WriteEndElementAsync().ConfigureAwait(false);
        }

        #endregion Constructor, Create, Save, Load, and Print Methods

        #region Properties

        /// <summary>
        /// Weapons.
        /// </summary>
        public TaggedObservableCollection<Weapon> Weapons
        {
            get
            {
                using (_objCharacter.LockObject.EnterReadLock())
                    return _lstVehicleWeapons;
            }
        }

        public TaggedObservableCollection<Cyberware> Cyberware
        {
            get
            {
                using (_objCharacter.LockObject.EnterReadLock())
                    return _lstCyberware;
            }
        }

        public WeaponMount WeaponMountParent
        {
            get => _objWeaponMountParent;
            set
            {
                if (Interlocked.Exchange(ref _objWeaponMountParent, value) == value)
                    return;
                Vehicle objNewParent = value?.Parent;
                if (objNewParent != null)
                    Parent = objNewParent;
            }
        }

        /// <summary>
        /// Identifier of the object within data files.
        /// </summary>
        public Guid SourceID => _guiSourceID;

        /// <summary>
        /// String-formatted identifier of the <inheritdoc cref="SourceID"/> from the data files.
        /// </summary>
        public string SourceIDString => _guiSourceID.ToString("D", GlobalSettings.InvariantCultureInfo);

        /// <summary>
        /// Internal identifier which will be used to identify this piece of Gear in the Character.
        /// </summary>
        public string InternalId => _guiID.ToString("D", GlobalSettings.InvariantCultureInfo);

        /// <summary>
        /// Name.
        /// </summary>
        public string Name
        {
            get => _strName;
            set
            {
                if (Interlocked.Exchange(ref _strName, value) == value)
                    return;
                _objCachedMyXmlNode = null;
                _objCachedMyXPathNode = null;
            }
        }

        /// <summary>
        /// Translated Category.
        /// </summary>
        public string DisplayCategory(string strLanguage)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Category;

            return _objCharacter.LoadDataXPath("vehicles.xml", strLanguage)
                                .SelectSingleNodeAndCacheExpression(
                                    "/chummer/categories/category[. = " + Category.CleanXPath() + "]/@translate")?.Value
                   ?? Category;
        }

        /// <summary>
        /// Translated Category.
        /// </summary>
        public async Task<string> DisplayCategoryAsync(string strLanguage, CancellationToken token = default)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Category;

            return (await _objCharacter.LoadDataXPathAsync("vehicles.xml", strLanguage, token: token)
                    .ConfigureAwait(false))
                .SelectSingleNodeAndCacheExpression(
                    "/chummer/categories/category[. = " + Category.CleanXPath() + "]/@translate",
                    token: token)?.Value ?? Category;
        }

        /// <summary>
        /// Category.
        /// </summary>
        public string Category
        {
            get => _strCategory;
            set => _strCategory = value;
        }

        /// <summary>
        /// Limits the Weapon Selection form to specified categories.
        /// </summary>
        public string WeaponMountCategories
        {
            set => _strWeaponMountCategories = value;
            get => _strWeaponMountCategories;
        }

        /// <summary>
        /// Which Vehicle types the Mod is limited to.
        /// </summary>
        public string Limit
        {
            get => _strLimit;
            set => _strLimit = value;
        }

        /// <summary>
        /// Number of Slots the Mod uses.
        /// </summary>
        public string Slots
        {
            get => _strSlots;
            set => _strSlots = value;
        }

        /// <summary>
        /// Vehicle Mod capacity.
        /// </summary>
        public string Capacity
        {
            get => _strCapacity;
            set => _strCapacity = value;
        }

        /// <summary>
        /// Rating.
        /// </summary>
        public int Rating
        {
            get => Math.Min(_intRating, MaxRating);
            set
            {
                int intNewRating = Math.Min(Math.Max(1, value), MaxRating);
                if (Interlocked.Exchange(ref _intRating, intNewRating) != intNewRating && !IncludedInVehicle && Equipped)
                {
                    if (Parent != null && (Bonus?["sensor"] != null || (WirelessOn && WirelessBonus?["sensor"] != null)))
                    {
                        // Any time any vehicle mod is changed, update our sensory array's rating, just in case
                        Gear objGear = Parent.GearChildren
                            .FirstOrDefault(x =>
                                x.Category == "Sensors" && x.Name == "Sensor Array" && x.IncludedInParent);
                        if (objGear != null)
                            objGear.Rating = Parent.CalculatedSensor;
                    }
                    if (_objCharacter.IsAI && _objCharacter.HomeNode is Vehicle objVehicle && objVehicle == Parent)
                        _objCharacter.OnPropertyChanged(nameof(Character.PhysicalCM));
                }
            }
        }

        public async Task<int> GetRatingAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Math.Min(_intRating, await GetMaxRatingAsync(token).ConfigureAwait(false));
        }

        public async Task SetRatingAsync(int value, CancellationToken token = default)
        {
            int intNewRating = Math.Min(Math.Max(1, value), await GetMaxRatingAsync(token).ConfigureAwait(false));
            if (Interlocked.Exchange(ref _intRating, intNewRating) != intNewRating && !IncludedInVehicle && Equipped)
            {
                if (Parent != null && (Bonus?["sensor"] != null || (WirelessOn && WirelessBonus?["sensor"] != null)))
                {
                    // Any time any vehicle mod is changed, update our sensory array's rating, just in case
                    Gear objGear = await _objParent.GearChildren
                        .FirstOrDefaultAsync(x =>
                            x.Category == "Sensors" && x.Name == "Sensor Array" && x.IncludedInParent, token: token).ConfigureAwait(false);
                    if (objGear != null)
                        await objGear.SetRatingAsync(await Parent.GetCalculatedSensorAsync(token).ConfigureAwait(false), token).ConfigureAwait(false);
                }
                if (await _objCharacter.GetIsAIAsync(token).ConfigureAwait(false) && await _objCharacter.GetHomeNodeAsync(token).ConfigureAwait(false) is Vehicle objVehicle && objVehicle == Parent)
                    await _objCharacter.OnPropertyChangedAsync(nameof(Character.PhysicalCM), token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Maximum Rating.
        /// </summary>
        public int MaxRating
        {
            get
            {
                string strText = _strMaxRating.ToUpperInvariant();
                if (string.IsNullOrEmpty(strText))
                    return 0;
                int intReturn = 0;
                switch (strText)
                {
                    case "QTY":
                        intReturn = Vehicle.MaxWheels;
                        break;
                    case "SEATS":
                        intReturn = Parent.GetTotalSeats(this);
                        break;
                    case "BODY":
                        intReturn = Parent.GetTotalBody(this);
                        break;
                    default:
                        int.TryParse(strText, out intReturn);
                        break;
                }

                int intMaxValue = int.MaxValue;
                if (Name.StartsWith("Armor", StringComparison.OrdinalIgnoreCase))
                    intMaxValue = Parent.MaxArmor;
                else
                {
                    switch (Category.ToUpperInvariant())
                    {
                        case "HANDLING":
                            intMaxValue = Parent.MaxHandling;
                            break;
                        case "SPEED":
                            intMaxValue = Parent.MaxSpeed;
                            break;
                        case "ACCELERATION":
                            intMaxValue = Parent.MaxAcceleration;
                            break;
                        case "SENSOR":
                            intMaxValue = Parent.MaxSensor;
                            break;
                        case "PILOT":
                            intMaxValue = Parent.MaxPilot;
                            break;
                        default:
                            if (Name.StartsWith("Pilot Program", StringComparison.OrdinalIgnoreCase))
                            {
                                intMaxValue = Parent.MaxPilot;
                            }
                            break;
                    }
                }

                return Math.Min(intReturn, intMaxValue);
            }
        }

        public async Task<int> GetMaxRatingAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            string strText = _strMaxRating.ToUpperInvariant();
            if (string.IsNullOrEmpty(strText))
                return 0;
            int intReturn = 0;
            switch (strText)
            {
                case "QTY":
                    intReturn = Vehicle.MaxWheels;
                    break;
                case "SEATS":
                    intReturn = await Parent.GetTotalSeatsAsync(this, token).ConfigureAwait(false);
                    break;
                case "BODY":
                    intReturn = await Parent.GetTotalBodyAsync(this, token).ConfigureAwait(false);
                    break;
                default:
                    int.TryParse(strText, out intReturn);
                    break;
            }

            int intMaxValue = int.MaxValue;
            if (Name.StartsWith("Armor", StringComparison.OrdinalIgnoreCase))
                intMaxValue = await Parent.GetMaxArmorAsync(token).ConfigureAwait(false);
            else
            {
                switch (Category.ToUpperInvariant())
                {
                    case "HANDLING":
                        intMaxValue = await Parent.GetMaxHandlingAsync(token).ConfigureAwait(false);
                        break;
                    case "SPEED":
                        intMaxValue = await Parent.GetMaxSpeedAsync(token).ConfigureAwait(false);
                        break;
                    case "ACCELERATION":
                        intMaxValue = await Parent.GetMaxAccelerationAsync(token).ConfigureAwait(false);
                        break;
                    case "SENSOR":
                        intMaxValue = await Parent.GetMaxSensorAsync(token).ConfigureAwait(false);
                        break;
                    case "PILOT":
                        intMaxValue = await Parent.GetMaxPilotAsync(token).ConfigureAwait(false);
                        break;
                    default:
                        if (Name.StartsWith("Pilot Program", StringComparison.OrdinalIgnoreCase))
                        {
                            intMaxValue = await Parent.GetMaxPilotAsync(token).ConfigureAwait(false);
                        }
                        break;
                }
            }

            return Math.Min(intReturn, intMaxValue);
        }

        public string RatingLabel
        {
            get => _strRatingLabel;
            set => _strRatingLabel = value;
        }

        /// <summary>
        /// Cost.
        /// </summary>
        public string Cost
        {
            get => _strCost;
            set => _strCost = value;
        }

        /// <summary>
        /// Markup.
        /// </summary>
        public decimal Markup
        {
            get => _decMarkup;
            set => _decMarkup = value;
        }

        /// <summary>
        /// Availability.
        /// </summary>
        public string Avail
        {
            get => _strAvail;
            set => _strAvail = value;
        }

        /// <summary>
        /// Sourcebook.
        /// </summary>
        public string Source
        {
            get => _strSource;
            set => _strSource = value;
        }

        /// <summary>
        /// Sourcebook Page Number.
        /// </summary>
        public string Page
        {
            get => _strPage;
            set => _strPage = value;
        }

        /// <summary>
        /// Sourcebook Page Number using a given language file.
        /// Returns Page if not found or the string is empty.
        /// </summary>
        /// <param name="strLanguage">Language file keyword to use.</param>
        /// <returns></returns>
        public string DisplayPage(string strLanguage)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Page;
            string s = this.GetNodeXPath(strLanguage)?.SelectSingleNodeAndCacheExpression("altpage")?.Value ?? Page;
            return !string.IsNullOrWhiteSpace(s) ? s : Page;
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
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Page;
            XPathNavigator objNode = await this.GetNodeXPathAsync(strLanguage, token: token).ConfigureAwait(false);
            string strReturn = objNode?.SelectSingleNodeAndCacheExpression("altpage", token: token)?.Value ?? Page;
            return !string.IsNullOrWhiteSpace(strReturn) ? strReturn : Page;
        }

        /// <summary>
        /// Bonus node.
        /// </summary>
        public XmlNode Bonus
        {
            get => _nodBonus;
            set
            {
                XmlNode xmlOldValue = Interlocked.Exchange(ref _nodBonus, value);
                if (xmlOldValue != value && !IncludedInVehicle && Equipped)
                {
                    if (_objParent != null && (xmlOldValue?["sensor"] != null || value?["sensor"] != null))
                    {
                        // Any time any vehicle mod is changed, update our sensory array's rating, just in case
                        Gear objGear = _objParent.GearChildren
                            .FirstOrDefault(x =>
                                x.Category == "Sensors" && x.Name == "Sensor Array" && x.IncludedInParent);
                        if (objGear != null)
                            objGear.Rating = _objParent.CalculatedSensor;
                    }
                    if (_objCharacter.IsAI && _objCharacter.HomeNode is Vehicle objVehicle && objVehicle == _objParent)
                        _objCharacter.OnPropertyChanged(nameof(Character.PhysicalCM));
                }
            }
        }

        /// <summary>
        /// Wireless Bonus node.
        /// </summary>
        public XmlNode WirelessBonus
        {
            get => _nodWirelessBonus;
            set
            {
                XmlNode xmlOldValue = Interlocked.Exchange(ref _nodWirelessBonus, value);
                if (xmlOldValue != value && !IncludedInVehicle && Equipped && WirelessOn)
                {
                    if (_objParent != null && (xmlOldValue?["sensor"] != null || value?["sensor"] != null))
                    {
                        // Any time any vehicle mod is changed, update our sensory array's rating, just in case
                        Gear objGear = _objParent.GearChildren
                            .FirstOrDefault(x =>
                                x.Category == "Sensors" && x.Name == "Sensor Array" && x.IncludedInParent);
                        if (objGear != null)
                            objGear.Rating = _objParent.CalculatedSensor;
                    }
                    if (_objCharacter.IsAI && _objCharacter.HomeNode is Vehicle objVehicle && objVehicle == _objParent)
                        _objCharacter.OnPropertyChanged(nameof(Character.PhysicalCM));
                }
            }
        }

        /// <summary>
        /// Whether the vehicle mod's wireless is enabled
        /// </summary>
        public bool WirelessOn
        {
            get => _blnWirelessOn;
            set
            {
                _blnWirelessOn = value;
                if (!IncludedInVehicle && Equipped)
                {
                    if (_objParent != null && WirelessBonus?["sensor"] != null)
                    {
                        // Any time any vehicle mod is changed, update our sensory array's rating, just in case
                        Gear objGear = _objParent.GearChildren
                            .FirstOrDefault(x =>
                                x.Category == "Sensors" && x.Name == "Sensor Array" && x.IncludedInParent);
                        if (objGear != null)
                            objGear.Rating = _objParent.CalculatedSensor;
                    }
                    if (_objCharacter.IsAI && _objCharacter.HomeNode is Vehicle objVehicle && objVehicle == _objParent)
                        _objCharacter.OnPropertyChanged(nameof(Character.PhysicalCM));
                }
            }
        }

        /// <summary>
        /// Whether the Mod included with the Vehicle by default.
        /// </summary>
        public bool IncludedInVehicle
        {
            get => _blnIncludeInVehicle;
            set
            {
                _blnIncludeInVehicle = value;
                if (Equipped)
                {
                    if (_objParent != null && (Bonus?["sensor"] != null || (WirelessOn && WirelessBonus?["sensor"] != null)))
                    {
                        // Any time any vehicle mod is changed, update our sensory array's rating, just in case
                        Gear objGear = _objParent.GearChildren
                            .FirstOrDefault(x =>
                                x.Category == "Sensors" && x.Name == "Sensor Array" && x.IncludedInParent);
                        if (objGear != null)
                            objGear.Rating = _objParent.CalculatedSensor;
                    }
                    if (_objCharacter.IsAI && _objCharacter.HomeNode is Vehicle objVehicle && objVehicle == _objParent)
                        _objCharacter.OnPropertyChanged(nameof(Character.PhysicalCM));
                }
            }
        }

        /// <summary>
        /// Whether this Mod is installed and contributing towards the Vehicle's stats.
        /// </summary>
        public bool Equipped
        {
            get => _blnEquipped;
            set
            {
                _blnEquipped = value;
                if (!IncludedInVehicle)
                {
                    if (_objParent != null && (Bonus?["sensor"] != null || (WirelessOn && WirelessBonus?["sensor"] != null)))
                    {
                        // Any time any vehicle mod is changed, update our sensory array's rating, just in case
                        Gear objGear = _objParent.GearChildren
                            .FirstOrDefault(x =>
                                x.Category == "Sensors" && x.Name == "Sensor Array" && x.IncludedInParent);
                        if (objGear != null)
                            objGear.Rating = _objParent.CalculatedSensor;
                    }
                    if (_objCharacter.IsAI && _objCharacter.HomeNode is Vehicle objVehicle && objVehicle == _objParent)
                        _objCharacter.OnPropertyChanged(nameof(Character.PhysicalCM));
                }
            }
        }

        /// <summary>
        /// Notes.
        /// </summary>
        public string Notes
        {
            get => _strNotes;
            set => _strNotes = value;
        }

        /// <summary>
        /// Forecolor to use for Notes in treeviews.
        /// </summary>
        public Color NotesColor
        {
            get => _colNotes;
            set => _colNotes = value;
        }

        /// <summary>
        /// Whether the Vehicle Mod allows Cyberware Plugins.
        /// </summary>
        public bool AllowCyberware => !string.IsNullOrEmpty(_strSubsystems);

        /// <summary>
        /// Allowed Cyberware Subsystems.
        /// </summary>
        public string Subsystems
        {
            get => _strSubsystems;
            set => _strSubsystems = value;
        }

        /// <summary>
        /// Value that was selected during an ImprovementManager dialogue.
        /// </summary>
        public string Extra
        {
            get => _strExtra;
            set => _strExtra = value;
        }

        /// <summary>
        /// Whether the Vehicle Mod's cost should be discounted by 10% through the Black Market Pipeline Quality.
        /// </summary>
        public bool DiscountCost
        {
            get => _blnDiscountCost;
            set => _blnDiscountCost = value;
        }

        /// <summary>
        /// Whether the Vehicle Mod is a downgrade for drone attributes
        /// </summary>
        public bool Downgrade => _blnDowngrade;

        /// <summary>
        /// Bonus/Penalty to the parent vehicle that this mod provides.
        /// </summary>
        public int ConditionMonitor => _intConditionMonitor;

        /// <summary>
        /// Vehicle that the Mod is attached to.
        /// </summary>
        public Vehicle Parent
        {
            get => _objParent;
            set
            {
                if (Interlocked.Exchange(ref _objParent, value) == value)
                    return;
                if (WeaponMountParent?.Parent != value)
                    WeaponMountParent = null;
                foreach (Weapon objChild in Weapons)
                    objChild.ParentVehicle = value;
                foreach (Cyberware objCyberware in Cyberware)
                    objCyberware.ParentVehicle = value;
            }
        }

        /// <summary>
        /// Adjust the Weapon's Ammo amount by the specified flat value.
        /// </summary>
        public int AmmoBonus
        {
            get => _intAmmoBonus;
            set => _intAmmoBonus = value;
        }

        /// <summary>
        /// Adjust the Weapon's Ammo amount by the specified percentage.
        /// </summary>
        public decimal AmmoBonusPercent
        {
            get => _decAmmoBonusPercent;
            set => _decAmmoBonusPercent = value;
        }

        /// <summary>
        /// Replace the Weapon's Ammo value with the Weapon Mod's value.
        /// </summary>
        public string AmmoReplace
        {
            get => _strAmmoReplace;
            set => _strAmmoReplace = value;
        }

        /// <summary>
        /// Used by our sorting algorithm to remember which order the user moves things to
        /// </summary>
        public int SortOrder
        {
            get => _intSortOrder;
            set => _intSortOrder = value;
        }

        /// <summary>
        /// Is the object stolen via the Stolen Gear quality?
        /// </summary>
        public bool Stolen
        {
            get => _blnStolen;
            set => _blnStolen = value;
        }

        #endregion Properties

        #region Complex Properties

        /// <summary>
        /// Total Availability in the program's current language.
        /// </summary>
        public string DisplayTotalAvail => TotalAvail(GlobalSettings.CultureInfo, GlobalSettings.Language);

        /// <summary>
        /// Total Availability in the program's current language.
        /// </summary>
        public Task<string> GetDisplayTotalAvailAsync(CancellationToken token = default) => TotalAvailAsync(GlobalSettings.CultureInfo, GlobalSettings.Language, token);

        /// <summary>
        /// Total Availability of the VehicleMod.
        /// </summary>
        public string TotalAvail(CultureInfo objCulture, string strLanguage)
        {
            return TotalAvailTuple().ToString(objCulture, strLanguage);
        }

        /// <summary>
        /// Calculated Availability of the Vehicle.
        /// </summary>
        public async Task<string> TotalAvailAsync(CultureInfo objCulture, string strLanguage, CancellationToken token = default)
        {
            return await (await TotalAvailTupleAsync(token: token).ConfigureAwait(false)).ToStringAsync(objCulture, strLanguage, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Total Availability as a triple.
        /// </summary>
        public AvailabilityValue TotalAvailTuple(bool blnCheckChildren = true)
        {
            bool blnModifyParentAvail = false;
            string strAvail = Avail;
            char chrLastAvailChar = ' ';
            int intAvail = 0;
            if (strAvail.Length > 0)
            {
                // Reordered to process fixed value strings
                if (strAvail.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strAvail.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strAvail = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                if (strAvail.StartsWith("Range(", StringComparison.Ordinal))
                {
                    // If the Availability code is based on the current Rating of the item, separate the Availability string into an array and find the first bracket that the Rating is lower than or equal to.
                    string[] strValues = strAvail.CheapReplace("MaxRating", () => MaxRating.ToString(GlobalSettings.InvariantCultureInfo)).TrimStartOnce("Range(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string strValue in strValues)
                    {
                        string[] astrValue = strValue.Split('[');
                        string strAvailCode = astrValue[1].Trim('[', ']');
                        int.TryParse(astrValue[0], NumberStyles.Any, GlobalSettings.InvariantCultureInfo,
                            out int intMax);
                        if (Rating > intMax)
                            continue;
                        strAvail = Rating.ToString(GlobalSettings.InvariantCultureInfo) + strAvailCode;
                        break;
                    }
                }

                chrLastAvailChar = strAvail[strAvail.Length - 1];
                if (chrLastAvailChar == 'F' || chrLastAvailChar == 'R')
                {
                    strAvail = strAvail.Substring(0, strAvail.Length - 1);
                }

                blnModifyParentAvail = strAvail.StartsWith('+', '-');

                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdAvail))
                {
                    sbdAvail.Append(strAvail.TrimStart('+'));
                    sbdAvail.Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo));
                    _objCharacter.AttributeSection.ProcessAttributesInXPath(sbdAvail, strAvail);
                    // If the availability is determined by the Rating, evaluate the expression.
                    sbdAvail.CheapReplace(strAvail, "Vehicle Cost",
                                          () => Parent?.OwnCost.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    // If the Body is 0 (Microdrone), treat it as 0.5 for the purposes of determine Modification cost.
                    sbdAvail.CheapReplace(strAvail, "Body",
                                          () => Parent?.Body > 0
                                              ? Parent.Body.ToString(GlobalSettings.InvariantCultureInfo)
                                              : "0.5");
                    sbdAvail.CheapReplace(strAvail, "Armor",
                                          () => Parent?.Armor.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdAvail.CheapReplace(strAvail, "Speed",
                                          () => Parent?.Speed.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdAvail.CheapReplace(strAvail, "Acceleration",
                                          () => Parent?.Accel.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdAvail.CheapReplace(strAvail, "Handling",
                                          () => Parent?.Handling.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdAvail.CheapReplace(strAvail, "Sensor",
                                          () => Parent?.BaseSensor.ToString(GlobalSettings.InvariantCultureInfo)
                                                ?? "0");
                    sbdAvail.CheapReplace(strAvail, "Pilot",
                                          () => Parent?.Pilot.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    (bool blnIsSuccess, object objProcess)
                        = CommonFunctions.EvaluateInvariantXPath(sbdAvail.ToString());
                    if (blnIsSuccess)
                        intAvail += ((double)objProcess).StandardRound();
                }
            }

            if (blnCheckChildren)
            {
                // Run through cyberware children and increase the Avail by any Mod whose Avail starts with "+" or "-".
                foreach (Cyberware objChild in Cyberware)
                {
                    if (objChild.ParentID != InternalId)
                    {
                        AvailabilityValue objLoopAvailTuple = objChild.TotalAvailTuple();
                        if (objLoopAvailTuple.AddToParent)
                            intAvail += objLoopAvailTuple.Value;
                        if (objLoopAvailTuple.Suffix == 'F')
                            chrLastAvailChar = 'F';
                        else if (chrLastAvailChar != 'F' && objLoopAvailTuple.Suffix == 'R')
                            chrLastAvailChar = 'R';
                    }
                }

                // Run through weapon children and increase the Avail by any Mod whose Avail starts with "+" or "-".
                foreach (Weapon objChild in Weapons)
                {
                    if (objChild.ParentID != InternalId)
                    {
                        AvailabilityValue objLoopAvailTuple = objChild.TotalAvailTuple();
                        if (objLoopAvailTuple.AddToParent)
                            intAvail += objLoopAvailTuple.Value;
                        if (objLoopAvailTuple.Suffix == 'F')
                            chrLastAvailChar = 'F';
                        else if (chrLastAvailChar != 'F' && objLoopAvailTuple.Suffix == 'R')
                            chrLastAvailChar = 'R';
                    }
                }
            }

            return new AvailabilityValue(intAvail, chrLastAvailChar, blnModifyParentAvail, IncludedInVehicle);
        }

        /// <summary>
        /// Total Availability as a triple.
        /// </summary>
        public async Task<AvailabilityValue> TotalAvailTupleAsync(bool blnCheckChildren = true, CancellationToken token = default)
        {
            bool blnModifyParentAvail = false;
            string strAvail = Avail;
            char chrLastAvailChar = ' ';
            int intAvail = 0;
            if (strAvail.Length > 0)
            {
                // Reordered to process fixed value strings
                if (strAvail.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strAvail.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strAvail = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                if (strAvail.StartsWith("Range(", StringComparison.Ordinal))
                {
                    // If the Availability code is based on the current Rating of the item, separate the Availability string into an array and find the first bracket that the Rating is lower than or equal to.
                    string[] strValues =
                        (await strAvail.CheapReplaceAsync("MaxRating",
                            async () => (await GetMaxRatingAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo),
                            token: token).ConfigureAwait(false)).TrimStartOnce("Range(", true).TrimEndOnce(')')
                        .Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string strValue in strValues)
                    {
                        string[] astrValue = strValue.Split('[');
                        string strAvailCode = astrValue[1].Trim('[', ']');
                        int.TryParse(astrValue[0], NumberStyles.Any, GlobalSettings.InvariantCultureInfo,
                            out int intMax);
                        if (Rating > intMax)
                            continue;
                        strAvail = Rating.ToString(GlobalSettings.InvariantCultureInfo) + strAvailCode;
                        break;
                    }
                }

                chrLastAvailChar = strAvail[strAvail.Length - 1];
                if (chrLastAvailChar == 'F' || chrLastAvailChar == 'R')
                {
                    strAvail = strAvail.Substring(0, strAvail.Length - 1);
                }

                blnModifyParentAvail = strAvail.StartsWith('+', '-');

                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdAvail))
                {
                    sbdAvail.Append(strAvail.TrimStart('+'));
                    sbdAvail.Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo));

                    await _objCharacter.AttributeSection.ProcessAttributesInXPathAsync(sbdAvail, strAvail, token: token).ConfigureAwait(false);

                    // If the availability is determined by the Rating, evaluate the expression.
                    await sbdAvail.CheapReplaceAsync(strAvail, "Vehicle Cost",
                                                     () => Parent?.OwnCost.ToString(GlobalSettings.InvariantCultureInfo) ?? "0", token: token).ConfigureAwait(false);
                    // If the Body is 0 (Microdrone), treat it as 0.5 for the purposes of determine Modification cost.
                    await sbdAvail.CheapReplaceAsync(strAvail, "Body",
                                                     () => Parent?.Body > 0
                                                         ? Parent.Body.ToString(GlobalSettings.InvariantCultureInfo)
                                                         : "0.5", token: token).ConfigureAwait(false);
                    await sbdAvail.CheapReplaceAsync(strAvail, "Armor",
                                                     () => Parent?.Armor.ToString(GlobalSettings.InvariantCultureInfo) ?? "0", token: token).ConfigureAwait(false);
                    await sbdAvail.CheapReplaceAsync(strAvail, "Speed",
                                                     () => Parent?.Speed.ToString(GlobalSettings.InvariantCultureInfo) ?? "0", token: token).ConfigureAwait(false);
                    await sbdAvail.CheapReplaceAsync(strAvail, "Acceleration",
                                                     () => Parent?.Accel.ToString(GlobalSettings.InvariantCultureInfo) ?? "0", token: token).ConfigureAwait(false);
                    await sbdAvail.CheapReplaceAsync(strAvail, "Handling",
                                                     () => Parent?.Handling.ToString(GlobalSettings.InvariantCultureInfo) ?? "0", token: token).ConfigureAwait(false);
                    await sbdAvail.CheapReplaceAsync(strAvail, "Sensor",
                                                     () => Parent?.BaseSensor.ToString(GlobalSettings.InvariantCultureInfo)
                                                           ?? "0", token: token).ConfigureAwait(false);
                    await sbdAvail.CheapReplaceAsync(strAvail, "Pilot",
                                                     async () => Parent != null ? (await Parent.GetPilotAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo) : "0", token: token).ConfigureAwait(false);
                    (bool blnIsSuccess, object objProcess)
                        = await CommonFunctions.EvaluateInvariantXPathAsync(sbdAvail.ToString(), token).ConfigureAwait(false);
                    if (blnIsSuccess)
                        intAvail += ((double)objProcess).StandardRound();
                }
            }

            if (blnCheckChildren)
            {
                // Run through cyberware children and increase the Avail by any Mod whose Avail starts with "+" or "-".
                intAvail += await Cyberware.SumAsync(async objChild =>
                {
                    if (objChild.ParentID == InternalId)
                        return 0;
                    AvailabilityValue objLoopAvailTuple
                        = await objChild.TotalAvailTupleAsync(token: token).ConfigureAwait(false);
                    if (objLoopAvailTuple.Suffix == 'F')
                        chrLastAvailChar = 'F';
                    else if (chrLastAvailChar != 'F' && objLoopAvailTuple.Suffix == 'R')
                        chrLastAvailChar = 'R';
                    return objLoopAvailTuple.AddToParent ? objLoopAvailTuple.Value : 0;
                }, token).ConfigureAwait(false) + await Weapons.SumAsync(async objChild =>
                {
                    if (objChild.ParentID == InternalId)
                        return 0;
                    AvailabilityValue objLoopAvailTuple
                        = await objChild.TotalAvailTupleAsync(token: token).ConfigureAwait(false);
                    if (objLoopAvailTuple.Suffix == 'F')
                        chrLastAvailChar = 'F';
                    else if (chrLastAvailChar != 'F' && objLoopAvailTuple.Suffix == 'R')
                        chrLastAvailChar = 'R';
                    return objLoopAvailTuple.AddToParent ? objLoopAvailTuple.Value : 0;
                }, token).ConfigureAwait(false);
            }

            return new AvailabilityValue(intAvail, chrLastAvailChar, blnModifyParentAvail, IncludedInVehicle);
        }

        /// <summary>
        /// Calculated Capacity of the Vehicle Mod.
        /// </summary>
        public string CalculatedCapacity
        {
            get
            {
                string strReturn = Capacity;
                if (string.IsNullOrEmpty(strReturn))
                    return 0.0m.ToString("#,0.##", GlobalSettings.CultureInfo);

                if (strReturn.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strReturn.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strReturn = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                int intPos = strReturn.IndexOf("/[", StringComparison.Ordinal);
                if (intPos != -1)
                {
                    string strFirstHalf = strReturn.Substring(0, intPos);
                    string strSecondHalf = strReturn.Substring(intPos + 1, strReturn.Length - intPos - 1);
                    bool blnSquareBrackets = strFirstHalf.StartsWith('[');

                    if (blnSquareBrackets && strFirstHalf.Length > 2)
                        strFirstHalf = strFirstHalf.Substring(1, strFirstHalf.Length - 2);

                    if (strFirstHalf == "[*]")
                        strReturn = "*";
                    else
                    {
                        if (strFirstHalf.StartsWith("FixedValues(", StringComparison.Ordinal))
                        {
                            string[] strValues = strFirstHalf.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                            strFirstHalf = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                        }

                        try
                        {
                            (bool blnIsSuccess, object objProcess) = CommonFunctions.EvaluateInvariantXPath(strFirstHalf.Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo)));
                            strReturn = blnIsSuccess ? ((double)objProcess).ToString("#,0.##", GlobalSettings.CultureInfo) : strFirstHalf;
                        }
                        catch (OverflowException) // Result is text and not a double
                        {
                            strReturn = strFirstHalf;
                        }
                        catch (InvalidCastException) // Result is text and not a double
                        {
                            strReturn = strFirstHalf;
                        }
                    }

                    if (blnSquareBrackets)
                        strReturn = '[' + strReturn + ']';

                    if (strSecondHalf.Contains("Rating"))
                    {
                        strSecondHalf = strSecondHalf.Trim('[', ']');
                        try
                        {
                            (bool blnIsSuccess, object objProcess) = CommonFunctions.EvaluateInvariantXPath(strSecondHalf.Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo)));
                            strSecondHalf = '[' + (blnIsSuccess ? ((double)objProcess).ToString("#,0.##", GlobalSettings.CultureInfo) : strSecondHalf) + ']';
                        }
                        catch (OverflowException) // Result is text and not a double
                        {
                            strSecondHalf = '[' + strSecondHalf + ']';
                        }
                        catch (InvalidCastException) // Result is text and not a double
                        {
                            strSecondHalf = '[' + strSecondHalf + ']';
                        }
                    }

                    strReturn += '/' + strSecondHalf;
                }
                else if (strReturn.Contains("Rating"))
                {
                    // If the Capacity is determined by the Rating, evaluate the expression.
                    // XPathExpression cannot evaluate while there are square brackets, so remove them if necessary.
                    bool blnSquareBrackets = strReturn.StartsWith('[');
                    string strCapacity = strReturn;
                    if (blnSquareBrackets)
                        strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                    (bool blnIsSuccess, object objProcess) = CommonFunctions.EvaluateInvariantXPath(strCapacity.Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo)));
                    strReturn = blnIsSuccess ? ((double)objProcess).ToString("#,0.##", GlobalSettings.CultureInfo) : strCapacity;
                    if (blnSquareBrackets)
                        strReturn = '[' + strReturn + ']';
                }
                else if (decimal.TryParse(strReturn, NumberStyles.Any, GlobalSettings.InvariantCultureInfo, out decimal decReturn))
                    return decReturn.ToString("#,0.##", GlobalSettings.CultureInfo);

                return strReturn;
            }
        }

        /// <summary>
        /// Calculated Capacity of the Vehicle Mod.
        /// </summary>
        public async Task<string> GetCalculatedCapacityAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            string strReturn = Capacity;
            if (string.IsNullOrEmpty(strReturn))
                return 0.0m.ToString("#,0.##", GlobalSettings.CultureInfo);

            if (strReturn.StartsWith("FixedValues(", StringComparison.Ordinal))
            {
                string[] strValues = strReturn.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                strReturn = strValues[Math.Max(Math.Min(await GetRatingAsync(token).ConfigureAwait(false), strValues.Length) - 1, 0)];
            }

            int intPos = strReturn.IndexOf("/[", StringComparison.Ordinal);
            if (intPos != -1)
            {
                string strFirstHalf = strReturn.Substring(0, intPos);
                string strSecondHalf = strReturn.Substring(intPos + 1, strReturn.Length - intPos - 1);
                bool blnSquareBrackets = strFirstHalf.StartsWith('[');

                if (blnSquareBrackets && strFirstHalf.Length > 2)
                    strFirstHalf = strFirstHalf.Substring(1, strFirstHalf.Length - 2);

                if (strFirstHalf == "[*]")
                    strReturn = "*";
                else
                {
                    if (strFirstHalf.StartsWith("FixedValues(", StringComparison.Ordinal))
                    {
                        string[] strValues = strFirstHalf.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                        strFirstHalf = strValues[Math.Max(Math.Min(await GetRatingAsync(token).ConfigureAwait(false), strValues.Length) - 1, 0)];
                    }

                    try
                    {
                        (bool blnIsSuccess, object objProcess) = await CommonFunctions.EvaluateInvariantXPathAsync(strFirstHalf.Replace("Rating", (await GetRatingAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo)), token).ConfigureAwait(false);
                        strReturn = blnIsSuccess ? ((double)objProcess).ToString("#,0.##", GlobalSettings.CultureInfo) : strFirstHalf;
                    }
                    catch (OverflowException) // Result is text and not a double
                    {
                        strReturn = strFirstHalf;
                    }
                    catch (InvalidCastException) // Result is text and not a double
                    {
                        strReturn = strFirstHalf;
                    }
                }

                if (blnSquareBrackets)
                    strReturn = '[' + strReturn + ']';

                if (strSecondHalf.Contains("Rating"))
                {
                    strSecondHalf = strSecondHalf.Trim('[', ']');
                    try
                    {
                        (bool blnIsSuccess, object objProcess) = await CommonFunctions.EvaluateInvariantXPathAsync(strSecondHalf.Replace("Rating", (await GetRatingAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo)), token).ConfigureAwait(false);
                        strSecondHalf = '[' + (blnIsSuccess ? ((double)objProcess).ToString("#,0.##", GlobalSettings.CultureInfo) : strSecondHalf) + ']';
                    }
                    catch (OverflowException) // Result is text and not a double
                    {
                        strSecondHalf = '[' + strSecondHalf + ']';
                    }
                    catch (InvalidCastException) // Result is text and not a double
                    {
                        strSecondHalf = '[' + strSecondHalf + ']';
                    }
                }

                strReturn += '/' + strSecondHalf;
            }
            else if (strReturn.Contains("Rating"))
            {
                // If the Capacity is determined by the Rating, evaluate the expression.
                // XPathExpression cannot evaluate while there are square brackets, so remove them if necessary.
                bool blnSquareBrackets = strReturn.StartsWith('[');
                string strCapacity = strReturn;
                if (blnSquareBrackets)
                    strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                (bool blnIsSuccess, object objProcess) = await CommonFunctions.EvaluateInvariantXPathAsync(strCapacity.Replace("Rating", (await GetRatingAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo)), token).ConfigureAwait(false);
                strReturn = blnIsSuccess ? ((double)objProcess).ToString("#,0.##", GlobalSettings.CultureInfo) : strCapacity;
                if (blnSquareBrackets)
                    strReturn = '[' + strReturn + ']';
            }
            else if (decimal.TryParse(strReturn, NumberStyles.Any, GlobalSettings.InvariantCultureInfo, out decimal decReturn))
                return decReturn.ToString("#,0.##", GlobalSettings.CultureInfo);

            return strReturn;
        }

        /// <summary>
        /// The amount of Capacity remaining in the Cyberware.
        /// </summary>
        public decimal CapacityRemaining
        {
            get
            {
                if (string.IsNullOrEmpty(Capacity))
                    return 0.0m;
                decimal decCapacity = 0;
                if (Capacity.Contains("/["))
                {
                    // Get the Cyberware base Capacity.
                    string strBaseCapacity = CalculatedCapacity;
                    strBaseCapacity = strBaseCapacity.Substring(0, strBaseCapacity.IndexOf('/'));
                    decCapacity = Convert.ToDecimal(strBaseCapacity, GlobalSettings.CultureInfo)
                                  // Run through its Children and deduct the Capacity costs.
                                  - Cyberware.Sum(objCyberware =>
                                  {
                                      string strCapacity = objCyberware.CalculatedCapacity;
                                      int intPos = strCapacity.IndexOf("/[", StringComparison.Ordinal);
                                      if (intPos != -1)
                                          strCapacity = strCapacity.Substring(intPos + 2,
                                              strCapacity.LastIndexOf(']') - intPos - 2);
                                      else if (strCapacity.StartsWith('['))
                                          strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                                      if (strCapacity == "*")
                                          strCapacity = "0";
                                      return Convert.ToDecimal(strCapacity, GlobalSettings.CultureInfo);
                                  });
                }
                else if (!Capacity.Contains('['))
                {
                    // Get the Cyberware base Capacity.
                    decCapacity = Convert.ToDecimal(CalculatedCapacity, GlobalSettings.CultureInfo)
                                  // Run through its Children and deduct the Capacity costs.
                                  - Cyberware.Sum(objCyberware =>
                                  {
                                      string strCapacity = objCyberware.CalculatedCapacity;
                                      int intPos = strCapacity.IndexOf("/[", StringComparison.Ordinal);
                                      if (intPos != -1)
                                          strCapacity = strCapacity.Substring(intPos + 2,
                                              strCapacity.LastIndexOf(']') - intPos - 2);
                                      else if (strCapacity.StartsWith('['))
                                          strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                                      if (strCapacity == "*")
                                          strCapacity = "0";
                                      return Convert.ToDecimal(strCapacity, GlobalSettings.CultureInfo);
                                  });
                }

                return decCapacity;
            }
        }

        /// <summary>
        /// The amount of Capacity remaining in the Cyberware.
        /// </summary>
        public async Task<decimal> GetCapacityRemainingAsync(CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(Capacity))
                return 0.0m;
            decimal decCapacity = 0;
            if (Capacity.Contains("/["))
            {
                // Get the Cyberware base Capacity.
                string strBaseCapacity = await GetCalculatedCapacityAsync(token).ConfigureAwait(false);
                strBaseCapacity = strBaseCapacity.Substring(0, strBaseCapacity.IndexOf('/'));
                decCapacity = Convert.ToDecimal(strBaseCapacity, GlobalSettings.CultureInfo)
                              // Run through its Children and deduct the Capacity costs.
                              - await Cyberware.SumAsync(async objCyberware =>
                              {
                                  string strCapacity = await objCyberware.GetCalculatedCapacityAsync(token).ConfigureAwait(false);
                                  int intPos = strCapacity.IndexOf("/[", StringComparison.Ordinal);
                                  if (intPos != -1)
                                      strCapacity = strCapacity.Substring(intPos + 2,
                                          strCapacity.LastIndexOf(']') - intPos - 2);
                                  else if (strCapacity.StartsWith('['))
                                      strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                                  if (strCapacity == "*") strCapacity = "0";
                                  return Convert.ToDecimal(strCapacity, GlobalSettings.CultureInfo);
                              }, token).ConfigureAwait(false);
            }
            else if (!Capacity.Contains('['))
            {
                // Get the Cyberware base Capacity.
                decCapacity = Convert.ToDecimal(await GetCalculatedCapacityAsync(token).ConfigureAwait(false), GlobalSettings.CultureInfo)
                              // Run through its Children and deduct the Capacity costs.
                              - await Cyberware.SumAsync(async objCyberware =>
                              {
                                  string strCapacity = await objCyberware.GetCalculatedCapacityAsync(token).ConfigureAwait(false);
                                  int intPos = strCapacity.IndexOf("/[", StringComparison.Ordinal);
                                  if (intPos != -1)
                                      strCapacity = strCapacity.Substring(intPos + 2,
                                          strCapacity.LastIndexOf(']') - intPos - 2);
                                  else if (strCapacity.StartsWith('['))
                                      strCapacity = strCapacity.Substring(1, strCapacity.Length - 2);
                                  if (strCapacity == "*") strCapacity = "0";
                                  return Convert.ToDecimal(strCapacity, GlobalSettings.CultureInfo);
                              }, token).ConfigureAwait(false);
            }

            return decCapacity;
        }

        public string DisplayCapacity
        {
            get
            {
                if (Capacity.Contains('[') && !Capacity.Contains("/["))
                    return CalculatedCapacity;
                return string.Format(GlobalSettings.CultureInfo, LanguageManager.GetString("String_CapacityRemaining"),
                    CalculatedCapacity, CapacityRemaining.ToString("#,0.##", GlobalSettings.CultureInfo));
            }
        }

        public async Task<string> GetDisplayCapacityAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (Capacity.Contains('[') && !Capacity.Contains("/["))
                return await GetCalculatedCapacityAsync(token).ConfigureAwait(false);
            return string.Format(GlobalSettings.CultureInfo, await LanguageManager.GetStringAsync("String_CapacityRemaining", token: token).ConfigureAwait(false),
                await GetCalculatedCapacityAsync(token).ConfigureAwait(false), (await GetCapacityRemainingAsync(token).ConfigureAwait(false)).ToString("#,0.##", GlobalSettings.CultureInfo));
        }

        /// <summary>
        /// Total cost of the VehicleMod.
        /// </summary>
        public async Task<decimal> TotalCostInMountCreation(int intSlots, CancellationToken token = default)
        {
            decimal decReturn = 0;
            if (!IncludedInVehicle)
            {
                // If the cost is determined by the Rating, evaluate the expression.
                string strCostExpr = Cost;
                if (strCostExpr.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strCostExpr.TrimStartOnce("FixedValues(", true).TrimEndOnce(')')
                                                    .Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strCostExpr = strValues[Math.Max(Math.Min(await GetRatingAsync(token).ConfigureAwait(false), strValues.Length) - 1, 0)];
                }

                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdCost))
                {
                    sbdCost.Append(strCostExpr.TrimStart('+'));
                    sbdCost.Replace("Rating", (await GetRatingAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo));

                    await _objCharacter.AttributeSection.ProcessAttributesInXPathAsync(sbdCost, strCostExpr, token: token).ConfigureAwait(false);

                    await sbdCost.CheapReplaceAsync(strCostExpr, "Vehicle Cost",
                                                    async () => Parent != null
                                                        ? (await Parent.GetOwnCostAsync(token).ConfigureAwait(false)).ToString(
                                                            GlobalSettings.InvariantCultureInfo)
                                                        : "0", token: token).ConfigureAwait(false);
                    // If the Body is 0 (Microdrone), treat it as 0.5 for the purposes of determine Modification cost.
                    await sbdCost.CheapReplaceAsync(strCostExpr, "Body",
                                                    () => Parent?.Body > 0
                                                        ? Parent.Body.ToString(GlobalSettings.InvariantCultureInfo)
                                                        : "0.5", token: token).ConfigureAwait(false);
                    await sbdCost.CheapReplaceAsync(strCostExpr, "Armor",
                                                    () => Parent?.Armor.ToString(GlobalSettings.InvariantCultureInfo)
                                                          ?? "0", token: token).ConfigureAwait(false);
                    await sbdCost.CheapReplaceAsync(strCostExpr, "Speed",
                                                    () => Parent?.Speed.ToString(GlobalSettings.InvariantCultureInfo)
                                                          ?? "0", token: token).ConfigureAwait(false);
                    await sbdCost.CheapReplaceAsync(strCostExpr, "Acceleration",
                                                    () => Parent?.Accel.ToString(GlobalSettings.InvariantCultureInfo)
                                                          ?? "0", token: token).ConfigureAwait(false);
                    await sbdCost.CheapReplaceAsync(strCostExpr, "Handling",
                                                    () => Parent?.Handling.ToString(GlobalSettings.InvariantCultureInfo)
                                                          ?? "0", token: token).ConfigureAwait(false);
                    await sbdCost.CheapReplaceAsync(strCostExpr, "Sensor",
                                                    () => Parent?.BaseSensor.ToString(GlobalSettings.InvariantCultureInfo)
                                                          ?? "0", token: token).ConfigureAwait(false);
                    await sbdCost.CheapReplaceAsync(strCostExpr, "Pilot",
                        async () => Parent != null
                            ? (await Parent.GetPilotAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo)
                            : "0", token: token).ConfigureAwait(false);
                    await sbdCost.CheapReplaceAsync(strCostExpr, "Slots",
                        async () => WeaponMountParent != null
                            ? (await WeaponMountParent.GetCalculatedSlotsAsync(token).ConfigureAwait(false)).ToString(
                                GlobalSettings.InvariantCultureInfo)
                            : "0", token: token).ConfigureAwait(false);
                    sbdCost.Replace("Slots", intSlots.ToString(GlobalSettings.InvariantCultureInfo));

                    (bool blnIsSuccess, object objProcess)
                        = await CommonFunctions.EvaluateInvariantXPathAsync(sbdCost.ToString(), token).ConfigureAwait(false);
                    if (blnIsSuccess)
                        decReturn = Convert.ToDecimal(objProcess, GlobalSettings.InvariantCultureInfo);
                }

                if (DiscountCost)
                    decReturn *= 0.9m;

                // Apply a markup if applicable.
                if (_decMarkup != 0)
                {
                    decReturn *= 1 + _decMarkup / 100.0m;
                }
            }

            return decReturn + await Weapons.SumAsync(x => x.ParentID != InternalId, x => x.GetTotalCostAsync(token), token).ConfigureAwait(false)
                             + await Cyberware.SumAsync(x => x.ParentID != InternalId, x => x.GetTotalCostAsync(token), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Total cost of the VehicleMod.
        /// </summary>
        public decimal TotalCost => OwnCost + Weapons.Sum(x => x.TotalCost) + Cyberware.Sum(x => x.TotalCost);

        public async Task<decimal> GetTotalCostAsync(CancellationToken token = default)
        {
            return await GetOwnCostAsync(token).ConfigureAwait(false)
                   + await Weapons.SumAsync(x => x.GetTotalCostAsync(token), token).ConfigureAwait(false)
                   + await Cyberware.SumAsync(x => x.GetTotalCostAsync(token), token).ConfigureAwait(false);
        }

        /// <summary>
        /// The cost of just the Vehicle Mod itself.
        /// </summary>
        public decimal OwnCost
        {
            get
            {
                decimal decReturn = 0;
                // If the cost is determined by the Rating, evaluate the expression.
                string strCostExpr = Cost;
                if (strCostExpr.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strCostExpr.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strCostExpr = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdCost))
                {
                    sbdCost.Append(strCostExpr.TrimStart('+'));
                    sbdCost.Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo));
                    _objCharacter.AttributeSection.ProcessAttributesInXPath(sbdCost, strCostExpr);
                    sbdCost.CheapReplace(strCostExpr, "Vehicle Cost",
                                         () => Parent?.OwnCost.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    // If the Body is 0 (Microdrone), treat it as 0.5 for the purposes of determine Modification cost.
                    sbdCost.CheapReplace(strCostExpr, "Body",
                                         () => Parent?.Body > 0
                                             ? Parent.Body.ToString(GlobalSettings.InvariantCultureInfo)
                                             : "0.5");
                    sbdCost.CheapReplace(strCostExpr, "Armor",
                                         () => Parent?.Armor.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdCost.CheapReplace(strCostExpr, "Speed",
                                         () => Parent?.Speed.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdCost.CheapReplace(strCostExpr, "Acceleration",
                                         () => Parent?.Accel.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdCost.CheapReplace(strCostExpr, "Handling",
                                         () => Parent?.Handling.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdCost.CheapReplace(strCostExpr, "Sensor",
                                         () => Parent?.BaseSensor.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdCost.CheapReplace(strCostExpr, "Pilot",
                                         () => Parent?.Pilot.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdCost.CheapReplace(strCostExpr, "Slots",
                                         () => WeaponMountParent?.CalculatedSlots.ToString(
                                             GlobalSettings.InvariantCultureInfo) ?? "0");

                    (bool blnIsSuccess, object objProcess)
                        = CommonFunctions.EvaluateInvariantXPath(sbdCost.ToString());
                    if (blnIsSuccess)
                        decReturn = Convert.ToDecimal(objProcess, GlobalSettings.InvariantCultureInfo);
                }

                if (DiscountCost)
                    decReturn *= 0.9m;

                // Apply a markup if applicable.
                if (_decMarkup != 0)
                {
                    decReturn *= 1 + _decMarkup / 100.0m;
                }

                return decReturn;
            }
        }

        /// <summary>
        /// The cost of just the Vehicle Mod itself.
        /// </summary>
        public async Task<decimal> GetOwnCostAsync(CancellationToken token = default)
        {
            decimal decReturn = 0;
            // If the cost is determined by the Rating, evaluate the expression.
            string strCostExpr = Cost;
            if (strCostExpr.StartsWith("FixedValues(", StringComparison.Ordinal))
            {
                string[] strValues = strCostExpr.TrimStartOnce("FixedValues(", true).TrimEndOnce(')')
                                                .Split(',', StringSplitOptions.RemoveEmptyEntries);
                strCostExpr = strValues[Math.Max(Math.Min(await GetRatingAsync(token).ConfigureAwait(false), strValues.Length) - 1, 0)];
            }

            using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdCost))
            {
                sbdCost.Append(strCostExpr.TrimStart('+'));
                sbdCost.Replace("Rating", (await GetRatingAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo));
                await _objCharacter.AttributeSection.ProcessAttributesInXPathAsync(sbdCost, strCostExpr, token: token).ConfigureAwait(false);
                await sbdCost.CheapReplaceAsync(strCostExpr, "Vehicle Cost",
                                                async () => Parent != null
                                                    ? (await Parent.GetOwnCostAsync(token).ConfigureAwait(false)).ToString(
                                                        GlobalSettings.InvariantCultureInfo)
                                                    : "0", token: token).ConfigureAwait(false);
                // If the Body is 0 (Microdrone), treat it as 0.5 for the purposes of determine Modification cost.
                await sbdCost.CheapReplaceAsync(strCostExpr, "Body",
                                                () => Parent?.Body > 0
                                                    ? Parent.Body.ToString(GlobalSettings.InvariantCultureInfo)
                                                    : "0.5", token: token).ConfigureAwait(false);
                await sbdCost.CheapReplaceAsync(strCostExpr, "Armor",
                                                () => Parent?.Armor.ToString(GlobalSettings.InvariantCultureInfo)
                                                      ?? "0", token: token).ConfigureAwait(false);
                await sbdCost.CheapReplaceAsync(strCostExpr, "Speed",
                                                () => Parent?.Speed.ToString(GlobalSettings.InvariantCultureInfo)
                                                      ?? "0", token: token).ConfigureAwait(false);
                await sbdCost.CheapReplaceAsync(strCostExpr, "Acceleration",
                                                () => Parent?.Accel.ToString(GlobalSettings.InvariantCultureInfo)
                                                      ?? "0", token: token).ConfigureAwait(false);
                await sbdCost.CheapReplaceAsync(strCostExpr, "Handling",
                                                () => Parent?.Handling.ToString(GlobalSettings.InvariantCultureInfo)
                                                      ?? "0", token: token).ConfigureAwait(false);
                await sbdCost.CheapReplaceAsync(strCostExpr, "Sensor",
                                                () => Parent?.BaseSensor.ToString(GlobalSettings.InvariantCultureInfo)
                                                      ?? "0", token: token).ConfigureAwait(false);
                await sbdCost.CheapReplaceAsync(strCostExpr, "Pilot",
                    async () => Parent != null
                        ? (await Parent.GetPilotAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo)
                        : "0", token: token).ConfigureAwait(false);
                await sbdCost.CheapReplaceAsync(strCostExpr, "Slots",
                    async () => WeaponMountParent != null
                        ? (await WeaponMountParent
                            .GetCalculatedSlotsAsync(token).ConfigureAwait(false)).ToString(
                            GlobalSettings.InvariantCultureInfo)
                        : "0", token: token).ConfigureAwait(false);

                (bool blnIsSuccess, object objProcess)
                    = await CommonFunctions.EvaluateInvariantXPathAsync(sbdCost.ToString(), token).ConfigureAwait(false);
                if (blnIsSuccess)
                    decReturn = Convert.ToDecimal(objProcess, GlobalSettings.InvariantCultureInfo);
            }

            if (DiscountCost)
                decReturn *= 0.9m;

            // Apply a markup if applicable.
            if (_decMarkup != 0)
            {
                decReturn *= 1 + _decMarkup / 100.0m;
            }

            return decReturn;
        }

        /// <summary>
        /// The number of Slots the Mod consumes.
        /// </summary>
        public int CalculatedSlots
        {
            get
            {
                string strSlotsExpression = Slots;
                if (strSlotsExpression.StartsWith("FixedValues(", StringComparison.Ordinal))
                {
                    string[] strValues = strSlotsExpression.TrimStartOnce("FixedValues(", true).TrimEndOnce(')').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    strSlotsExpression = strValues[Math.Max(Math.Min(Rating, strValues.Length) - 1, 0)];
                }

                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdReturn))
                {
                    sbdReturn.Append(strSlotsExpression.TrimStart('+'));
                    sbdReturn.Replace("Rating", Rating.ToString(GlobalSettings.InvariantCultureInfo));
                    _objCharacter.AttributeSection.ProcessAttributesInXPath(sbdReturn, strSlotsExpression);
                    sbdReturn.CheapReplace(strSlotsExpression, "Vehicle Cost",
                                           () => Parent?.OwnCost.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    // If the Body is 0 (Microdrone), treat it as 0.5 for the purposes of determine Modification cost.
                    sbdReturn.CheapReplace(strSlotsExpression, "Body",
                                           () => Parent?.Body > 0
                                               ? Parent.Body.ToString(GlobalSettings.InvariantCultureInfo)
                                               : "0.5");
                    sbdReturn.CheapReplace(strSlotsExpression, "Armor",
                                           () => Parent?.Armor.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdReturn.CheapReplace(strSlotsExpression, "Speed",
                                           () => Parent?.Speed.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdReturn.CheapReplace(strSlotsExpression, "Acceleration",
                                           () => Parent?.Accel.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdReturn.CheapReplace(strSlotsExpression, "Handling",
                                           () => Parent?.Handling.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    sbdReturn.CheapReplace(strSlotsExpression, "Sensor",
                                           () => Parent?.BaseSensor.ToString(GlobalSettings.InvariantCultureInfo)
                                                 ?? "0");
                    sbdReturn.CheapReplace(strSlotsExpression, "Pilot",
                                           () => Parent?.Pilot.ToString(GlobalSettings.InvariantCultureInfo) ?? "0");
                    (bool blnIsSuccess, object objProcess)
                        = CommonFunctions.EvaluateInvariantXPath(sbdReturn.ToString());
                    return blnIsSuccess ? ((double)objProcess).StandardRound() : 0;
                }
            }
        }

        /// <summary>
        /// The number of Slots the Mod consumes.
        /// </summary>
        public async Task<int> GetCalculatedSlotsAsync(CancellationToken token = default)
        {
            string strSlotsExpression = Slots;
            if (strSlotsExpression.StartsWith("FixedValues(", StringComparison.Ordinal))
            {
                string[] strValues = strSlotsExpression.TrimStartOnce("FixedValues(", true).TrimEndOnce(')')
                                                       .Split(',', StringSplitOptions.RemoveEmptyEntries);
                strSlotsExpression = strValues[Math.Max(Math.Min(await GetRatingAsync(token).ConfigureAwait(false), strValues.Length) - 1, 0)];
            }

            using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool, out StringBuilder sbdReturn))
            {
                sbdReturn.Append(strSlotsExpression.TrimStart('+'));
                sbdReturn.Replace("Rating", (await GetRatingAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo));
                await _objCharacter.AttributeSection.ProcessAttributesInXPathAsync(sbdReturn, strSlotsExpression, token: token).ConfigureAwait(false);
                await sbdReturn.CheapReplaceAsync(strSlotsExpression, "Vehicle Cost",
                                                  async () => Parent != null
                                                      ? (await Parent.GetOwnCostAsync(token).ConfigureAwait(false)).ToString(
                                                          GlobalSettings.InvariantCultureInfo)
                                                      : "0", token: token).ConfigureAwait(false);
                // If the Body is 0 (Microdrone), treat it as 0.5 for the purposes of determine Modification cost.
                await sbdReturn.CheapReplaceAsync(strSlotsExpression, "Body",
                                                  () => Parent?.Body > 0
                                                      ? Parent.Body.ToString(GlobalSettings.InvariantCultureInfo)
                                                      : "0.5", token: token).ConfigureAwait(false);
                await sbdReturn.CheapReplaceAsync(strSlotsExpression, "Armor",
                                                  () => Parent?.Armor.ToString(GlobalSettings.InvariantCultureInfo)
                                                        ?? "0", token: token).ConfigureAwait(false);
                await sbdReturn.CheapReplaceAsync(strSlotsExpression, "Speed",
                                                  () => Parent?.Speed.ToString(GlobalSettings.InvariantCultureInfo)
                                                        ?? "0", token: token).ConfigureAwait(false);
                await sbdReturn.CheapReplaceAsync(strSlotsExpression, "Acceleration",
                                                  () => Parent?.Accel.ToString(GlobalSettings.InvariantCultureInfo)
                                                        ?? "0", token: token).ConfigureAwait(false);
                await sbdReturn.CheapReplaceAsync(strSlotsExpression, "Handling",
                                                  () => Parent?.Handling.ToString(GlobalSettings.InvariantCultureInfo)
                                                        ?? "0", token: token).ConfigureAwait(false);
                await sbdReturn.CheapReplaceAsync(strSlotsExpression, "Sensor",
                                                  () => Parent?.BaseSensor.ToString(GlobalSettings.InvariantCultureInfo)
                                                        ?? "0", token: token).ConfigureAwait(false);
                await sbdReturn.CheapReplaceAsync(strSlotsExpression, "Pilot",
                    async () => Parent != null
                        ? (await Parent.GetPilotAsync(token).ConfigureAwait(false)).ToString(GlobalSettings.InvariantCultureInfo)
                        : "0", token: token).ConfigureAwait(false);
                (bool blnIsSuccess, object objProcess)
                    = await CommonFunctions.EvaluateInvariantXPathAsync(sbdReturn.ToString(), token).ConfigureAwait(false);
                return blnIsSuccess ? ((double) objProcess).StandardRound() : 0;
            }
        }

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public string DisplayNameShort(string strLanguage)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Name;

            return this.GetNodeXPath(strLanguage)?.SelectSingleNodeAndCacheExpression("translate")?.Value ?? Name;
        }

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public async Task<string> DisplayNameShortAsync(string strLanguage, CancellationToken token = default)
        {
            if (strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                return Name;

            XPathNavigator objNode = await this.GetNodeXPathAsync(strLanguage, token: token).ConfigureAwait(false);
            return objNode?.SelectSingleNodeAndCacheExpression("translate", token: token)?.Value ?? Name;
        }

        public string CurrentDisplayNameShort => DisplayNameShort(GlobalSettings.Language);

        public Task<string> GetCurrentDisplayNameShortAsync(CancellationToken token = default) =>
            DisplayNameShortAsync(GlobalSettings.Language, token);

        /// <summary>
        /// The name of the object as it should be displayed in lists. Qty Name (Rating) (Extra).
        /// </summary>
        public string DisplayName(CultureInfo objCulture, string strLanguage)
        {
            string strReturn = DisplayNameShort(strLanguage);
            string strSpace = LanguageManager.GetString("String_Space", strLanguage);
            if (!string.IsNullOrEmpty(Extra))
                strReturn += strSpace + '(' + _objCharacter.TranslateExtra(Extra, strLanguage) + ')';
            int intRating = Rating;
            if (intRating > 0)
                strReturn += strSpace + '(' + LanguageManager.GetString(RatingLabel, strLanguage) + strSpace + intRating.ToString(objCulture) + ')';
            return strReturn;
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Qty Name (Rating) (Extra).
        /// </summary>
        public async Task<string> DisplayNameAsync(CultureInfo objCulture, string strLanguage, CancellationToken token = default)
        {
            string strReturn = await DisplayNameShortAsync(strLanguage, token).ConfigureAwait(false);
            string strSpace = await LanguageManager.GetStringAsync("String_Space", strLanguage, token: token).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(Extra))
                strReturn += strSpace + '(' + await _objCharacter.TranslateExtraAsync(Extra, strLanguage, token: token).ConfigureAwait(false) + ')';
            int intRating = await GetRatingAsync(token).ConfigureAwait(false);
            if (intRating > 0)
                strReturn += strSpace + '(' + await LanguageManager.GetStringAsync(RatingLabel, strLanguage, token: token).ConfigureAwait(false) + strSpace + intRating.ToString(objCulture) + ')';
            return strReturn;
        }

        public string CurrentDisplayName => DisplayName(GlobalSettings.CultureInfo, GlobalSettings.Language);

        public Task<string> GetCurrentDisplayNameAsync(CancellationToken token = default) =>
            DisplayNameAsync(GlobalSettings.CultureInfo, GlobalSettings.Language, token);

        private static readonly string[] s_astrLimbStrings = { "ARM", "LEG" };

        /// <summary>
        /// Vehicle arm/leg Strength.
        /// </summary>
        public int TotalStrength
        {
            get
            {
                if (!Name.ContainsAny(s_astrLimbStrings, StringComparison.OrdinalIgnoreCase))
                    return 0;
                int intAttribute = 0;
                int intBody = 1;
                if (Parent != null)
                {
                    intBody = Parent.TotalBody * 2;
                    intAttribute = Math.Max(Parent.TotalBody, 0);
                }
                int intBonus = 0;

                foreach (Cyberware objChild in Cyberware)
                {
                    switch (objChild.Name)
                    {
                        // If the limb has Customized Strength, this is its new base value.
                        case "Customized Strength":
                            intAttribute = objChild.Rating;
                            break;
                        // If the limb has Enhanced Strength, this adds to the limb's value.
                        case "Enhanced Strength":
                            intBonus = objChild.Rating;
                            break;
                    }
                }
                return Math.Min(intAttribute + intBonus, Math.Max(intBody, 1));
            }
        }

        /// <summary>
        /// Vehicle arm/leg Agility.
        /// </summary>
        public int TotalAgility
        {
            get
            {
                if (!Name.ContainsAny(s_astrLimbStrings, StringComparison.OrdinalIgnoreCase))
                    return 0;

                int intAttribute = 0;
                int intBody = 1;
                if (Parent != null)
                {
                    intBody = Parent.TotalBody * 2;
                    intAttribute = Math.Max(Parent.Pilot, 0);
                }
                int intBonus = 0;

                foreach (Cyberware objChild in Cyberware)
                {
                    switch (objChild.Name)
                    {
                        // If the limb has Customized Agility, this is its new base value.
                        case "Customized Agility":
                            intAttribute = objChild.Rating;
                            break;
                        // If the limb has Enhanced Agility, this adds to the limb's value.
                        case "Enhanced Agility":
                            intBonus = objChild.Rating;
                            break;
                    }
                }

                return Math.Min(intAttribute + intBonus, Math.Max(intBody, 1));
            }
        }

        /// <summary>
        /// Vehicle arm/leg Strength.
        /// </summary>
        public async Task<int> GetTotalStrengthAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!Name.ContainsAny(s_astrLimbStrings, StringComparison.OrdinalIgnoreCase))
                return 0;
            int intAttribute = 0;
            int intBody = 1;
            if (Parent != null)
            {
                intBody = await Parent.GetTotalBodyAsync(token).ConfigureAwait(false) * 2;
                intAttribute = Math.Max(await Parent.GetTotalBodyAsync(token).ConfigureAwait(false), 0);
            }

            int intBonus = 0;

            await Cyberware.ForEachAsync(async objChild =>
            {
                switch (objChild.Name)
                {
                    // If the limb has Customized Strength, this is its new base value.
                    case "Customized Strength":
                        intAttribute = await objChild.GetRatingAsync(token).ConfigureAwait(false);
                        break;
                    // If the limb has Enhanced Strength, this adds to the limb's value.
                    case "Enhanced Strength":
                        intBonus = await objChild.GetRatingAsync(token).ConfigureAwait(false);
                        break;
                }
            }, token: token).ConfigureAwait(false);

            return Math.Min(intAttribute + intBonus, Math.Max(intBody, 1));
        }

        /// <summary>
        /// Vehicle arm/leg Agility.
        /// </summary>
        public async Task<int> GetTotalAgilityAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!Name.ContainsAny(s_astrLimbStrings, StringComparison.OrdinalIgnoreCase))
                return 0;

            int intAttribute = 0;
            int intBody = 1;
            if (Parent != null)
            {
                intBody = await Parent.GetTotalBodyAsync(token).ConfigureAwait(false) * 2;
                intAttribute = Math.Max(await Parent.GetPilotAsync(token).ConfigureAwait(false), 0);
            }

            int intBonus = 0;

            await Cyberware.ForEachAsync(async objChild =>
            {
                switch (objChild.Name)
                {
                    // If the limb has Customized Strength, this is its new base value.
                    case "Customized Agility":
                        intAttribute = await objChild.GetRatingAsync(token).ConfigureAwait(false);
                        break;
                    // If the limb has Enhanced Strength, this adds to the limb's value.
                    case "Enhanced Agility":
                        intBonus = await objChild.GetRatingAsync(token).ConfigureAwait(false);
                        break;
                }
            }, token: token).ConfigureAwait(false);

            return Math.Min(intAttribute + intBonus, Math.Max(intBody, 1));
        }

        /// <summary>
        /// Whether the Mod is allowed to accept Cyberware Modular Plugins.
        /// </summary>
        public bool AllowModularPlugins
        {
            get
            {
                return Cyberware.Any(objChild => objChild.AllowedSubsystems.Contains("Modular Plug-In"));
            }
        }

        public async Task<XmlNode> GetNodeCoreAsync(bool blnSync, string strLanguage, CancellationToken token = default)
        {
            XmlNode objReturn = _objCachedMyXmlNode;
            if (objReturn != null && strLanguage == _strCachedXmlNodeLanguage
                                  && !GlobalSettings.LiveCustomData)
                return objReturn;
            XmlDocument objDoc = blnSync
                // ReSharper disable once MethodHasAsyncOverload
                ? _objCharacter.LoadData("vehicles.xml", strLanguage, token: token)
                : await _objCharacter.LoadDataAsync("vehicles.xml", strLanguage, token: token).ConfigureAwait(false);
            if (SourceID != Guid.Empty)
                objReturn = objDoc.TryGetNodeById("/chummer/mods/mod", SourceID)
                            ?? objDoc.TryGetNodeById("/chummer/weaponmountmods/mod", SourceID);
            if (objReturn == null)
            {
                objReturn = objDoc.TryGetNodeByNameOrId("/chummer/mods/mod", Name)
                            ?? objDoc.TryGetNodeByNameOrId("/chummer/weaponmountmods/mod", Name);
                objReturn?.TryGetFieldUninitialized("id", ref _guiSourceID);
            }
            _objCachedMyXmlNode = objReturn;
            _strCachedXmlNodeLanguage = strLanguage;
            return objReturn;
        }

        private XPathNavigator _objCachedMyXPathNode;
        private string _strCachedXPathNodeLanguage = string.Empty;

        public async Task<XPathNavigator> GetNodeXPathCoreAsync(bool blnSync, string strLanguage, CancellationToken token = default)
        {
            XPathNavigator objReturn = _objCachedMyXPathNode;
            if (objReturn != null && strLanguage == _strCachedXPathNodeLanguage
                                  && !GlobalSettings.LiveCustomData)
                return objReturn;
            XPathNavigator objDoc = blnSync
                // ReSharper disable once MethodHasAsyncOverload
                ? _objCharacter.LoadDataXPath("vehicles.xml", strLanguage, token: token)
                : await _objCharacter.LoadDataXPathAsync("vehicles.xml", strLanguage, token: token).ConfigureAwait(false);
            if (SourceID != Guid.Empty)
                objReturn = objDoc.TryGetNodeById("/chummer/mods/mod", SourceID)
                            ?? objDoc.TryGetNodeById("/chummer/weaponmountmods/mod", SourceID);
            if (objReturn == null)
            {
                objReturn = objDoc.TryGetNodeByNameOrId("/chummer/mods/mod", Name)
                            ?? objDoc.TryGetNodeByNameOrId("/chummer/weaponmountmods/mod", Name);
                objReturn?.TryGetFieldUninitialized("id", ref _guiSourceID);
            }
            _objCachedMyXPathNode = objReturn;
            _strCachedXPathNodeLanguage = strLanguage;
            return objReturn;
        }

        #endregion Complex Properties

        #region Methods

        public decimal DeleteVehicleMod(bool blnDoRemoval = true)
        {
            if (blnDoRemoval)
            {
                if (WeaponMountParent != null)
                    WeaponMountParent.Mods.Remove(this);
                else
                    Parent.Mods.Remove(this);
            }

            decimal decReturn = Weapons.AsEnumerableWithSideEffects().Sum(x => x.DeleteWeapon(false))
                                + Cyberware.AsEnumerableWithSideEffects().Sum(x => x.DeleteCyberware(false));

            DisposeSelf();

            return decReturn;
        }

        public async Task<decimal> DeleteVehicleModAsync(bool blnDoRemoval = true,
                                                              CancellationToken token = default)
        {
            if (blnDoRemoval)
            {
                if (WeaponMountParent != null)
                    await WeaponMountParent.Mods.RemoveAsync(this, token).ConfigureAwait(false);
                else
                    await Parent.Mods.RemoveAsync(this, token).ConfigureAwait(false);
            }

            decimal decReturn = await Weapons.SumWithSideEffectsAsync(x => x.DeleteWeaponAsync(false, token), token)
                                             .ConfigureAwait(false)
                                + await Cyberware.SumWithSideEffectsAsync(x => x.DeleteCyberwareAsync(false, token: token),
                                                           token).ConfigureAwait(false);

            await DisposeSelfAsync().ConfigureAwait(false);

            return decReturn;
        }

        /// <summary>
        /// Checks a nominated piece of gear for Availability requirements.
        /// </summary>
        /// <param name="dicRestrictedGearLimits">Dictionary of Restricted Gear availabilities still available with the amount of items that can still use that availability.</param>
        /// <param name="sbdAvailItems">StringBuilder used to list names of gear that are currently over the availability limit.</param>
        /// <param name="sbdRestrictedItems">StringBuilder used to list names of gear that are being used for Restricted Gear.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task<int> CheckRestrictedGear(IDictionary<int, int> dicRestrictedGearLimits, StringBuilder sbdAvailItems, StringBuilder sbdRestrictedItems, CancellationToken token = default)
        {
            int intRestrictedCount = 0;
            if (!IncludedInVehicle)
            {
                AvailabilityValue objTotalAvail = await TotalAvailTupleAsync(token: token).ConfigureAwait(false);
                if (!objTotalAvail.AddToParent)
                {
                    int intAvailInt = objTotalAvail.Value;
                    if (intAvailInt > _objCharacter.Settings.MaximumAvailability)
                    {
                        int intLowestValidRestrictedGearAvail = -1;
                        foreach (int intValidAvail in dicRestrictedGearLimits.Keys)
                        {
                            if (intValidAvail >= intAvailInt && (intLowestValidRestrictedGearAvail < 0
                                                                 || intValidAvail < intLowestValidRestrictedGearAvail))
                                intLowestValidRestrictedGearAvail = intValidAvail;
                        }

                        string strNameToUse = await GetCurrentDisplayNameAsync(token).ConfigureAwait(false);
                        if (Parent != null)
                            strNameToUse += await LanguageManager.GetStringAsync("String_Space", token: token).ConfigureAwait(false) + '(' + await Parent.GetCurrentDisplayNameAsync(token).ConfigureAwait(false) + ')';

                        if (intLowestValidRestrictedGearAvail >= 0
                            && dicRestrictedGearLimits[intLowestValidRestrictedGearAvail] > 0)
                        {
                            --dicRestrictedGearLimits[intLowestValidRestrictedGearAvail];
                            sbdRestrictedItems.AppendLine().Append("\t\t").Append(strNameToUse);
                        }
                        else
                        {
                            dicRestrictedGearLimits.Remove(intLowestValidRestrictedGearAvail);
                            ++intRestrictedCount;
                            sbdAvailItems.AppendLine().Append("\t\t").Append(strNameToUse);
                        }
                    }
                }
            }

            intRestrictedCount += await Weapons
                                        .SumAsync(objChild =>
                                                objChild
                                                    .CheckRestrictedGear(
                                                        dicRestrictedGearLimits, sbdAvailItems,
                                                        sbdRestrictedItems,
                                                        token), token: token)
                                        .ConfigureAwait(false)
                                  + await Cyberware
                                          .SumAsync(objChild =>
                                                  objChild
                                                      .CheckRestrictedGear(
                                                          dicRestrictedGearLimits,
                                                          sbdAvailItems,
                                                          sbdRestrictedItems,
                                                          token),
                                              token: token)
                                          .ConfigureAwait(false);

            return intRestrictedCount;
        }

        #region UI Methods

        /// <summary>
        /// Add a piece of Armor to the Armor TreeView.
        /// </summary>
        public async Task<TreeNode> CreateTreeNode(ContextMenuStrip cmsVehicleMod, ContextMenuStrip cmsCyberware, ContextMenuStrip cmsCyberwareGear, ContextMenuStrip cmsVehicleWeapon, ContextMenuStrip cmsVehicleWeaponAccessory, ContextMenuStrip cmsVehicleWeaponAccessoryGear, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (IncludedInVehicle && !string.IsNullOrEmpty(Source) && !await _objCharacter.Settings.BookEnabledAsync(Source, token).ConfigureAwait(false))
                return null;

            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = await GetCurrentDisplayNameAsync(token).ConfigureAwait(false),
                Tag = this,
                ContextMenuStrip = cmsVehicleMod,
                ForeColor = PreferredColor,
                ToolTipText = Notes.WordWrap()
            };

            TreeNodeCollection lstChildNodes = objNode.Nodes;
            // Cyberware.
            await Cyberware.ForEachAsync(async objCyberware =>
            {
                TreeNode objLoopNode = await objCyberware.CreateTreeNode(cmsCyberware, cmsCyberwareGear, token).ConfigureAwait(false);
                if (objLoopNode != null)
                    lstChildNodes.Add(objLoopNode);
            }, token).ConfigureAwait(false);

            // VehicleWeapons.
            await Weapons.ForEachAsync(async objWeapon =>
            {
                TreeNode objLoopNode = await objWeapon.CreateTreeNode(cmsVehicleWeapon, cmsVehicleWeaponAccessory,
                    cmsVehicleWeaponAccessoryGear, token).ConfigureAwait(false);
                if (objLoopNode != null)
                    lstChildNodes.Add(objLoopNode);
            }, token).ConfigureAwait(false);

            if (lstChildNodes.Count > 0)
                objNode.Expand();

            return objNode;
        }

        public Color PreferredColor
        {
            get
            {
                if (!string.IsNullOrEmpty(Notes))
                {
                    return IncludedInVehicle
                        ? ColorManager.GenerateCurrentModeDimmedColor(NotesColor)
                        : ColorManager.GenerateCurrentModeColor(NotesColor);
                }
                return IncludedInVehicle
                    ? ColorManager.GrayText
                    : ColorManager.WindowText;
            }
        }

        public decimal StolenTotalCost => CalculatedStolenTotalCost(true);

        public decimal NonStolenTotalCost => CalculatedStolenTotalCost(false);

        public decimal CalculatedStolenTotalCost(bool blnStolen)
        {
            decimal d = 0;
            if (Stolen == blnStolen)
                d += OwnCost;
            d += Weapons.Sum(objWeapon => objWeapon.CalculatedStolenTotalCost(blnStolen));
            d += Cyberware.Sum(objCyberware => objCyberware.CalculatedStolenTotalCost(blnStolen));
            return d;
        }

        public Task<decimal> GetStolenTotalCostAsync(CancellationToken token = default) => CalculatedStolenTotalCostAsync(true, token);

        public Task<decimal> GetNonStolenTotalCostAsync(CancellationToken token = default) => CalculatedStolenTotalCostAsync(false, token);

        public async Task<decimal> CalculatedStolenTotalCostAsync(bool blnStolen, CancellationToken token = default)
        {
            decimal d = 0;
            if (Stolen == blnStolen)
                d += await GetOwnCostAsync(token).ConfigureAwait(false);
            d += await Weapons.SumAsync(objWeapon => objWeapon.CalculatedStolenTotalCostAsync(blnStolen, token), token).ConfigureAwait(false);
            d += await Cyberware.SumAsync(objCyberware => objCyberware.CalculatedStolenTotalCostAsync(blnStolen, token), token).ConfigureAwait(false);
            return d;
        }

        #endregion UI Methods

        #endregion Methods

        public void SetSourceDetail(Control sourceControl)
        {
            if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                _objCachedSourceDetail = default;
            SourceDetail.SetControl(sourceControl);
        }

        public async Task SetSourceDetailAsync(Control sourceControl, CancellationToken token = default)
        {
            if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                _objCachedSourceDetail = default;
            await (await GetSourceDetailAsync(token).ConfigureAwait(false)).SetControlAsync(sourceControl, token).ConfigureAwait(false);
        }

        public bool AllowPasteXml
        {
            get
            {
                switch (GlobalSettings.ClipboardContentType)
                {
                    case ClipboardContentType.Weapon:
                        {
                            // TODO: Make this not depend on string names
                            return Name.StartsWith("Mechanical Arm", StringComparison.Ordinal) || Name.Contains("Drone Arm");
                        }
                    default:
                        return false;
                }
            }
        }

        public bool AllowPasteObject(object input)
        {
            throw new NotImplementedException();
        }

        public bool Remove(bool blnConfirmDelete = true)
        {
            if (blnConfirmDelete && !CommonFunctions.ConfirmDelete(LanguageManager.GetString("Message_DeleteVehicleMod")))
                return false;

            DeleteVehicleMod();
            return true;
        }

        public async Task<bool> RemoveAsync(bool blnConfirmDelete = true, CancellationToken token = default)
        {
            if (blnConfirmDelete && !await CommonFunctions
                    .ConfirmDeleteAsync(
                        await LanguageManager
                            .GetStringAsync("Message_DeleteVehicleMod", token: token)
                            .ConfigureAwait(false), token).ConfigureAwait(false))
                return false;

            await DeleteVehicleModAsync(token: token).ConfigureAwait(false);
            return true;
        }

        public bool Sell(decimal decPercentage, bool blnConfirmDelete)
        {
            if (!_objCharacter.Created)
                return Remove(blnConfirmDelete);

            if (blnConfirmDelete && !CommonFunctions.ConfirmDelete(LanguageManager.GetString("Message_DeleteVehicleMod")))
                return false;

            IHasCost objParent = (IHasCost)WeaponMountParent ?? Parent;
            decimal decAmount;
            if (objParent != null)
            {
                decimal decOriginal = objParent.TotalCost;
                decAmount = DeleteVehicleMod() * decPercentage;
                decAmount += (decOriginal - objParent.TotalCost) * decPercentage;
            }
            else
            {
                decimal decOriginal = TotalCost;
                decAmount = (DeleteVehicleMod() + decOriginal) * decPercentage;
            }
            // Create the Expense Log Entry for the sale.
            ExpenseLogEntry objExpense = new ExpenseLogEntry(_objCharacter);
            objExpense.Create(decAmount, LanguageManager.GetString("String_ExpenseSoldVehicleMod") + ' ' + CurrentDisplayNameShort, ExpenseType.Nuyen, DateTime.Now);
            _objCharacter.ExpenseEntries.AddWithSort(objExpense);
            _objCharacter.Nuyen += decAmount;
            return true;
        }

        public async Task<bool> SellAsync(decimal decPercentage, bool blnConfirmDelete,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!await _objCharacter.GetCreatedAsync(token).ConfigureAwait(false))
                return await RemoveAsync(blnConfirmDelete, token).ConfigureAwait(false);

            if (blnConfirmDelete && !await CommonFunctions
                    .ConfirmDeleteAsync(
                        await LanguageManager.GetStringAsync("Message_DeleteVehicleMod", token: token).ConfigureAwait(false),
                        token).ConfigureAwait(false))
                return false;

            IHasCost objParent = (IHasCost)WeaponMountParent ?? Parent;
            decimal decAmount;
            if (objParent != null)
            {
                decimal decOriginal = await objParent.GetTotalCostAsync(token).ConfigureAwait(false);
                decAmount = await DeleteVehicleModAsync(token: token).ConfigureAwait(false) * decPercentage;
                decAmount += (decOriginal - await objParent.GetTotalCostAsync(token).ConfigureAwait(false)) * decPercentage;
            }
            else
            {
                decimal decOriginal = await GetTotalCostAsync(token).ConfigureAwait(false);
                decAmount = (await DeleteVehicleModAsync(token: token).ConfigureAwait(false) + decOriginal) * decPercentage;
            }

            // Create the Expense Log Entry for the sale.
            ExpenseLogEntry objExpense = new ExpenseLogEntry(_objCharacter);
            objExpense.Create(decAmount,
                await LanguageManager.GetStringAsync("String_ExpenseSoldVehicleMod", token: token).ConfigureAwait(false) +
                ' ' + await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false), ExpenseType.Nuyen,
                DateTime.Now);
            await _objCharacter.ExpenseEntries.AddWithSortAsync(objExpense, token: token).ConfigureAwait(false);
            await _objCharacter.ModifyNuyenAsync(decAmount, token).ConfigureAwait(false);
            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (Weapon objChild in _lstVehicleWeapons)
                objChild.Dispose();
            foreach (Cyberware objChild in _lstCyberware)
                objChild.Dispose();
            DisposeSelf();
        }

        private void DisposeSelf()
        {
            _lstVehicleWeapons.Dispose();
            _lstCyberware.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            foreach (Weapon objChild in _lstVehicleWeapons)
                await objChild.DisposeAsync().ConfigureAwait(false);
            foreach (Cyberware objChild in _lstCyberware)
                await objChild.DisposeAsync().ConfigureAwait(false);
            await DisposeSelfAsync().ConfigureAwait(false);
        }

        private async ValueTask DisposeSelfAsync()
        {
            await _lstVehicleWeapons.DisposeAsync().ConfigureAwait(false);
            await _lstCyberware.DisposeAsync().ConfigureAwait(false);
        }

        public Character CharacterObject => _objCharacter;
    }
}
