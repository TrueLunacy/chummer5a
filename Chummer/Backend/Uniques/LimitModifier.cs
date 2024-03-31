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
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Chummer
{
    public enum LimitType
    {
        Physical = 0,
        Mental,
        Social,
        Astral,
        NumLimitTypes // 🡐 This one should always be the last defined enum
    }

    /// <summary>
    /// A Skill Limit Modifier.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DisplayName) + "}")]
    public class LimitModifier : IHasInternalId, IHasName, ICanRemove, IHasCharacterObject
    {
        private Guid _guiID;
        private bool _blnCanDelete = true;
        private string _strName = string.Empty;
        private string _strNotes = string.Empty;
        private string _strLimit = string.Empty;
        private string _strCondition = string.Empty;
        private int _intBonus;
        private readonly Character _objCharacter;

        public Character CharacterObject => _objCharacter; // readonly member, no locking needed

        #region Constructor, Create, Save, Load, and Print Methods

        public LimitModifier(Character objCharacter, string strGuid = "")
        {
            // Create the GUID for the new Skill Limit Modifier.
            _guiID = strGuid.IsGuid() ? new Guid(strGuid) : Guid.NewGuid();
            _objCharacter = objCharacter;
        }

        /// <summary>
        /// Create a Skill Limit Modifier from properties.
        /// </summary>
        /// <param name="strName">The name of the modifier.</param>
        /// <param name="intBonus">The bonus amount.</param>
        /// <param name="strLimit">The limit this modifies.</param>
        /// <param name="strCondition">Condition when the limit modifier is to be activated.</param>
        /// <param name="blnCanDelete">Can this limit modifier be deleted.</param>
        public void Create(string strName, int intBonus, string strLimit, string strCondition, bool blnCanDelete)
        {
            _strName = strName;
            _strLimit = strLimit;
            _intBonus = intBonus;
            _strCondition = strCondition;
            _blnCanDelete = blnCanDelete;
        }

        /// <summary>
        /// Save the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        public void Save(XmlWriter objWriter)
        {
            if (objWriter == null)
                return;
            objWriter.WriteStartElement("limitmodifier");
            objWriter.WriteElementString("guid", _guiID.ToString("D", GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("name", _strName);
            objWriter.WriteElementString("limit", _strLimit);
            objWriter.WriteElementString("bonus", _intBonus.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("condition", _strCondition);
            objWriter.WriteElementString("candelete", _blnCanDelete.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("notes", _strNotes.CleanOfInvalidUnicodeChars());
            objWriter.WriteEndElement();
        }

        /// <summary>
        /// Load the Skill Limit Modifier from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        public void Load(XmlNode objNode)
        {
            if (!objNode.TryGetField("guid", out _guiID))
                _guiID = Guid.NewGuid();
            objNode.TryGetStringFieldQuickly("name", ref _strName);
            objNode.TryGetStringFieldQuickly("limit", ref _strLimit);
            objNode.TryGetFieldUninitialized("bonus", ref _intBonus);
            objNode.TryGetStringFieldQuickly("condition", ref _strCondition);
            if (!objNode.TryGetFieldUninitialized("candelete", ref _blnCanDelete))
            {
                _blnCanDelete = _objCharacter.Improvements.All(x => x.ImproveType != Improvement.ImprovementType.LimitModifier || x.ImprovedName != InternalId);
            }
            objNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with</param>
        /// <param name="objCulture">Culture in which to print</param>
        /// <param name="strLanguageToPrint">Language in which to print</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task Print(XmlWriter objWriter, CultureInfo objCulture, string strLanguageToPrint, CancellationToken token = default)
        {
            if (objWriter == null)
                return;
            // <limitmodifier>
            XmlElementWriteHelper objBaseElement = await objWriter.StartElementAsync("limitmodifier", token: token).ConfigureAwait(false);
            try
            {
                await objWriter.WriteElementStringAsync("guid", InternalId, token: token).ConfigureAwait(false);
                await objWriter
                      .WriteElementStringAsync(
                          "name", await DisplayNameAsync(objCulture, strLanguageToPrint, token).ConfigureAwait(false),
                          token: token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("name_english", Name, token: token).ConfigureAwait(false);
                await objWriter
                      .WriteElementStringAsync("condition",
                                               await _objCharacter
                                                     .TranslateExtraAsync(Condition, strLanguageToPrint, token: token)
                                                     .ConfigureAwait(false), token: token).ConfigureAwait(false);
                if (GlobalSettings.PrintNotes)
                    await objWriter.WriteElementStringAsync("notes", Notes, token: token).ConfigureAwait(false);
            }
            finally
            {
                // </limitmodifier>
                await objBaseElement.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Constructor, Create, Save, Load, and Print Methods

        #region Properties

        /// <summary>
        /// Internal identifier which will be used to identify this Skill Limit Modifier in the Improvement system.
        /// </summary>
        public string InternalId => _guiID.ToString("D", GlobalSettings.InvariantCultureInfo);

        /// <summary>
        /// Name.
        /// </summary>
        public string Name
        {
            get => _strName;
            set => _strName = value;
        }

        /// <summary>
        /// Name.
        /// </summary>
        public string Notes
        {
            get => _strNotes;
            set => _strNotes = value;
        }

        /// <summary>
        /// Limit.
        /// </summary>
        public string Limit
        {
            get => _strLimit;
            set => _strLimit = value;
        }

        private string _strCachedCondition = string.Empty;
        private string _strCachedDisplayCondition = string.Empty;
        private string _strCachedDisplayConditionLanguage = string.Empty;

        /// <summary>
        /// Condition.
        /// </summary>
        public string Condition
        {
            get
            {
                // If we've already cached a value for this, just return it.
                // TODO: invalidate cache if active language changes
                // (Ghetto fix cache culture tag and compare to current?)
                if (!string.IsNullOrWhiteSpace(_strCachedCondition))
                {
                    return _strCachedCondition;
                }

                // Assume that if the original string contains spaces it's not a
                // valid language key. Spare checking it against the dictionary.
                _strCachedCondition = _strCondition.Contains(' ')
                    ? _strCondition
                    : LanguageManager.GetString(_strCondition, false);
                if (string.IsNullOrWhiteSpace(_strCachedCondition))
                {
                    _strCachedCondition = _strCondition;
                }

                return _strCachedCondition;
            }
            set
            {
                if (Interlocked.Exchange(ref _strCondition, value) == value)
                    return;
                _strCachedCondition = string.Empty;
                _strCachedDisplayCondition = string.Empty;
                _strCachedDisplayConditionLanguage = string.Empty;
            }
        }

        public string DisplayCondition(string strLanguage)
        {
            // If we've already cached a value for this, just return it.
            // (Ghetto fix cache culture tag and compare to current?)
            if (!string.IsNullOrWhiteSpace(_strCachedDisplayCondition) && strLanguage == _strCachedDisplayConditionLanguage)
            {
                return _strCachedDisplayCondition;
            }

            _strCachedDisplayConditionLanguage = strLanguage;
            // Assume that if the original string contains spaces it's not a
            // valid language key. Spare checking it against the dictionary.
            _strCachedDisplayCondition = _strCondition.Contains(' ')
                ? _strCondition
                : LanguageManager.GetString(_strCondition, strLanguage, false);
            if (string.IsNullOrWhiteSpace(_strCachedDisplayCondition))
            {
                _strCachedDisplayCondition = _strCondition;
            }

            return _strCachedDisplayCondition;
        }

        public async Task<string> DisplayConditionAsync(string strLanguage, CancellationToken token = default)
        {
            // If we've already cached a value for this, just return it.
            // (Ghetto fix cache culture tag and compare to current?)
            if (!string.IsNullOrWhiteSpace(_strCachedDisplayCondition) && strLanguage == _strCachedDisplayConditionLanguage)
            {
                return _strCachedDisplayCondition;
            }

            _strCachedDisplayConditionLanguage = strLanguage;
            // Assume that if the original string contains spaces it's not a
            // valid language key. Spare checking it against the dictionary.
            _strCachedDisplayCondition = _strCondition.Contains(' ')
                ? _strCondition
                : await LanguageManager.GetStringAsync(_strCondition, strLanguage, false, token).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(_strCachedDisplayCondition))
            {
                _strCachedDisplayCondition = _strCondition;
            }

            return _strCachedDisplayCondition;
        }

        /// <summary>
        /// Bonus.
        /// </summary>
        public int Bonus
        {
            get => _intBonus;
            set => _intBonus = value;
        }

        /// <summary>
        /// Whether this limit can be modified and/or deleted
        /// </summary>
        public bool CanDelete => _blnCanDelete;

        /// <summary>
        /// The name of the object as it should be displayed on printouts (translated name only).
        /// </summary>
        public string DisplayNameShort
        {
            get
            {
                string strReturn = _strName;
                return strReturn;
            }
        }

        /// <summary>
        /// The name of the object as it should be displayed in lists. Name (Extra).
        /// </summary>
        public string CurrentDisplayName => DisplayName(GlobalSettings.CultureInfo, GlobalSettings.Language);

        public Task<string> GetCurrentDisplayNameAsync(CancellationToken token = default) => DisplayNameAsync(GlobalSettings.CultureInfo, GlobalSettings.Language, token);

        public string DisplayName(CultureInfo objCulture, string strLanguage)
        {
            string strBonus;
            if (_intBonus > 0)
                strBonus = '+' + _intBonus.ToString(objCulture);
            else
                strBonus = _intBonus.ToString(objCulture);

            string strSpace = LanguageManager.GetString("String_Space", strLanguage);
            string strReturn = DisplayNameShort + strSpace + '[' + strBonus + ']';
            string strCondition = DisplayCondition(strLanguage);
            if (!string.IsNullOrEmpty(strCondition))
                strReturn += strSpace + '(' + strCondition + ')';
            return strReturn;
        }

        public async Task<string> DisplayNameAsync(CultureInfo objCulture, string strLanguage, CancellationToken token = default)
        {
            string strBonus;
            if (_intBonus > 0)
                strBonus = '+' + _intBonus.ToString(objCulture);
            else
                strBonus = _intBonus.ToString(objCulture);

            string strSpace = await LanguageManager.GetStringAsync("String_Space", strLanguage, token: token).ConfigureAwait(false);
            string strReturn = DisplayNameShort + strSpace + '[' + strBonus + ']';
            string strCondition = await DisplayConditionAsync(strLanguage, token).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(strCondition))
                strReturn += strSpace + '(' + strCondition + ')';
            return strReturn;
        }

        #endregion Properties

        #region UI Methods

        public TreeNode CreateTreeNode(ContextMenuStrip cmsLimitModifier)
        {
            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                ContextMenuStrip = cmsLimitModifier,
                Text = CurrentDisplayName,
                Tag = this,
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
                    return !CanDelete
                        ? ColorManager.GrayHasNotesColor
                        : ColorManager.HasNotesColor;
                }
                return !CanDelete
                    ? ColorManager.GrayText
                    : ColorManager.WindowText;
            }
        }

        #endregion UI Methods

        public bool Remove(bool blnConfirmDelete = true)
        {
            if (_objCharacter.LimitModifiers.Contains(this) && blnConfirmDelete)
            {
                return CommonFunctions.ConfirmDelete(LanguageManager.GetString("Message_DeleteLimitModifier"))
                       && _objCharacter.LimitModifiers.Remove(this);
            }

            // No character-created limits found, which means it comes from an improvement.
            // TODO: ImprovementSource exists for a reason.
            Program.ShowScrollableMessageBox(LanguageManager.GetString("Message_CannotDeleteLimitModifier"),
                                             LanguageManager.GetString("MessageTitle_CannotDeleteLimitModifier"),
                                             MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        public async Task<bool> RemoveAsync(bool blnConfirmDelete = true, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (await _objCharacter.LimitModifiers.ContainsAsync(this, token).ConfigureAwait(false) && blnConfirmDelete)
            {
                return await CommonFunctions.ConfirmDeleteAsync(
                           await LanguageManager.GetStringAsync("Message_DeleteLimitModifier", token: token).ConfigureAwait(false), token).ConfigureAwait(false)
                       && await _objCharacter.LimitModifiers.RemoveAsync(this, token).ConfigureAwait(false);
            }

            // No character-created limits found, which means it comes from an improvement.
            // TODO: ImprovementSource exists for a reason.
            Program.ShowScrollableMessageBox(
                await LanguageManager.GetStringAsync("Message_CannotDeleteLimitModifier", token: token).ConfigureAwait(false),
                await LanguageManager.GetStringAsync("MessageTitle_CannotDeleteLimitModifier", token: token).ConfigureAwait(false),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
    }
}
