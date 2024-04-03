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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Chummer.Backend.Attributes;

namespace Chummer
{
    /// <summary>
    /// An Initiation Grade.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Grade) + "}")]
    public class InitiationGrade : IHasInternalId, IComparable, ICanRemove, IHasCharacterObject
    {
        private Guid _guiID;
        private bool _blnGroup;
        private bool _blnOrdeal;
        private bool _blnSchooling;
        private bool _blnTechnomancer;
        private int _intGrade;
        private string _strNotes = string.Empty;
        private Color _colNotes = ColorManager.HasNotesColor;

        private readonly Character _objCharacter;

        public Character CharacterObject => _objCharacter; // readonly member, no locking needed

        #region Constructor, Create, Save, and Load Methods

        public InitiationGrade(Character objCharacter)
        {
            // Create the GUID for the new InitiationGrade.
            _guiID = Guid.NewGuid();
            _objCharacter = objCharacter;
        }

        /// <summary>
        /// Create an Initiation Grade from an XmlNode.
        /// </summary>
        /// <param name="intGrade">Grade number.</param>
        /// <param name="blnTechnomancer">Whether the character is a Technomancer.</param>
        /// <param name="blnGroup">Whether a Group was used.</param>
        /// <param name="blnOrdeal">Whether an Ordeal was used.</param>
        /// <param name="blnSchooling">Whether Schooling was used.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public void Create(int intGrade, bool blnTechnomancer, bool blnGroup, bool blnOrdeal, bool blnSchooling, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _intGrade = intGrade;
            _blnTechnomancer = blnTechnomancer;
            _blnGroup = blnGroup;
            _blnOrdeal = blnOrdeal;
            _blnSchooling = blnSchooling;
            //TODO: I'm not happy with this.
            //KC 90: a Cyberadept who has Submerged may restore Resonance that has been lost to cyberware (and only cyberware) by an amount equal to half their Submersion Grade(rounded up).
            //To handle this, we ceiling the CyberwareEssence value up, as a non-zero loss of Essence removes a point of Resonance, and cut the submersion grade in half.
            //Whichever value is lower becomes the value of the improvement.
            if (intGrade > 0 && blnTechnomancer)
            {
                token.ThrowIfCancellationRequested();
                using (_objCharacter.LockObject.EnterUpgradeableReadLock(token))
                {
                    token.ThrowIfCancellationRequested();
                    if (_objCharacter.RESEnabled && !_objCharacter.Settings.SpecialKarmaCostBasedOnShownValue
                                                 && ImprovementManager.GetCachedImprovementListForValueOf(_objCharacter,
                                                     Improvement.ImprovementType.CyberadeptDaemon, token: token).Count >
                                                 0)
                    {
                        decimal decNonCyberwareEssence = _objCharacter.BiowareEssence + _objCharacter.EssenceHole;
                        int intResonanceRecovered = Math.Min(intGrade.DivAwayFromZero(2), (int)(
                            Math.Ceiling(decNonCyberwareEssence) == Math.Floor(decNonCyberwareEssence)
                                ? Math.Ceiling(_objCharacter.CyberwareEssence)
                                : Math.Floor(_objCharacter.CyberwareEssence)));
                        // Cannot increase RES to be more than what it would be without any Essence loss.
                        intResonanceRecovered = _objCharacter.Settings.ESSLossReducesMaximumOnly
                            ? Math.Min(intResonanceRecovered,
                                _objCharacter.RES.MaximumNoEssenceLoss() - intGrade - _objCharacter.RES.TotalMaximum)
                            // +1 compared to normal because this Grade's effect has not been processed yet.
                            : Math.Min(intResonanceRecovered,
                                _objCharacter.RES.MaximumNoEssenceLoss() - intGrade + 1 - _objCharacter.RES.Value);
                        token.ThrowIfCancellationRequested();
                        using (_objCharacter.LockObject.EnterWriteLock(token))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                ImprovementManager.CreateImprovement(_objCharacter, "RESBase",
                                    Improvement.ImprovementSource.CyberadeptDaemon,
                                    InternalId, Improvement.ImprovementType.Attribute,
                                    string.Empty, 0, intResonanceRecovered, 0, 1, 1, token: token);
                            }
                            catch
                            {
                                ImprovementManager.Rollback(_objCharacter, CancellationToken.None);
                                throw;
                            }

                            ImprovementManager.Commit(_objCharacter);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create an Initiation Grade from an XmlNode.
        /// </summary>
        /// <param name="intGrade">Grade number.</param>
        /// <param name="blnTechnomancer">Whether the character is a Technomancer.</param>
        /// <param name="blnGroup">Whether a Group was used.</param>
        /// <param name="blnOrdeal">Whether an Ordeal was used.</param>
        /// <param name="blnSchooling">Whether Schooling was used.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task CreateAsync(int intGrade, bool blnTechnomancer, bool blnGroup, bool blnOrdeal, bool blnSchooling, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _intGrade = intGrade;
            _blnTechnomancer = blnTechnomancer;
            _blnGroup = blnGroup;
            _blnOrdeal = blnOrdeal;
            _blnSchooling = blnSchooling;
            //TODO: I'm not happy with this.
            //KC 90: a Cyberadept who has Submerged may restore Resonance that has been lost to cyberware (and only cyberware) by an amount equal to half their Submersion Grade(rounded up).
            //To handle this, we ceiling the CyberwareEssence value up, as a non-zero loss of Essence removes a point of Resonance, and cut the submersion grade in half.
            //Whichever value is lower becomes the value of the improvement.
            if (intGrade > 0 && blnTechnomancer)
            {
                IAsyncDisposable objLocker = await _objCharacter.LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    if (await _objCharacter.GetRESEnabledAsync(token).ConfigureAwait(false) && !_objCharacter.Settings
                                                                          .SpecialKarmaCostBasedOnShownValue
                                                                      && (await ImprovementManager
                                                                          .GetCachedImprovementListForValueOfAsync(
                                                                              _objCharacter,
                                                                              Improvement.ImprovementType
                                                                                  .CyberadeptDaemon, token: token).ConfigureAwait(false))
                                                                      .Count > 0)
                    {
                        decimal decNonCyberwareEssence = await _objCharacter.GetBiowareEssenceAsync(token).ConfigureAwait(false) +
                                                         await _objCharacter.GetEssenceHoleAsync(token).ConfigureAwait(false);
                        int intResonanceRecovered = Math.Min(intGrade.DivAwayFromZero(2), (int)(
                            Math.Ceiling(decNonCyberwareEssence) == Math.Floor(decNonCyberwareEssence)
                                ? Math.Ceiling(await _objCharacter.GetCyberwareEssenceAsync(token).ConfigureAwait(false))
                                : Math.Floor(await _objCharacter.GetCyberwareEssenceAsync(token).ConfigureAwait(false))));
                        // Cannot increase RES to be more than what it would be without any Essence loss.
                        CharacterAttrib objRes = await _objCharacter.GetAttributeAsync("RES", token: token).ConfigureAwait(false);
                        intResonanceRecovered = _objCharacter.Settings.ESSLossReducesMaximumOnly
                            ? Math.Min(intResonanceRecovered,
                                await objRes.MaximumNoEssenceLossAsync(token: token).ConfigureAwait(false) - intGrade -
                                await objRes.GetTotalMaximumAsync(token).ConfigureAwait(false))
                            // +1 compared to normal because this Grade's effect has not been processed yet.
                            : Math.Min(intResonanceRecovered,
                                await objRes.MaximumNoEssenceLossAsync(token: token).ConfigureAwait(false) - intGrade + 1 -
                                await objRes.GetValueAsync(token).ConfigureAwait(false));
                        token.ThrowIfCancellationRequested();
                        IAsyncDisposable objLocker2 = await _objCharacter.LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                        try
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                await ImprovementManager.CreateImprovementAsync(_objCharacter, "RESBase",
                                        Improvement.ImprovementSource.CyberadeptDaemon,
                                        InternalId, Improvement.ImprovementType.Attribute,
                                        string.Empty, 0, intResonanceRecovered, 0, 1, 1, token: token)
                                    .ConfigureAwait(false);
                            }
                            catch
                            {
                                await ImprovementManager.RollbackAsync(_objCharacter, CancellationToken.None)
                                    .ConfigureAwait(false);
                                throw;
                            }

                            await ImprovementManager.CommitAsync(_objCharacter, token).ConfigureAwait(false);
                        }
                        finally
                        {
                            await objLocker2.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    await objLocker.DisposeAsync().ConfigureAwait(false);
                }
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
            objWriter.WriteStartElement("initiationgrade");
            objWriter.WriteElementString("guid", _guiID.ToString("D", GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("res", _blnTechnomancer.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("grade", _intGrade.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("group", _blnGroup.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("ordeal", _blnOrdeal.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("schooling", _blnSchooling.ToString(GlobalSettings.InvariantCultureInfo));
            objWriter.WriteElementString("notes", _strNotes.CleanOfInvalidUnicodeChars());
            objWriter.WriteElementString("notesColor", ColorTranslator.ToHtml(_colNotes));
            objWriter.WriteEndElement();
        }

        /// <summary>
        /// Load the Initiation Grade from the XmlNode.
        /// </summary>
        /// <param name="objNode">XmlNode to load.</param>
        public void Load(XmlNode objNode)
        {
            if (objNode == null)
                return;
            if (!objNode.TryGetField("guid", Guid.TryParse, out _guiID))
                _guiID = Guid.NewGuid();
            objNode.TryGetBoolFieldQuickly("res", ref _blnTechnomancer);
            objNode.TryGetInt32FieldQuickly("grade", ref _intGrade);
            objNode.TryGetBoolFieldQuickly("group", ref _blnGroup);
            objNode.TryGetBoolFieldQuickly("ordeal", ref _blnOrdeal);
            objNode.TryGetBoolFieldQuickly("schooling", ref _blnSchooling);
            objNode.TryGetMultiLineStringFieldQuickly("notes", ref _strNotes);

            string sNotesColor = ColorTranslator.ToHtml(ColorManager.HasNotesColor);
            objNode.TryGetStringFieldQuickly("notesColor", ref sNotesColor);
            _colNotes = ColorTranslator.FromHtml(sNotesColor);
        }

        /// <summary>
        /// Print the object's XML to the XmlWriter.
        /// </summary>
        /// <param name="objWriter">XmlTextWriter to write with.</param>
        /// <param name="objCulture">Culture in which to print</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public async Task Print(XmlWriter objWriter, CultureInfo objCulture, CancellationToken token = default)
        {
            if (objWriter == null)
                return;
            // <initiationgrade>
            XmlElementWriteHelper objBaseElement = await objWriter.StartElementAsync("initiationgrade", token).ConfigureAwait(false);
            try
            {
                await objWriter.WriteElementStringAsync("guid", InternalId, token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("grade", Grade.ToString(objCulture), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("group", Group.ToString(GlobalSettings.InvariantCultureInfo), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("ordeal", Ordeal.ToString(GlobalSettings.InvariantCultureInfo), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("schooling", Schooling.ToString(GlobalSettings.InvariantCultureInfo), token).ConfigureAwait(false);
                await objWriter.WriteElementStringAsync("technomancer", Technomancer.ToString(GlobalSettings.InvariantCultureInfo), token).ConfigureAwait(false);
                if (GlobalSettings.PrintNotes)
                    await objWriter.WriteElementStringAsync("notes", Notes, token).ConfigureAwait(false);
            }
            finally
            {
                // </initiationgrade>
                await objBaseElement.DisposeAsync().ConfigureAwait(false);
            }
        }

        #endregion Constructor, Create, Save, and Load Methods

        #region Properties

        /// <summary>
        /// Internal identifier which will be used to identify this Initiation Grade in the Improvement system.
        /// </summary>
        public string InternalId => _guiID.ToString("D", GlobalSettings.InvariantCultureInfo);

        /// <summary>
        /// Initiate Grade.
        /// </summary>
        public int Grade
        {
            get => _intGrade;
            set => _intGrade = value;
        }

        /// <summary>
        /// Whether a Group was used.
        /// </summary>
        public bool Group
        {
            get => _blnGroup;
            set => _blnGroup = value;
        }

        /// <summary>
        /// Whether an Ordeal was used.
        /// </summary>
        public bool Ordeal
        {
            get => _blnOrdeal;
            set => _blnOrdeal = value;
        }

        /// <summary>
        /// Whether Schooling was used.
        /// </summary>
        public bool Schooling
        {
            get => _blnSchooling;
            set => _blnSchooling = value;
        }

        /// <summary>
        /// Whether the Initiation Grade is for a Technomancer.
        /// </summary>
        public bool Technomancer
        {
            get => _blnTechnomancer;
            set => _blnTechnomancer = value;
        }

        #endregion Properties

        #region Complex Properties

        /// <summary>
        /// The Initiation Grade's Karma cost.
        /// </summary>
        public int KarmaCost
        {
            get
            {
                CharacterSettings objSettings = _objCharacter.Settings;
                decimal decCost = objSettings.KarmaInitiationFlat + Grade * objSettings.KarmaInitiation;
                decimal decMultiplier = 1.0m;

                // Discount for Group.
                if (Group)
                    decMultiplier -= Technomancer
                        ? objSettings.KarmaRESInitiationGroupPercent
                        : objSettings.KarmaMAGInitiationGroupPercent;

                // Discount for Ordeal.
                if (Ordeal)
                    decMultiplier -= Technomancer
                        ? objSettings.KarmaRESInitiationOrdealPercent
                        : objSettings.KarmaMAGInitiationOrdealPercent;

                // Discount for Schooling.
                if (Schooling)
                    decMultiplier -= Technomancer
                        ? objSettings.KarmaRESInitiationSchoolingPercent
                        : objSettings.KarmaMAGInitiationSchoolingPercent;

                return (decCost * decMultiplier).StandardRound();
            }
        }

        /// <summary>
        /// Text to display in the Initiation Grade list.
        /// </summary>
        public string Text(string strLanguage)
        {
            string strSpace = LanguageManager.GetString("String_Space", strLanguage);
            using (new FetchSafelyFromPool<StringBuilder>(Utils.StringBuilderPool,
                                                          out StringBuilder sbdReturn))
            {
                sbdReturn.Append(LanguageManager.GetString("String_Grade", strLanguage)).Append(strSpace)
                         .Append(Grade.ToString(GlobalSettings.CultureInfo));
                if (Group || Ordeal)
                {
                    sbdReturn.Append(strSpace).Append('(');
                    if (Group)
                    {
                        sbdReturn.Append(
                            LanguageManager.GetString(Technomancer ? "String_Network" : "String_Group", strLanguage));
                        if (Ordeal || Schooling)
                            sbdReturn.Append(',').Append(strSpace);
                    }

                    if (Ordeal)
                    {
                        sbdReturn.Append(
                            LanguageManager.GetString(Technomancer ? "String_Task" : "String_Ordeal", strLanguage));
                        if (Schooling)
                            sbdReturn.Append(',').Append(strSpace);
                    }

                    if (Schooling)
                    {
                        sbdReturn.Append(LanguageManager.GetString("String_Schooling", strLanguage));
                    }

                    sbdReturn.Append(')');
                }

                return sbdReturn.ToString();
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

        public Color PreferredColor =>
            !string.IsNullOrEmpty(Notes)
                ? ColorManager.GenerateCurrentModeColor(NotesColor)
                : ColorManager.WindowText;

        #endregion Complex Properties

        #region Methods

        public TreeNode CreateTreeNode(ContextMenuStrip cmsInitiationGrade)
        {
            TreeNode objNode = new TreeNode
            {
                ContextMenuStrip = cmsInitiationGrade,
                Name = InternalId,
                Text = Text(GlobalSettings.Language),
                Tag = this,
                ForeColor = PreferredColor,
                ToolTipText = Notes.WordWrap()
            };
            return objNode;
        }

        public int CompareTo(object obj)
        {
            return CompareTo((InitiationGrade)obj);
        }

        public int CompareTo(InitiationGrade objGrade)
        {
            return objGrade == null ? 1 : Grade.CompareTo(objGrade.Grade);
        }

        #endregion Methods

        public bool Remove(bool blnConfirmDelete = true)
        {
            return Remove(blnConfirmDelete, true);
        }

        public bool Remove(bool blnConfirmDelete, bool blnPerformGradeCheck)
        {
            using (_objCharacter.LockObject.EnterUpgradeableReadLock())
            {
                // Stop if this isn't the highest grade
                if (_objCharacter.MAGEnabled)
                {
                    if (Grade != _objCharacter.InitiateGrade && blnPerformGradeCheck)
                    {
                        Program.ShowScrollableMessageBox(LanguageManager.GetString("Message_DeleteGrade"),
                            LanguageManager.GetString("MessageTitle_DeleteGrade"), MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return false;
                    }

                    if (blnConfirmDelete &&
                        !CommonFunctions.ConfirmDelete(LanguageManager.GetString("Message_DeleteInitiateGrade")))
                        return false;
                }
                else if (_objCharacter.RESEnabled)
                {
                    if (Grade != _objCharacter.SubmersionGrade && blnPerformGradeCheck)
                    {
                        Program.ShowScrollableMessageBox(LanguageManager.GetString("Message_DeleteGrade"),
                            LanguageManager.GetString("MessageTitle_DeleteGrade"), MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return false;
                    }

                    if (blnConfirmDelete &&
                        !CommonFunctions.ConfirmDelete(LanguageManager.GetString("Message_DeleteSubmersionGrade")))
                        return false;

                    ImprovementManager.RemoveImprovements(_objCharacter, Improvement.ImprovementSource.CyberadeptDaemon,
                        InternalId);
                }
                else
                    return false;

                using (_objCharacter.LockObject.EnterWriteLock())
                {
                    _objCharacter.InitiationGrades.Remove(this);
                    // Remove the child objects (arts, metamagics, enhancements, enchantments, rituals)
                    // Arts
                    for (int i = _objCharacter.Arts.Count - 1; i > 0; --i)
                    {
                        Art objLoop = _objCharacter.Arts[i];
                        if (objLoop.Grade == Grade)
                            objLoop.Remove(false);
                    }

                    // Metamagics
                    for (int i = _objCharacter.Metamagics.Count - 1; i > 0; --i)
                    {
                        Metamagic objLoop = _objCharacter.Metamagics[i];
                        if (objLoop.Grade == Grade)
                            objLoop.Remove(false);
                    }

                    // Enhancements
                    for (int i = _objCharacter.Enhancements.Count - 1; i > 0; --i)
                    {
                        Enhancement objLoop = _objCharacter.Enhancements[i];
                        if (objLoop.Grade == Grade)
                            objLoop.Remove(false);
                    }

                    // Spells
                    for (int i = _objCharacter.Spells.Count - 1; i > 0; --i)
                    {
                        Spell objLoop = _objCharacter.Spells[i];
                        if (objLoop.Grade == Grade)
                            objLoop.Remove(false);
                    }

                    // Complex Forms
                    for (int i = _objCharacter.ComplexForms.Count - 1; i > 0; --i)
                    {
                        ComplexForm objLoop = _objCharacter.ComplexForms[i];
                        if (objLoop.Grade == Grade)
                            objLoop.Remove(false);
                    }
                }
            }

            return true;
        }

        public Task<bool> RemoveAsync(bool blnConfirmDelete = true, CancellationToken token = default)
        {
            return RemoveAsync(blnConfirmDelete, true, token);
        }

        public async Task<bool> RemoveAsync(bool blnConfirmDelete, bool blnPerformGradeCheck,
                                                 CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            IAsyncDisposable objLocker = await _objCharacter.LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                // Stop if this isn't the highest grade
                if (await _objCharacter.GetMAGEnabledAsync(token).ConfigureAwait(false))
                {
                    if (Grade != await _objCharacter.GetInitiateGradeAsync(token).ConfigureAwait(false)
                        && blnPerformGradeCheck)
                    {
                        Program.ShowScrollableMessageBox(
                            await LanguageManager.GetStringAsync("Message_DeleteGrade", token: token)
                                .ConfigureAwait(false),
                            await LanguageManager.GetStringAsync("MessageTitle_DeleteGrade", token: token)
                                .ConfigureAwait(false), MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return false;
                    }

                    if (blnConfirmDelete && !await CommonFunctions
                            .ConfirmDeleteAsync(
                                await LanguageManager
                                    .GetStringAsync("Message_DeleteInitiateGrade", token: token)
                                    .ConfigureAwait(false), token).ConfigureAwait(false))
                        return false;
                }
                else if (await _objCharacter.GetRESEnabledAsync(token).ConfigureAwait(false))
                {
                    if (Grade != await _objCharacter.GetSubmersionGradeAsync(token).ConfigureAwait(false)
                        && blnPerformGradeCheck)
                    {
                        Program.ShowScrollableMessageBox(
                            await LanguageManager.GetStringAsync("Message_DeleteGrade", token: token)
                                .ConfigureAwait(false),
                            await LanguageManager.GetStringAsync("MessageTitle_DeleteGrade", token: token)
                                .ConfigureAwait(false), MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return false;
                    }

                    if (blnConfirmDelete && !await CommonFunctions
                            .ConfirmDeleteAsync(
                                await LanguageManager
                                    .GetStringAsync("Message_DeleteSubmersionGrade", token: token)
                                    .ConfigureAwait(false), token).ConfigureAwait(false))
                        return false;

                    await ImprovementManager
                        .RemoveImprovementsAsync(_objCharacter, Improvement.ImprovementSource.CyberadeptDaemon,
                            InternalId, token).ConfigureAwait(false);
                }
                else
                    return false;

                token.ThrowIfCancellationRequested();
                IAsyncDisposable objLocker2 = await _objCharacter.LockObject.EnterWriteLockAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    await _objCharacter.InitiationGrades.RemoveAsync(this, token).ConfigureAwait(false);
                    // Remove the child objects (arts, metamagics, enhancements, enchantments, rituals)
                    // Arts
                    for (int i = await _objCharacter.Arts.GetCountAsync(token).ConfigureAwait(false) - 1; i > 0; --i)
                    {
                        Art objLoop = await _objCharacter.Arts.GetValueAtAsync(i, token).ConfigureAwait(false);
                        if (objLoop.Grade == Grade)
                            await objLoop.RemoveAsync(false, token).ConfigureAwait(false);
                    }

                    // Metamagics
                    for (int i = await _objCharacter.Metamagics.GetCountAsync(token).ConfigureAwait(false) - 1;
                         i > 0;
                         --i)
                    {
                        Metamagic objLoop =
                            await _objCharacter.Metamagics.GetValueAtAsync(i, token).ConfigureAwait(false);
                        if (objLoop.Grade == Grade)
                            await objLoop.RemoveAsync(false, token).ConfigureAwait(false);
                    }

                    // Enhancements
                    for (int i = await _objCharacter.Enhancements.GetCountAsync(token).ConfigureAwait(false) - 1;
                         i > 0;
                         --i)
                    {
                        Enhancement objLoop =
                            await _objCharacter.Enhancements.GetValueAtAsync(i, token).ConfigureAwait(false);
                        if (objLoop.Grade == Grade)
                            await objLoop.RemoveAsync(false, token).ConfigureAwait(false);
                    }

                    // Spells
                    for (int i = await _objCharacter.Spells.GetCountAsync(token).ConfigureAwait(false) - 1; i > 0; --i)
                    {
                        Spell objLoop = await _objCharacter.Spells.GetValueAtAsync(i, token).ConfigureAwait(false);
                        if (objLoop.Grade == Grade)
                            await objLoop.RemoveAsync(false, token).ConfigureAwait(false);
                    }

                    // Complex Forms
                    for (int i = await _objCharacter.ComplexForms.GetCountAsync(token).ConfigureAwait(false) - 1;
                         i > 0;
                         --i)
                    {
                        ComplexForm objLoop =
                            await _objCharacter.ComplexForms.GetValueAtAsync(i, token).ConfigureAwait(false);
                        if (objLoop.Grade == Grade)
                            await objLoop.RemoveAsync(false, token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await objLocker2.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is InitiationGrade objGrade)
            {
                return Grade.Equals(objGrade.Grade);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (InternalId, Grade, Group, Ordeal, Schooling, Technomancer, Notes).GetHashCode();
        }

        public static bool operator ==(InitiationGrade left, InitiationGrade right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(InitiationGrade left, InitiationGrade right)
        {
            return !(left == right);
        }

        public static bool operator <(InitiationGrade left, InitiationGrade right)
        {
            return left is null ? !(right is null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(InitiationGrade left, InitiationGrade right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(InitiationGrade left, InitiationGrade right)
        {
            return !(left is null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(InitiationGrade left, InitiationGrade right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
    }
}
