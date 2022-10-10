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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chummer.Annotations;
using Chummer.Backend.Attributes;
using Chummer.Backend.Skills;
using Chummer.Properties;
using Timer = System.Windows.Forms.Timer;

namespace Chummer.UI.Skills
{
    [DebuggerDisplay("{_objSkill.Name}")]
    public sealed partial class SkillControl : UserControl
    {
        private readonly bool _blnLoading = true;
        private bool _blnUpdatingSpec = true;
        private readonly Skill _objSkill;
        private readonly Timer _tmrSpecChangeTimer;
        private readonly Font _fntNormal;
        private readonly Font _fntItalic;
        private readonly Font _fntNormalName;
        private readonly Font _fntItalicName;
        private CharacterAttrib _objAttributeActive;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Button cmdDelete;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly ButtonWithToolTip btnCareerIncrease;

        private readonly Label lblCareerRating;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly NumericUpDownEx nudKarma;

        private readonly NumericUpDownEx nudSkill;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Label lblCareerSpec;

        private readonly ButtonWithToolTip btnAddSpec;
        private readonly ElasticComboBox cboSpec;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly ColorableCheckBox chkKarma;

        private readonly ElasticComboBox cboSelectAttribute;

        public SkillControl(Skill objSkill)
        {
            if (objSkill == null)
                return;
            _objSkill = objSkill;
            _objAttributeActive = objSkill.AttributeObject;
            if (_objAttributeActive != null)
                _objAttributeActive.PropertyChanged += Attribute_PropertyChanged;
            InitializeComponent();
            Disposed += (sender, args) => UnbindSkillControl();
            SuspendLayout();
            pnlAttributes.SuspendLayout();
            tlpMain.SuspendLayout();
            tlpRight.SuspendLayout();
            try
            {
                //Display
                _fntNormalName = lblName.Font;
                _fntItalicName = new Font(_fntNormalName, FontStyle.Italic);
                _fntNormal = btnAttribute.Font;
                _fntItalic = new Font(_fntNormal, FontStyle.Italic);
                Disposed += (sender, args) =>
                {
                    _fntItalicName.Dispose();
                    _fntItalic.Dispose();
                };

                if (!_objSkill.Default)
                    lblName.Font = _fntItalicName;
                lblName.DoOneWayDataBinding("Text", objSkill, nameof(Skill.CurrentDisplayName));
                lblName.DoOneWayDataBinding("ForeColor", objSkill, nameof(Skill.PreferredColor));
                lblName.DoOneWayDataBinding("ToolTipText", objSkill, nameof(Skill.HtmlSkillToolTip));

                btnAttribute.DoOneWayDataBinding("Text", objSkill, nameof(Skill.DisplayAttribute));

                RefreshPoolTooltipAndDisplay();

                // Creating controls outside of the designer saves on handles if the controls would be invisible anyway
                if (objSkill.AllowDelete) // For active skills, can only change by going from Create to Career mode, so no databinding necessary
                {
                    cmdDelete = new Button
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(3, 0, 3, 0),
                        Name = "cmdDelete",
                        Tag = "String_Delete",
                        Text = "Delete",
                        UseVisualStyleBackColor = true
                    };
                    cmdDelete.Click += cmdDelete_Click;
                    tlpRight.Controls.Add(cmdDelete, 4, 0);
                }

                if (objSkill.CharacterObject.Created)
                {
                    lblCareerRating = new Label
                    {
                        Anchor = AnchorStyles.Right,
                        AutoSize = true,
                        Margin = new Padding(3, 6, 3, 6),
                        Name = "lblCareerRating",
                        Text = "00",
                        TextAlign = ContentAlignment.MiddleRight
                    };
                    btnCareerIncrease = new ButtonWithToolTip
                    {
                        Anchor = AnchorStyles.Right,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        ImageDpi96 = Resources.add,
                        ImageDpi192 = Resources.add1,
                        MinimumSize = new Size(24, 24),
                        Name = "btnCareerIncrease",
                        Padding = new Padding(1),
                        UseVisualStyleBackColor = true
                    };
                    btnCareerIncrease.Click += btnCareerIncrease_Click;

                    lblCareerRating.DoOneWayDataBinding("Text", objSkill, nameof(Skill.Rating));
                    btnCareerIncrease.DoOneWayDataBinding("Enabled", objSkill, nameof(Skill.CanUpgradeCareer));
                    btnCareerIncrease.DoOneWayDataBinding("ToolTipText", objSkill, nameof(Skill.UpgradeToolTip));

                    tlpMain.Controls.Add(lblCareerRating, 2, 0);
                    tlpMain.Controls.Add(btnCareerIncrease, 3, 0);

                    btnAddSpec = new ButtonWithToolTip
                    {
                        Anchor = AnchorStyles.Right,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        ImageDpi96 = Resources.add,
                        ImageDpi192 = Resources.add1,
                        MinimumSize = new Size(24, 24),
                        Name = "btnAddSpec",
                        Padding = new Padding(1),
                        UseVisualStyleBackColor = true
                    };
                    btnAddSpec.Click += btnAddSpec_Click;
                    lblCareerSpec = new Label
                    {
                        Anchor = AnchorStyles.Left,
                        AutoSize = true,
                        Margin = new Padding(3, 6, 3, 6),
                        Name = "lblCareerSpec",
                        Text = "[Specializations]",
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    lblCareerSpec.DoOneWayDataBinding("Text", objSkill, nameof(Skill.CurrentDisplaySpecialization));
                    btnAddSpec.DoOneWayDataBinding("Enabled", objSkill, nameof(Skill.CanAffordSpecialization));
                    btnAddSpec.DoOneWayDataBinding("Visible", objSkill, nameof(Skill.CanHaveSpecs));
                    btnAddSpec.DoOneWayDataBinding("ToolTipText", objSkill, nameof(Skill.AddSpecToolTip));

                    tlpRight.Controls.Add(lblCareerSpec, 0, 0);
                    tlpRight.Controls.Add(btnAddSpec, 1, 0);

                    using (new FetchSafelyFromPool<List<ListItem>>(Utils.ListItemListPool,
                                                                   out List<ListItem> lstAttributeItems))
                    {
                        foreach (string strLoopAttribute in AttributeSection.AttributeStrings)
                        {
                            if (strLoopAttribute == "MAGAdept")
                            {
                                if (!objSkill.CharacterObject.Settings.MysAdeptSecondMAGAttribute)
                                    continue;
                                lstAttributeItems.Add(new ListItem(strLoopAttribute, LanguageManager.MAGAdeptString()));
                            }
                            else
                            {
                                string strAttributeShort = LanguageManager.GetString(
                                    "String_Attribute" + strLoopAttribute + "Short", GlobalSettings.Language, false);
                                lstAttributeItems.Add(new ListItem(strLoopAttribute,
                                                                   !string.IsNullOrEmpty(strAttributeShort)
                                                                       ? strAttributeShort
                                                                       : strLoopAttribute));
                            }
                        }

                        cboSelectAttribute = new ElasticComboBox
                        {
                            Dock = DockStyle.Fill,
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            FormattingEnabled = true,
                            Margin = new Padding(3, 0, 3, 0),
                            Name = "cboSelectAttribute"
                        };
                        cboSelectAttribute.PopulateWithListItems(lstAttributeItems);
                        cboSelectAttribute.SelectedValue = _objSkill.AttributeObject.Abbrev;
                        cboSelectAttribute.DropDownClosed += cboSelectAttribute_Closed;
                        pnlAttributes.Controls.Add(cboSelectAttribute);
                    }
                }
                else
                {
                    nudSkill = new NumericUpDownEx
                    {
                        Anchor = AnchorStyles.Right,
                        AutoSize = true,
                        InterceptMouseWheel = NumericUpDownEx.InterceptMouseWheelMode.WhenMouseOver,
                        Margin = new Padding(3, 2, 3, 2),
                        Maximum = new decimal(new[] { 99, 0, 0, 0 }),
                        Name = "nudSkill"
                    };
                    nudKarma = new NumericUpDownEx
                    {
                        Anchor = AnchorStyles.Right,
                        AutoSize = true,
                        InterceptMouseWheel = NumericUpDownEx.InterceptMouseWheelMode.WhenMouseOver,
                        Margin = new Padding(3, 2, 3, 2),
                        Maximum = new decimal(new[] { 99, 0, 0, 0 }),
                        Name = "nudKarma"
                    };

                    // Trick to make it seem like the button is a label (+ onclick method not doing anything in Create mode)
                    btnAttribute.FlatAppearance.MouseDownBackColor = Color.Transparent;
                    btnAttribute.FlatAppearance.MouseOverBackColor = Color.Transparent;

                    nudSkill.DoOneWayDataBinding("Visible", objSkill.CharacterObject,
                        nameof(objSkill.CharacterObject.EffectiveBuildMethodUsesPriorityTables));
                    nudSkill.DoDataBinding("Value", objSkill, nameof(Skill.Base));
                    nudSkill.DoOneWayDataBinding("Enabled", objSkill, nameof(Skill.BaseUnlocked));
                    nudSkill.InterceptMouseWheel = GlobalSettings.InterceptMode;
                    nudKarma.DoDataBinding("Value", objSkill, nameof(Skill.Karma));
                    nudKarma.DoOneWayDataBinding("Enabled", objSkill, nameof(Skill.KarmaUnlocked));
                    nudKarma.InterceptMouseWheel = GlobalSettings.InterceptMode;

                    tlpMain.Controls.Add(nudSkill, 2, 0);
                    tlpMain.Controls.Add(nudKarma, 3, 0);

                    if (objSkill.IsExoticSkill)
                    {
                        lblCareerSpec = new Label
                        {
                            Anchor = AnchorStyles.Left,
                            AutoSize = true,
                            Margin = new Padding(3, 6, 3, 6),
                            Name = "lblCareerSpec",
                            Text = "[Specializations]",
                            TextAlign = ContentAlignment.MiddleLeft
                        };
                        lblCareerSpec.DoOneWayDataBinding("Text", objSkill, nameof(Skill.CurrentDisplaySpecialization));
                        tlpRight.Controls.Add(lblCareerSpec, 0, 0);
                    }
                    else
                    {
                        cboSpec = new ElasticComboBox
                        {
                            Anchor = AnchorStyles.Left | AnchorStyles.Right,
                            AutoCompleteMode = AutoCompleteMode.Suggest,
                            FormattingEnabled = true,
                            Margin = new Padding(3, 0, 3, 0),
                            Name = "cboSpec",
                            Sorted = true,
                            TabStop = false
                        };
                        cboSpec.PopulateWithListItems(objSkill.CGLSpecializations);
                        cboSpec.DoOneWayDataBinding("Enabled", objSkill, nameof(Skill.CanHaveSpecs));
                        cboSpec.Text = objSkill.CurrentDisplaySpecialization;
                        cboSpec.TextChanged += cboSpec_TextChanged;
                        _blnUpdatingSpec = false;
                        _tmrSpecChangeTimer = new Timer { Interval = 1000 };
                        _tmrSpecChangeTimer.Tick += SpecChangeTimer_Tick;
                        chkKarma = new ColorableCheckBox
                        {
                            Anchor = AnchorStyles.Left,
                            AutoSize = true,
                            DefaultColorScheme = true,
                            Margin = new Padding(3, 4, 3, 4),
                            Name = "chkKarma",
                            UseVisualStyleBackColor = true
                        };
                        chkKarma.DoOneWayDataBinding("Visible", objSkill.CharacterObject,
                            nameof(objSkill.CharacterObject.EffectiveBuildMethodUsesPriorityTables));
                        chkKarma.DoDataBinding("Checked", objSkill, nameof(Skill.BuyWithKarma));
                        chkKarma.DoOneWayDataBinding("Enabled", objSkill, nameof(Skill.CanHaveSpecs));
                        tlpRight.Controls.Add(cboSpec, 0, 0);
                        tlpRight.Controls.Add(chkKarma, 1, 0);

                        // Hacky way of fixing a weird UI issue caused by items of a combobox only being populated from the DataSource after the combobox is added
                        _blnUpdatingSpec = true;
                        try
                        {
                            cboSpec.Text = objSkill.CurrentDisplaySpecialization;
                        }
                        finally
                        {
                            _blnUpdatingSpec = false;
                        }
                    }
                }

                this.DoOneWayDataBinding("Enabled", objSkill, nameof(Skill.Enabled));
                this.DoOneWayDataBinding("BackColor", objSkill, nameof(Skill.PreferredControlColor));

                AdjustForDpi();
                this.UpdateLightDarkMode();
                this.TranslateWinForm(blnDoResumeLayout: false);

                foreach (ToolStripItem tssItem in cmsSkillLabel.Items)
                {
                    tssItem.UpdateLightDarkMode();
                    tssItem.TranslateToolStripItemsRecursively();
                }
            }
            finally
            {
                _blnLoading = false;
                tlpRight.ResumeLayout();
                tlpMain.ResumeLayout();
                pnlAttributes.ResumeLayout();
                ResumeLayout(true);
                _objSkill.PropertyChanged += Skill_PropertyChanged;
            }
        }

