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
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Chummer.Backend.Attributes;
using Chummer.Backend.Skills;
using NLog;

namespace Chummer
{
    /// <summary>
    /// A Technomancer Program or Complex Form.
    /// </summary>
    [HubClassTag("SourceID", true, "Name", "Extra")]
    [DebuggerDisplay("{DisplayName(GlobalSettings.DefaultLanguage)}")]
    public class ComplexForm : IHasInternalId, IHasName, IHasSourceId, IHasXmlDataNode, IHasNotes, ICanRemove, IHasSource, IHasCharacterObject
    {
        private static readonly Lazy<Logger> s_ObjLogger = new Lazy<Logger>(LogManager.GetCurrentClassLogger);
        private static Logger Log => s_ObjLogger.Value;
        private Guid _guiID;
        private Guid _guiSourceID = Guid.Empty;
        private string _strName = string.Empty;
        private string _strTarget = string.Empty;
        private string _strDuration = string.Empty;
        private string _strFv = string.Empty;
        private string _strSource = string.Empty;
        private string _strPage = string.Empty;
        private string _strNotes = string.Empty;
        private Color _colNotes = ColorManager.HasNotesColor;
        private string _strExtra = string.Empty;
        private int _intGrade;
        private readonly Character _objCharacter;
        private SourceString _objCachedSourceDetail;

        public Character CharacterObject => _objCharacter; // readonly member, no locking needed

        #region Constructor, Create, Save, Load, and Print Methods

        public ComplexForm(Character objCharacter)
        {
            // Create the GUID for the new Complex Form.
            _guiID = Guid.NewGuid();
            _objCharacter = objCharacter;
        }

        /// <summary>
        /// Create a Complex Form from an XmlNode.
        /// </summary>
        /// <param name="objXmlComplexFormNode">XmlNode to create the object from.</param>
        /// <param name="strExtra">Value to forcefully select for any ImprovementManager prompts.</param>
        public void Create(XmlNode objXmlComplexFormNode, string strExtra = "")
        {
            if (!objXmlComplexFormNode.TryGetField("id", Guid.TryParse, out _guiSourceID))
            {
                Log.Warn(new object[] { "Missing id field for complex form xmlnode", objXmlComplexFormNode });
                Utils.BreakIfDebug();
            }
            objXmlComplexFormNode.TryGetField("id", Guid.TryParse, out _guiSourceID);
            objXmlComplexFormNode.TryGetStringFieldQuickly("name", ref _strName);
            objXmlComplexFormNode.TryGetStringFieldQuickly("target", ref _strTarget);
            objXmlComplexFormNode.TryGetStringFieldQuickly("source", ref _strSource);
            objXmlComplexFormNode.TryGetStringFieldQuickly("page", ref _strPage);
            objXmlComplexFormNode.TryGetStringFieldQuickly("duration", ref _strDuration);
            objXmlComplexFormNode.TryGetStringFieldQuickly("fv", ref _strFv);
            if (!objXmlComplexFormNode.TryGetMultiLineStringFieldQuickly("altnotes", ref _strNotes))
                objXmlComplexFormNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);

            string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
            objXmlComplexFormNode.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
            _colNotes = ColorTranslator.FromHtml(sNotesColor);

            if (GlobalSettings.InsertPdfNotesIfAvailable && string.IsNullOrEmpty(Notes))
            {
                Notes = CommonFunctions.GetBookNotes(objXmlComplexFormNode, Name, CurrentDisplayName, Source, Page,
                    DisplayPage(GlobalSettings.Language), _objCharacter);
            }