        private async void Skill_PropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (_blnLoading)
                return;

            bool blnUpdateAll = false;
            //I learned something from this but i'm not sure it is a good solution
            //scratch that, i'm sure it is a bad solution. (Tooltip manager from tooltip, properties from reflection?

            //if name of changed is null it does magic to change all, otherwise it only does one.
            switch (propertyChangedEventArgs?.PropertyName)
            {
                case null:
                    blnUpdateAll = true;
                    goto case nameof(Skill.DisplayPool);
                case nameof(Skill.DisplayPool):
                    await RefreshPoolTooltipAndDisplayAsync().ConfigureAwait(false);
                    if (blnUpdateAll)
                        goto case nameof(Skill.Default);
                    break;

                case nameof(Skill.Default):
                    await lblName.DoThreadSafeAsync(x => x.Font = !_objSkill.Default ? _fntItalicName : _fntNormalName).ConfigureAwait(false);
                    if (blnUpdateAll)
                        goto case nameof(Skill.DefaultAttribute);
                    break;

                case nameof(Skill.DefaultAttribute):
                    if (cboSelectAttribute != null)
                    {
                        await cboSelectAttribute.DoThreadSafeAsync(x => x.SelectedValue = _objSkill.AttributeObject.Abbrev).ConfigureAwait(false);
                        await DoSelectAttributeClosed().ConfigureAwait(false);
                    }
                    else
                    {
                        await SetAttributeActiveAsync(_objSkill.AttributeObject).ConfigureAwait(false);
                    }
                    if (blnUpdateAll)
                        goto case nameof(Skill.TopMostDisplaySpecialization);
                    break;

                case nameof(Skill.TopMostDisplaySpecialization):
                    if (cboSpec != null && !_blnUpdatingSpec)
                    {
                        string strDisplaySpec = await _objSkill.GetTopMostDisplaySpecializationAsync().ConfigureAwait(false);
                        _blnUpdatingSpec = true;
                        try
                        {
                            await cboSpec.DoThreadSafeAsync(x => x.Text = strDisplaySpec).ConfigureAwait(false);
                        }
                        finally
                        {
                            _blnUpdatingSpec = false;
                        }
                    }
                    if (blnUpdateAll)
                        goto case nameof(Skill.CGLSpecializations);
                    break;

                case nameof(Skill.CGLSpecializations):
                    if (cboSpec != null && await cboSpec.DoThreadSafeFuncAsync(x => x.Visible).ConfigureAwait(false))
                    {
                        string strOldSpec = await cboSpec.DoThreadSafeFuncAsync(x => x.Text).ConfigureAwait(false);
                        IReadOnlyList<ListItem> lstSpecializations = await _objSkill.GetCGLSpecializationsAsync().ConfigureAwait(false);
                        _blnUpdatingSpec = true;
                        try
                        {
                            await cboSpec.PopulateWithListItemsAsync(lstSpecializations).ConfigureAwait(false);
                            await cboSpec.DoThreadSafeAsync(x =>
                            {
                                if (string.IsNullOrEmpty(strOldSpec))
                                    x.SelectedIndex = -1;
                                else
                                {
                                    x.SelectedValue = strOldSpec;
                                    if (x.SelectedIndex == -1)
                                        x.Text = strOldSpec;
                                }
                            }).ConfigureAwait(false);
                        }
                        finally
                        {
                            _blnUpdatingSpec = false;
                        }
                    }
                    if (blnUpdateAll)
                        goto case nameof(Skill.Specializations);
                    break;

                case nameof(Skill.Specializations):
                    {
                        if (await Program.GetFormForDialogAsync(_objSkill.CharacterObject).ConfigureAwait(false) is CharacterShared frmParent)
                            await frmParent.RequestCharacterUpdate().ConfigureAwait(false);
                        break;
                    }
            }
        }