            if (objXmlComplexFormNode["bonus"] != null)
            {
                ImprovementManager.ForcedValue = strExtra;
                if (!ImprovementManager.CreateImprovements(_objCharacter, Improvement.ImprovementSource.ComplexForm, _guiID.ToString("D", GlobalSettings.InvariantCultureInfo), objXmlComplexFormNode["bonus"], 1, CurrentDisplayNameShort))
                {
                    _guiID = Guid.Empty;
                    return;
                }
                if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue))
                {
                    _strExtra = ImprovementManager.SelectedValue;
                }
            }
            else
            {
                _strExtra = strExtra;
            }
        }

        /// <summary>
        /// Create a Complex Form from an XmlNode.
        /// </summary>
        /// <param name="objXmlComplexFormNode">XmlNode to create the object from.</param>
        /// <param name="strExtra">Value to forcefully select for any ImprovementManager prompts.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task CreateAsync(XmlNode objXmlComplexFormNode, string strExtra = "", CancellationToken token = default)
        {
            if (!objXmlComplexFormNode.TryGetField("id", Guid.TryParse, out _guiSourceID))
            {
                Log.Warn(new object[] { "Missing id field for complex form xmlnode", objXmlComplexFormNode });
                Utils.BreakIfDebug();
            }
            objXmlComplexFormNode.TryGetField("id", Guid.TryParse, out _guiSourceID);
            objXmlComplexFormNode.TryGetStringFieldQuickly("name", ref _strName);
            objXmlComplexFormNode.TryGetStringFieldQuickly("target", ref _strTarget);
            objXmlComplexFormNode.TryGetStringFieldQuickly("source", ref _strSource);
            objXmlComplexFormNode.TryGetStringFieldQuickly("page", ref _strPage);
            objXmlComplexFormNode.TryGetStringFieldQuickly("duration", ref _strDuration);
            objXmlComplexFormNode.TryGetStringFieldQuickly("fv", ref _strFv);
            if (!objXmlComplexFormNode.TryGetMultiLineStringFieldQuickly("altnotes", ref _strNotes))
                objXmlComplexFormNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);

            string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
            objXmlComplexFormNode.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
            _colNotes = ColorTranslator.FromHtml(sNotesColor);

            if (GlobalSettings.InsertPdfNotesIfAvailable && string.IsNullOrEmpty(Notes))
            {
                Notes = await CommonFunctions.GetBookNotesAsync(objXmlComplexFormNode, Name, await GetCurrentDisplayNameAsync(token).ConfigureAwait(false), Source, Page,
                    await DisplayPageAsync(GlobalSettings.Language, token).ConfigureAwait(false), _objCharacter, token).ConfigureAwait(false);
            }

            if (objXmlComplexFormNode["bonus"] != null)
            {
                ImprovementManager.ForcedValue = strExtra;
                if (!await ImprovementManager.CreateImprovementsAsync(_objCharacter, Improvement.ImprovementSource.ComplexForm, _guiID.ToString("D", GlobalSettings.InvariantCultureInfo), objXmlComplexFormNode["bonus"], 1, await GetCurrentDisplayNameShortAsync(token).ConfigureAwait(false), token: token).ConfigureAwait(false))
                {
                    _guiID = Guid.Empty;
                    return;
                }
                if (!string.IsNullOrEmpty(ImprovementManager.SelectedValue))
                {
                    _strExtra = ImprovementManager.SelectedValue;
                }
            }
            else
            {
                _strExtra = strExtra;
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
            objWriter.WriteStartElement("complexform");
            objWriter.WriteElementString("sourceid", SourceIDString);
            objWriter.WriteElementString("guid", InternalId);
            objWriter.WriteElementString("name", _strName);
            objWriter.WriteElementString("target", _strTarget);
            objWriter.WriteElementString("duration", _strDuration);
            objWriter.WriteElementString("fv", _strFv);
            objWriter.WriteElementString("extra", _strExtra);
            objWriter.WriteElementString("source", _strSource);
            objWriter.WriteElementString("page", _strPage);
            objWriter.WriteElementString("notes", _strNotes.CleanOfInvalidUnicodeChars());
            objWriter.WriteElementString("notesColor", ColorTranslator.ToHtml(_colNotes));
            objWriter.WriteElementString("grade", _intGrade.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteEndElement();
        }

        /// <summary>
        /// Load the Complex Form from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        public void Load(XmlNode objNode)
        {
            if (!objNode.TryGetField("guid", Guid.TryParse, out _guiID))
            {
                _guiID = Guid.NewGuid();
            }
            objNode.TryGetStringFieldQuickly("name", ref _strName);
            _objCachedMyXmlNode = null;
            _objCachedMyXPathNode = null;
            if (!objNode.TryGetGuidFieldQuickly("sourceid", ref _guiSourceID))
            {
                this.GetNodeXPath()?.TryGetGuidFieldQuickly("id", ref _guiSourceID);
            }

            objNode.TryGetStringFieldQuickly("target", ref _strTarget);
            objNode.TryGetStringFieldQuickly("source", ref _strSource);
            objNode.TryGetStringFieldQuickly("page", ref _strPage);
            objNode.TryGetStringFieldQuickly("duration", ref _strDuration);
            objNode.TryGetStringFieldQuickly("extra", ref _strExtra);
            objNode.TryGetStringFieldQuickly("fv", ref _strFv);
            objNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);

            string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
            objNode.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
            _colNotes = ColorTranslator.FromHtml(sNotesColor);

            objNode.TryGetInt32FieldQuickly("grade", ref _intGrade);
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        /// <param name="strLanguageToPrint">Language in which to print</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task Print(XmlWriter objWriter, string strLanguageToPrint, CancellationToken token = default)
        {
            if (objWriter == null)
                return;
            // <complexform>
            XmlElementWriteHelper objBaseElement = await objWriter.StartElementAsync("complexform", token).ConfigureAwait(false);
            try
            {
                await objWriter.WriteElementStringAsync("guid", InternalId, token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("sourceid", SourceIDString, token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("name", await DisplayNameShortAsync(strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("fullname", await DisplayNameAsync(strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("name_english", Name, token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("duration", await DisplayDurationAsync(strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("fv", await DisplayFvAsync(strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("target", await DisplayTargetAsync(strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("source", await _objCharacter.LanguageBookShortAsync(Source, strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("page", await DisplayPageAsync(strLanguageToPrint, token).ConfigureAwait(false), token).ConfigureAwait(false);
                if (GlobalSettings.PrintNotes)
                    await objWriter.WriteElementStringAsync("notes", Notes, token).ConfigureAwait(false);
            }
            finally
            {
                // </complexform>
                await objBaseElement.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Constructor, Create, Save, Load, and Print Methods

        #region Properties

        /// <summary>
        /// Identifier of the object within data files.
        /// </summary>
        public Guid SourceID
        {
            get => _guiSourceID;
            set
            {
                if (_guiSourceID == value)
                    return;
                _guiSourceID = value;
                _objCachedMyXmlNode = null;
                _objCachedMyXPathNode = null;
            }
        }

        /// <summary>
        /// String-formatted identifier of the <inheritdoc cref="SourceID"/> from the data files.
        /// </summary>
        public string SourceIDString => _guiSourceID.ToString("D", GlobalSettings.InvariantCultureInfo);

        /// <summary>
        /// Internal identifier which will be used to identify this Complex Form in the Improvement system.
        /// </summary>
        public string InternalId => _guiID.ToString("D", GlobalSettings.InvariantCultureInfo);

        /// <summary>
        /// Complex Form's name.
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
        /// Complex Form's extra info.
        /// </summary>
        public string Extra
        {
            get => _strExtra;
            set => _strExtra = _objCharacter.ReverseTranslateExtra(value);
        }

        /// <summary>
        /// Complex Form's grade.
        /// </summary>
        public int Grade
        {
            get => _intGrade;
            set => _intGrade = value;
        }

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public string DisplayNameShort(string strLanguage)
        {
            string strReturn = Name;
            // Get the translated name if applicable.
            if (!strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                strReturn = this.GetNodeXPath(strLanguage)?.SelectSingleNodeAndCacheExpression("translate")?.Value ?? Name;

            return strReturn;
        }

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public async Task<string> DisplayNameShortAsync(string strLanguage, CancellationToken token = default)
        {
            string strReturn = Name;
            // Get the translated name if applicable.
            if (!strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
            {
                XPathNavigator objNode = await this.GetNodeXPathAsync(strLanguage, token: token).ConfigureAwait(false);
                strReturn = objNode != null ? objNode.SelectSingleNodeAndCacheExpression("translate", token: token)?.Value ?? Name : Name;
            }

            return strReturn;
        }

        public string DisplayName(string strLanguage)
        {
            string strReturn = DisplayNameShort(strLanguage);

            if (!string.IsNullOrEmpty(Extra))
            {
                string strExtra = Extra;
                if (!strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                    strExtra = _objCharacter.TranslateExtra(Extra, strLanguage);
                strReturn += LanguageManager.GetString("String_Space", strLanguage) + '(' + strExtra + ')';
            }
            return strReturn;
        }

        public async Task<string> DisplayNameAsync(string strLanguage, CancellationToken token = default)
        {
            string strReturn = await DisplayNameShortAsync(strLanguage, token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(Extra))
            {
                string strExtra = Extra;
                if (!strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                    strExtra = await _objCharacter.TranslateExtraAsync(Extra, strLanguage, token: token).ConfigureAwait(false);
                strReturn += await LanguageManager.GetStringAsync("String_Space", strLanguage, token: token).ConfigureAwait(false) + '(' + strExtra + ')';
            }
            return strReturn;
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Name (Extra).
        /// </summary>
        public string CurrentDisplayName => DisplayName(GlobalSettings.Language);

        public Task<string> GetCurrentDisplayNameAsync(CancellationToken token = default) => DisplayNameAsync(GlobalSettings.Language, token);

        public string CurrentDisplayNameShort => DisplayNameShort(GlobalSettings.Language);

        public Task<string> GetCurrentDisplayNameShortAsync(CancellationToken token = default) => DisplayNameShortAsync(GlobalSettings.Language, token);

        /// <summary>
        /// Translated Duration.
        /// </summary>
        public string DisplayDuration(string strLanguage)
        {
            switch (Duration)
            {
                case "P":
                    return LanguageManager.GetString("String_SpellDurationPermanent", strLanguage);

                case "S":
                    return LanguageManager.GetString("String_SpellDurationSustained", strLanguage);

                case "I":
                    return LanguageManager.GetString("String_SpellDurationInstant", strLanguage);

                case "Special":
                    return LanguageManager.GetString("String_SpellDurationSpecial", strLanguage);

                default:
                    return LanguageManager.GetString("String_None", strLanguage);
            }
        }

        /// <summary>
        /// Translated Duration.
        /// </summary>
        public Task<string> DisplayDurationAsync(string strLanguage, CancellationToken token = default)
        {
            switch (Duration)
            {
                case "P":
                    return LanguageManager.GetStringAsync("String_SpellDurationPermanent", strLanguage, token: token);

                case "S":
                    return LanguageManager.GetStringAsync("String_SpellDurationSustained", strLanguage, token: token);

                case "I":
                    return LanguageManager.GetStringAsync("String_SpellDurationInstant", strLanguage, token: token);

                case "Special":
                    return LanguageManager.GetStringAsync("String_SpellDurationSpecial", strLanguage, token: token);

                default:
                    return LanguageManager.GetStringAsync("String_None", strLanguage, token: token);
            }
        }

        /// <summary>
        /// Complex Form's Duration.
        /// </summary>
        public string Duration
        {
            get => _strDuration;
            set => _strDuration = value;
        }

        /// <summary>
        /// Translated Fading Value.
        /// </summary>
        public string DisplayFv(string strLanguage)
        {
            string strReturn = CalculatedFv.Replace('/', '÷').Replace('*', '×');
            if (!strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
            {
                strReturn = strReturn.CheapReplace("L", () => LanguageManager.GetString("String_ComplexFormLevel", strLanguage))
                    .CheapReplace("Overflow damage", () => LanguageManager.GetString("String_SpellOverflowDamage", strLanguage))
                    .CheapReplace("Damage Value", () => LanguageManager.GetString("String_SpellDamageValue", strLanguage))
                    .CheapReplace("Toxin DV", () => LanguageManager.GetString("String_SpellToxinDV", strLanguage))
                    .CheapReplace("Disease DV", () => LanguageManager.GetString("String_SpellDiseaseDV", strLanguage))
                    .CheapReplace("Radiation Power", () => LanguageManager.GetString("String_SpellRadiationPower", strLanguage))
                    .CheapReplace("Special", () => LanguageManager.GetString("String_Special", strLanguage));
            }
            return strReturn;
        }

        /// <summary>
        /// Translated Fading Value.
        /// </summary>
        public async Task<string> DisplayFvAsync(string strLanguage, CancellationToken token = default)
        {
            string strReturn = CalculatedFv.Replace('/', '÷').Replace('*', '×');
            if (!strLanguage.Equals(GlobalSettings.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
            {
                strReturn = await strReturn
                                  .CheapReplaceAsync(
                                      "L", () => LanguageManager.GetStringAsync("String_ComplexFormLevel", strLanguage, token: token), token: token)
                                  .CheapReplaceAsync("Overflow damage",
                                                     () => LanguageManager.GetStringAsync(
                                                         "String_SpellOverflowDamage", strLanguage, token: token), token: token)
                                  .CheapReplaceAsync("Damage Value",
                                                     () => LanguageManager.GetStringAsync(
                                                         "String_SpellDamageValue", strLanguage, token: token), token: token)
                                  .CheapReplaceAsync(
                                      "Toxin DV", () => LanguageManager.GetStringAsync("String_SpellToxinDV", strLanguage, token: token), token: token)
                                  .CheapReplaceAsync("Disease DV",
                                                     () => LanguageManager.GetStringAsync(
                                                         "String_SpellDiseaseDV", strLanguage, token: token), token: token)
                                  .CheapReplaceAsync("Radiation Power",
                                                     () => LanguageManager.GetStringAsync(
                                                         "String_SpellRadiationPower", strLanguage, token: token), token: token)
                                  .CheapReplaceAsync(
                                      "Special", () => LanguageManager.GetStringAsync("String_Special", strLanguage, token: token), token: token).ConfigureAwait(false);
            }
            return strReturn;
        }

        /// <summary>
        /// Fading Tooltip.
        /// </summary>
        public string FvTooltip
        {
            get
            {
                int intRES = _objCharacter.RES.TotalValue;
                string strFv = FvBase;
                string strSpace = LanguageManager.GetString("String_Space");
                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                                                              out StringBuilder sbdTip))
                {
                    sbdTip.Append(LanguageManager.GetString("Tip_ComplexFormFading"));
                    for (int i = 1; i <= intRES * 2; i++)
                    {
                        // Calculate the Complex Form's Fading for the current Level.
                        (bool blnIsSuccess, object xprResult) = CommonFunctions.EvaluateInvariantXPath(
                            strFv.Replace("L", i.ToString(GlobalSettings.InvariantCultureInfo)).Replace("/", " div "));

                        if (blnIsSuccess && strFv != "Special")
                        {
                            int intFv = ((double)xprResult).StandardRound();

                            // Fading cannot be lower than 2.
                            if (intFv < 2)
                                intFv = 2;
                            sbdTip.AppendLine().Append(LanguageManager.GetString("String_Level")).Append(strSpace)
                                  .Append(
                                      i.ToString(GlobalSettings.CultureInfo))
                                  .Append(LanguageManager.GetString("String_Colon"))
                                  .Append(strSpace).Append(intFv.ToString(GlobalSettings.CultureInfo));
                        }
                        else
                        {
                            sbdTip.Clear();
                            sbdTip.Append(LanguageManager.GetString("Tip_ComplexFormFadingSeeDescription"));
                            break;
                        }
                    }

                    sbdTip.AppendLine().Append(LanguageManager.GetString("Tip_ComplexFormFadingBase")).Append(strSpace).Append('(').Append(FvBase).Append(')');
                    foreach (Improvement objLoopImprovement in ImprovementManager.GetCachedImprovementListForValueOf(
                                 _objCharacter, Improvement.ImprovementType.FadingValue, Name, true))
                    {
                        sbdTip.Append(strSpace).Append('+').Append(strSpace).Append(_objCharacter.GetObjectName(objLoopImprovement)).Append(strSpace)
                              .Append('(').Append(objLoopImprovement.Value.ToString("0;-0;0")).Append(')');
                    }
                    return sbdTip.ToString();
                }
            }
        }

        /// <summary>
        /// The Complex Form's FV.
        /// </summary>
        public string CalculatedFv
        {
            get
            {
                string strReturn = FvBase;
                List<Improvement> lstImprovements
                    = ImprovementManager.GetCachedImprovementListForValueOf(
                        _objCharacter, Improvement.ImprovementType.FadingValue, Name, true);
                if (lstImprovements.Count > 0)
                {
                    bool force = strReturn.StartsWith('L');
                    string strFv = strReturn;
                    if (force)
                        strFv = strFv.TrimStartOnce("L", true);
                    //Navigator can't do math on a single value, so inject a mathable value.
                    if (string.IsNullOrEmpty(strFv))
                    {
                        strFv = "0";
                    }
                    else
                    {
                        int intPos = strReturn.IndexOf('-');
                        if (intPos != -1)
                        {
                            strFv = strReturn.Substring(intPos);
                        }
                        else
                        {
                            intPos = strReturn.IndexOf('+');
                            if (intPos != -1)
                            {
                                strFv = strReturn.Substring(intPos);
                            }
                        }
                    }

                    using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                                                                  out StringBuilder sbdFv))
                    {
                        sbdFv.Append(strFv);
                        foreach (Improvement objImprovement in lstImprovements)
                        {
                            sbdFv.AppendFormat(GlobalSettings.InvariantCultureInfo, "{0:+0;-0;+0}",
                                               objImprovement.Value);
                        }

                        (bool blnIsSuccess, object xprResult)
                            = CommonFunctions.EvaluateInvariantXPath(sbdFv.ToString());

                        if (blnIsSuccess)
                        {
                            if (force)
                            {
                                strReturn = string.Format(GlobalSettings.CultureInfo, "L{0:+0;-0;}", xprResult);
                            }
                            else if (xprResult.ToString() != "0")
                            {
                                strReturn += xprResult;
                            }
                        }
                    }
                }
                return strReturn;
            }
        }

        public string FvBase
        {
            get => _strFv;
            set => _strFv = value;
        }

        /// <summary>
        /// Translated Duration.
        /// </summary>
        public string DisplayTarget(string strLanguage)
        {
            switch (Target)
            {
                case "Persona":
                    return LanguageManager.GetString("String_ComplexFormTargetPersona", strLanguage);

                case "Device":
                    return LanguageManager.GetString("String_ComplexFormTargetDevice", strLanguage);

                case "File":
                    return LanguageManager.GetString("String_ComplexFormTargetFile", strLanguage);

                case "Self":
                    return LanguageManager.GetString("String_SpellRangeSelf", strLanguage);

                case "Sprite":
                    return LanguageManager.GetString("String_ComplexFormTargetSprite", strLanguage);

                case "Host":
                    return LanguageManager.GetString("String_ComplexFormTargetHost", strLanguage);

                case "IC":
                    return LanguageManager.GetString("String_ComplexFormTargetIC", strLanguage);

                case "Icon":
                    return LanguageManager.GetString("String_ComplexFormTargetIcon", strLanguage);

                case "Special":
                    return LanguageManager.GetString("String_Special", strLanguage);

                default:
                    return LanguageManager.GetString("String_None", strLanguage);
            }
        }

        /// <summary>
        /// Translated Duration.
        /// </summary>
        public Task<string> DisplayTargetAsync(string strLanguage, CancellationToken token = default)
        {
            switch (Target)
            {
                case "Persona":
                    return LanguageManager.GetStringAsync("String_ComplexFormTargetPersona", strLanguage, token: token);

                case "Device":
                    return LanguageManager.GetStringAsync("String_ComplexFormTargetDevice", strLanguage, token: token);

                case "File":
                    return LanguageManager.GetStringAsync("String_ComplexFormTargetFile", strLanguage, token: token);

                case "Self":
                    return LanguageManager.GetStringAsync("String_SpellRangeSelf", strLanguage, token: token);

                case "Sprite":
                    return LanguageManager.GetStringAsync("String_ComplexFormTargetSprite", strLanguage, token: token);

                case "Host":
                    return LanguageManager.GetStringAsync("String_ComplexFormTargetHost", strLanguage, token: token);

                case "IC":
                    return LanguageManager.GetStringAsync("String_ComplexFormTargetIC", strLanguage, token: token);

                case "Icon":
                    return LanguageManager.GetStringAsync("String_ComplexFormTargetIcon", strLanguage, token: token);

                case "Special":
                    return LanguageManager.GetStringAsync("String_Special", strLanguage, token: token);

                default:
                    return LanguageManager.GetStringAsync("String_None", strLanguage, token: token);
            }
        }

        /// <summary>
        /// The Complex Form's Target.
        /// </summary>
        public string Target
        {
            get => _strTarget;
            set => _strTarget = value;
        }

        /// <summary>
        /// Complex Form's Source.
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

        public Skill Skill => _objCharacter.SkillsSection.GetActiveSkill("Software");

        /// <summary>
        /// The Dice Pool size for the Active Skill required to thread the Complex Form.
        /// </summary>
        public int DicePool
        {
            get
            {
                int intReturn = 0;
                if (Skill != null)
                {
                    intReturn = Skill.PoolOtherAttribute("RES");
                    // Add any Specialization bonus if applicable.
                    intReturn += Skill.GetSpecializationBonus(CurrentDisplayName);
                }

                // Include any Improvements to Threading.
                intReturn += ImprovementManager.ValueOf(_objCharacter, Improvement.ImprovementType.ActionDicePool, false, "Threading").StandardRound();

                return intReturn;
            }
        }

        /// <summary>
        /// Tooltip information for the Dice Pool.
        /// </summary>
        public string DicePoolTooltip
        {
            get
            {
                string strSpace = LanguageManager.GetString("String_Space");
                using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                                                              out StringBuilder sbdReturn))
                {
                    string strFormat = strSpace + "{0}" + strSpace + "({1})";
                    CharacterAttrib objResonanceAttrib = _objCharacter.GetAttribute("RES");
                    if (objResonanceAttrib != null)
                    {
                        sbdReturn.AppendFormat(GlobalSettings.CultureInfo, strFormat,
                                               objResonanceAttrib.DisplayNameFormatted,
                                               objResonanceAttrib.DisplayValue);
                    }

                    if (Skill != null)
                    {
                        if (sbdReturn.Length > 0)
                            sbdReturn.Append(strSpace).Append('+').Append(strSpace);
                        sbdReturn.Append(Skill.FormattedDicePool(Skill.PoolOtherAttribute("RES") -
                                                                 (objResonanceAttrib?.TotalValue ?? 0),
                                                                 CurrentDisplayName));
                    }

                    // Include any Improvements to the Spell Category.
                    foreach (Improvement objImprovement in ImprovementManager.GetCachedImprovementListForValueOf(
                                 _objCharacter, Improvement.ImprovementType.ActionDicePool, "Threading"))
                    {
                        if (sbdReturn.Length > 0)
                            sbdReturn.Append(strSpace).Append('+').Append(strSpace);
                        sbdReturn.AppendFormat(GlobalSettings.CultureInfo, strFormat,
                                               _objCharacter.GetObjectName(objImprovement), objImprovement.Value);
                    }

                    return sbdReturn.ToString();
                }
            }
        }

        private XmlNode _objCachedMyXmlNode;
        private string _strCachedXmlNodeLanguage = string.Empty;

        public async Task<XmlNode> GetNodeCoreAsync(bool blnSync, string strLanguage, CancellationToken token = default)
        {
            XmlNode objReturn = _objCachedMyXmlNode;
            if (objReturn != null && strLanguage == _strCachedXmlNodeLanguage
                                  && !GlobalSettings.LiveCustomData)
                return objReturn;
            XmlNode objDoc = blnSync
                // ReSharper disable once MethodHasAsyncOverload
                ? _objCharacter.LoadData("complexforms.xml", strLanguage, token: token)
                : await _objCharacter.LoadDataAsync("complexforms.xml", strLanguage, token: token).ConfigureAwait(false);
            if (SourceID != Guid.Empty)
                objReturn = objDoc.TryGetNodeById("/chummer/complexforms/complexform", SourceID);
            if (objReturn == null)
            {
                objReturn = objDoc.TryGetNodeByNameOrId("/chummer/complexforms/complexform", Name);
                objReturn?.TryGetGuidFieldQuickly("id", ref _guiSourceID);
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
                ? _objCharacter.LoadDataXPath("complexforms.xml", strLanguage, token: token)
                : await _objCharacter.LoadDataXPathAsync("complexforms.xml", strLanguage, token: token).ConfigureAwait(false);
            if (SourceID != Guid.Empty)
                objReturn = objDoc.TryGetNodeById("/chummer/complexforms/complexform", SourceID);
            if (objReturn == null)
            {
                objReturn = objDoc.TryGetNodeByNameOrId("/chummer/complexforms/complexform", Name);
                objReturn?.TryGetGuidFieldQuickly("id", ref _guiSourceID);
            }
            _objCachedMyXPathNode = objReturn;
            _strCachedXPathNodeLanguage = strLanguage;
            return objReturn;
        }

        #endregion Properties

        #region UI Methods

        public TreeNode CreateTreeNode(ContextMenuStrip cmsComplexForm)
        {
            if (Grade != 0 && !string.IsNullOrEmpty(Source) && !_objCharacter.Settings.BookEnabled(Source))
                return null;

            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = CurrentDisplayName,
                Tag = this,
                ContextMenuStrip = cmsComplexForm,
                ForeColor = PreferredColor,
                ToolTipText = Notes.WordWrap()
            };
            return objNode;
        }

        public Color PreferredColor
        {
            get
            {
                if (!string.IsNullOrEmpty(Notes))
                {
                    return Grade != 0
                        ? ColorManager.GenerateCurrentModeDimmedColor(NotesColor)
                        : ColorManager.GenerateCurrentModeColor(NotesColor);
                }
                return Grade != 0
                    ? ColorManager.GrayText
                    : ColorManager.WindowText;
            }
        }

        #endregion UI Methods

        public bool Remove(bool blnConfirmDelete = true)
        {
            if (blnConfirmDelete)
            {
                if (Grade != 0) // If we are prompting, we are not removing this by removing the initiation/submersion that granted it
                    return false;
                if (!CommonFunctions.ConfirmDelete(LanguageManager.GetString("Message_DeleteComplexForm")))
                    return false;
            }

            ImprovementManager.RemoveImprovements(_objCharacter, Improvement.ImprovementSource.ComplexForm, InternalId);

            return _objCharacter.ComplexForms.Remove(this);
        }

        public async Task<bool> RemoveAsync(bool blnConfirmDelete = true, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (blnConfirmDelete)
            {
                if (Grade != 0) // If we are prompting, we are not removing this by removing the initiation/submersion that granted it
                    return false;
                if (!await CommonFunctions
                        .ConfirmDeleteAsync(
                            await LanguageManager.GetStringAsync("Message_DeleteComplexForm", token: token)
                                .ConfigureAwait(false), token).ConfigureAwait(false))
                    return false;
            }

            await ImprovementManager
                  .RemoveImprovementsAsync(_objCharacter, Improvement.ImprovementSource.ComplexForm, InternalId, token)
                  .ConfigureAwait(false);

            return await _objCharacter.ComplexForms.RemoveAsync(this, token).ConfigureAwait(false);
        }

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
    }
}