        private async void Attribute_PropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (_blnLoading)
                return;

            switch (propertyChangedEventArgs?.PropertyName)
            {
                case null:
                case nameof(CharacterAttrib.Abbrev):
                case nameof(CharacterAttrib.TotalValue):
                    await RefreshPoolTooltipAndDisplayAsync().ConfigureAwait(false);
                    break;
            }
        }

        private async void btnCareerIncrease_Click(object sender, EventArgs e)
        {
            using (await EnterReadLock.EnterAsync(_objSkill.LockObject).ConfigureAwait(false))
            {
                string confirmstring = string.Format(GlobalSettings.CultureInfo,
                                                     await LanguageManager.GetStringAsync(
                                                         "Message_ConfirmKarmaExpense").ConfigureAwait(false),
                                                     await _objSkill.GetCurrentDisplayNameAsync().ConfigureAwait(false),
                                                     await _objSkill.GetRatingAsync().ConfigureAwait(false) + 1,
                                                     await _objSkill.GetUpgradeKarmaCostAsync().ConfigureAwait(false));

                if (!await CommonFunctions.ConfirmKarmaExpenseAsync(confirmstring).ConfigureAwait(false))
                    return;

                await _objSkill.Upgrade().ConfigureAwait(false);
            }
        }

        private async void btnAddSpec_Click(object sender, EventArgs e)
        {
            using (await EnterReadLock.EnterAsync(_objSkill.LockObject).ConfigureAwait(false))
            {
                int price = _objSkill.CharacterObject.Settings.KarmaSpecialization;

                decimal decExtraSpecCost = 0;
                int intTotalBaseRating = await _objSkill.GetTotalBaseRatingAsync().ConfigureAwait(false);
                decimal decSpecCostMultiplier = 1.0m;
                foreach (Improvement objLoopImprovement in _objSkill.CharacterObject.Improvements)
                {
                    if (objLoopImprovement.Minimum <= intTotalBaseRating
                        && (string.IsNullOrEmpty(objLoopImprovement.Condition)
                            || (objLoopImprovement.Condition == "career") == _objSkill.CharacterObject.Created
                            || (objLoopImprovement.Condition == "create") != _objSkill.CharacterObject.Created)
                        && objLoopImprovement.Enabled
                        && objLoopImprovement.ImprovedName == _objSkill.SkillCategory)
                    {
                        switch (objLoopImprovement.ImproveType)
                        {
                            case Improvement.ImprovementType.SkillCategorySpecializationKarmaCost:
                                decExtraSpecCost += objLoopImprovement.Value;
                                break;

                            case Improvement.ImprovementType.SkillCategorySpecializationKarmaCostMultiplier:
                                decSpecCostMultiplier *= objLoopImprovement.Value / 100.0m;
                                break;
                        }
                    }
                }

                if (decSpecCostMultiplier != 1.0m)
                    price = (price * decSpecCostMultiplier + decExtraSpecCost).StandardRound();
                else
                    price += decExtraSpecCost.StandardRound(); //Spec

                string confirmstring = string.Format(GlobalSettings.CultureInfo,
                    await LanguageManager.GetStringAsync("Message_ConfirmKarmaExpenseSkillSpecialization").ConfigureAwait(false), price);

                if (!await CommonFunctions.ConfirmKarmaExpenseAsync(confirmstring).ConfigureAwait(false))
                    return;

                using (ThreadSafeForm<SelectSpec> selectForm =
                       await ThreadSafeForm<SelectSpec>.GetAsync(() => new SelectSpec(_objSkill)).ConfigureAwait(false))
                {
                    if (await selectForm.ShowDialogSafeAsync(_objSkill.CharacterObject).ConfigureAwait(false) != DialogResult.OK)
                        return;
                    await _objSkill.AddSpecialization(selectForm.MyForm.SelectedItem).ConfigureAwait(false);
                }
            }
        }

        private void btnAttribute_Click(object sender, EventArgs e)
        {
            if (cboSelectAttribute != null)
            {
                btnAttribute.Visible = false;
                cboSelectAttribute.Visible = true;
                cboSelectAttribute.DroppedDown = true;
            }
        }

        private async void cboSelectAttribute_Closed(object sender, EventArgs e)
        {
            await DoSelectAttributeClosed().ConfigureAwait(false);
        }

        private async ValueTask DoSelectAttributeClosed(CancellationToken token = default)
        {
            await btnAttribute.DoThreadSafeAsync(x => x.Visible = true, token: token).ConfigureAwait(false);
            await cboSelectAttribute.DoThreadSafeAsync(x => x.Visible = false, token: token).ConfigureAwait(false);
            await SetAttributeActiveAsync(
                await _objSkill.CharacterObject.GetAttributeAsync(
                    (string)await cboSelectAttribute.DoThreadSafeFuncAsync(x => x.SelectedValue, token: token).ConfigureAwait(false),
                    token: token).ConfigureAwait(false), token).ConfigureAwait(false);
            string strText = await cboSelectAttribute.DoThreadSafeFuncAsync(x => x.Text, token: token).ConfigureAwait(false);
            await btnAttribute.DoThreadSafeAsync(x => x.Text = strText, token: token).ConfigureAwait(false);
        }

        private CharacterAttrib AttributeActive
        {
            get => _objAttributeActive;
            set
            {
                if (_objAttributeActive == value)
                    return;
                if (_objAttributeActive != null)
                    _objAttributeActive.PropertyChanged -= Attribute_PropertyChanged;
                _objAttributeActive = value;
                if (_objAttributeActive != null)
                    _objAttributeActive.PropertyChanged += Attribute_PropertyChanged;
                btnAttribute.DoThreadSafe(x => x.Font = _objAttributeActive == _objSkill.AttributeObject
                                              ? _fntNormal
                                              : _fntItalic);
                RefreshPoolTooltipAndDisplay();
                CustomAttributeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private async ValueTask SetAttributeActiveAsync(CharacterAttrib value, CancellationToken token = default)
        {
            if (_objAttributeActive == value)
                return;
            if (_objAttributeActive != null)
                _objAttributeActive.PropertyChanged -= Attribute_PropertyChanged;
            _objAttributeActive = value;
            if (_objAttributeActive != null)
                _objAttributeActive.PropertyChanged += Attribute_PropertyChanged;
            await btnAttribute.DoThreadSafeAsync(x => x.Font = _objAttributeActive == _objSkill.AttributeObject
                                                     ? _fntNormal
                                                     : _fntItalic, token).ConfigureAwait(false);
            await RefreshPoolTooltipAndDisplayAsync(token).ConfigureAwait(false);
            CustomAttributeChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CustomAttributeChanged;

        public bool CustomAttributeSet => AttributeActive != _objSkill.AttributeObject;

        [UsedImplicitly]
        public int NameWidth => lblName.DoThreadSafeFunc(x => x.PreferredWidth + x.Margin.Right) + pnlAttributes.DoThreadSafeFunc(x => x.Margin.Left + x.Width);

        [UsedImplicitly]
        public int NudSkillWidth => nudSkill?.DoThreadSafeFunc(x => x.Visible) == true ? nudSkill.DoThreadSafeFunc(x => x.Width) : 0;

        [UsedImplicitly]
        public async ValueTask ResetSelectAttribute(CancellationToken token = default)
        {
            if (!CustomAttributeSet)
                return;
            if (cboSelectAttribute == null)
                return;
            await cboSelectAttribute.DoThreadSafeAsync(x =>
            {
                x.SelectedValue = _objSkill.AttributeObject.Abbrev;
                x.Visible = false;
            }, token: token).ConfigureAwait(false);
            await SetAttributeActiveAsync(
                await _objSkill.CharacterObject.GetAttributeAsync(
                    (string)await cboSelectAttribute.DoThreadSafeFuncAsync(x => x.SelectedValue, token: token).ConfigureAwait(false), token: token).ConfigureAwait(false), token).ConfigureAwait(false);
            string strText = await cboSelectAttribute.DoThreadSafeFuncAsync(x => x.Text, token: token).ConfigureAwait(false);
            await btnAttribute.DoThreadSafeAsync(x =>
            {
                x.Visible = true;
                x.Text = strText;
            }, token: token).ConfigureAwait(false);
        }

        private async void cmdDelete_Click(object sender, EventArgs e)
        {
            if (!_objSkill.AllowDelete)
                return;
            if (!await CommonFunctions.ConfirmDeleteAsync(await LanguageManager.GetStringAsync(_objSkill.IsExoticSkill ? "Message_DeleteExoticSkill" : "Message_DeleteSkill").ConfigureAwait(false)).ConfigureAwait(false))
                return;
            await _objSkill.CharacterObject.SkillsSection.Skills.RemoveAsync(_objSkill).ConfigureAwait(false);
        }

        private async void tsSkillLabelNotes_Click(object sender, EventArgs e)
        {
            using (ThreadSafeForm<EditNotes> frmItemNotes = await ThreadSafeForm<EditNotes>.GetAsync(() => new EditNotes(_objSkill.Notes, _objSkill.NotesColor)).ConfigureAwait(false))
            {
                if (await frmItemNotes.ShowDialogSafeAsync(_objSkill.CharacterObject).ConfigureAwait(false) != DialogResult.OK)
                    return;
                _objSkill.Notes = frmItemNotes.MyForm.Notes;
            }
        }

        private async void lblName_Click(object sender, EventArgs e)
        {
            CursorWait objCursorWait = await CursorWait.NewAsync(ParentForm).ConfigureAwait(false);
            try
            {
                await CommonFunctions.OpenPdf(_objSkill.Source + ' ' + await _objSkill.DisplayPageAsync(GlobalSettings.Language).ConfigureAwait(false),
                                              _objSkill.CharacterObject).ConfigureAwait(false);
            }
            finally
            {
                await objCursorWait.DisposeAsync().ConfigureAwait(false);
            }
        }

        [UsedImplicitly]
        public void MoveControls(int intNewNameWidth)
        {
            lblName.DoThreadSafe(x => x.MinimumSize = new Size(intNewNameWidth - x.Margin.Right - pnlAttributes.DoThreadSafeFunc(y => y.Margin.Left + y.Width), x.MinimumSize.Height));
        }

        private void UnbindSkillControl()
        {
            _tmrSpecChangeTimer?.Dispose();
            _objSkill.PropertyChanged -= Skill_PropertyChanged;
            if (AttributeActive != null)
                AttributeActive.PropertyChanged -= Attribute_PropertyChanged;

            foreach (Control objControl in Controls)
            {
                objControl.DataBindings.Clear();
            }
        }

        /// <summary>
        /// I'm not super pleased with how this works, but it's functional so w/e.
        /// The goal is for controls to retain the ability to display tooltips even while disabled. IT DOES NOT WORK VERY WELL.
        /// </summary>
        #region ButtonWithToolTip Visibility workaround

        private ButtonWithToolTip _activeButton;

        private ButtonWithToolTip ActiveButton
        {
            get => _activeButton;
            set
            {
                if (value == ActiveButton)
                    return;
                ActiveButton?.ToolTipObject.Hide(this);
                _activeButton = value;
                if (ActiveButton?.Visible == true)
                {
                    ActiveButton.ToolTipObject.Show(ActiveButton.ToolTipText, this);
                }
            }
        }

        private ButtonWithToolTip FindToolTipControl(Point pt)
        {
            return Controls.OfType<ButtonWithToolTip>().FirstOrDefault(c => c.Bounds.Contains(pt));
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            ActiveButton = FindToolTipControl(e.Location);
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            ActiveButton = null;
        }

        #endregion ButtonWithToolTip Visibility workaround

        private void SkillControl_DpiChangedAfterParent(object sender, EventArgs e)
        {
            AdjustForDpi();
        }

        private void AdjustForDpi()
        {
            using (Graphics g = CreateGraphics())
            {
                pnlAttributes.MinimumSize = new Size((int)(40 * g.DpiX / 96.0f), 0);
                if (lblCareerRating != null)
                    lblCareerRating.MinimumSize = new Size((int)(25 * g.DpiX / 96.0f), 0);
                lblModifiedRating.MinimumSize = new Size((int)(50 * g.DpiX / 96.0f), 0);
            }
        }

        /// <summary>
        /// Refreshes the Tooltip and Displayed Dice Pool. Can be used in another Thread
        /// </summary>
        private void RefreshPoolTooltipAndDisplay()
        {
            string backgroundCalcPool = _objSkill.DisplayOtherAttribute(AttributeActive.Abbrev);
            string backgroundCalcTooltip = _objSkill.CompileDicepoolTooltip(AttributeActive.Abbrev);
            lblModifiedRating.DoThreadSafe(x =>
            {
                x.Text = backgroundCalcPool;
                x.ToolTipText = backgroundCalcTooltip;
            });
        }

        /// <summary>
        /// Refreshes the Tooltip and Displayed Dice Pool. Can be used in another Thread
        /// </summary>
        private async Task RefreshPoolTooltipAndDisplayAsync(CancellationToken token = default)
        {
            string backgroundCalcPool = await _objSkill.DisplayOtherAttributeAsync(AttributeActive.Abbrev, token).ConfigureAwait(false);
            string backgroundCalcTooltip = await _objSkill.CompileDicepoolTooltipAsync(AttributeActive.Abbrev, token: token).ConfigureAwait(false);
            await lblModifiedRating.DoThreadSafeAsync(x =>
            {
                x.Text = backgroundCalcPool;
                x.ToolTipText = backgroundCalcTooltip;
            }, token: token).ConfigureAwait(false);
        }

        // Hacky solutions to data binding causing cursor to reset whenever the user is typing something in: have text changes start a timer, and have a 1s delay in the timer update fire the text update
        private void cboSpec_TextChanged(object sender, EventArgs e)
        {
            if (_tmrSpecChangeTimer == null)
                return;
            if (_tmrSpecChangeTimer.Enabled)
                _tmrSpecChangeTimer.Stop();
            if (_blnUpdatingSpec)
                return;
            _tmrSpecChangeTimer.Start();
        }

        private async void SpecChangeTimer_Tick(object sender, EventArgs e)
        {
            _tmrSpecChangeTimer.Stop();
            _blnUpdatingSpec = true;
            try
            {
                string strSpec = await cboSpec.DoThreadSafeFuncAsync(x => x.Text).ConfigureAwait(false);
                await _objSkill.SetTopMostDisplaySpecializationAsync(strSpec).ConfigureAwait(false);
            }
            finally
            {
                _blnUpdatingSpec = false;
            }
        }
    }
}
