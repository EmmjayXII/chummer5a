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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chummer.Annotations;
using Chummer.Backend.Attributes;
using Chummer.Properties;

namespace Chummer.UI.Attributes
{
    public partial class AttributeControl : UserControl
    {
        public event EventHandlerExtensions.SafeAsyncEventHandler ValueChanged;

        private readonly string _strAttributeName;
        private int _oldBase;
        private int _oldKarma;
        private readonly Character _objCharacter;

        private readonly NumericUpDownEx nudKarma;
        private readonly NumericUpDownEx nudBase;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly ButtonWithToolTip cmdBurnEdge;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly ButtonWithToolTip cmdImproveATT;

        private bool _blnLoading = true;

        private readonly CancellationToken _objMyToken;

        public AttributeControl(CharacterAttrib attribute, CancellationToken objMyToken = default)
        {
            if (attribute == null)
                return;
            _objMyToken = objMyToken;
            _strAttributeName = attribute.Abbrev;
            _objCharacter = attribute.CharacterObject;

            InitializeComponent();

            Disposed += (sender, args) => UnbindAttributeControl();

            SuspendLayout();
            try
            {
                // To make sure that the initial load formats the name column properly, we need to set the attribute name in the constructor
                lblName.Text = attribute.DisplayNameFormatted;
                //Display
                if (_objCharacter.Created)
                {
                    cmdImproveATT = new ButtonWithToolTip
                    {
                        Anchor = AnchorStyles.Right,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        Padding = new Padding(1),
                        MinimumSize = new Size(24, 24),
                        Name = "cmdImproveATT",
                        UseVisualStyleBackColor = true
                    };
                    cmdImproveATT.BatchSetImages(Resources.add_16, Resources.add_20, Resources.add_24, Resources.add_32,
                        Resources.add_48, Resources.add_64);
                    cmdImproveATT.Click += cmdImproveATT_Click;
                    flpRight.Controls.Add(cmdImproveATT);
                    if (AttributeName == "EDG")
                    {
                        cmdBurnEdge = new ButtonWithToolTip
                        {
                            Anchor = AnchorStyles.Right,
                            AutoSize = true,
                            AutoSizeMode = AutoSizeMode.GrowAndShrink,
                            Padding = new Padding(1),
                            MinimumSize = new Size(24, 24),
                            Name = "cmdBurnEdge",
                            ToolTipText = LanguageManager.GetString("Tip_CommonBurnEdge", token: objMyToken),
                            UseVisualStyleBackColor = true
                        };
                        cmdBurnEdge.BatchSetImages(Resources.fire_16, Resources.fire_20, Resources.fire_24,
                            Resources.fire_32,
                            Resources.fire_48, Resources.fire_64);
                        cmdBurnEdge.Click += cmdBurnEdge_Click;
                        flpRight.Controls.Add(cmdBurnEdge);
                    }
                }
                else
                {
                    nudKarma = new NumericUpDownEx
                    {
                        Anchor = AnchorStyles.Right,
                        AutoSize = true,
                        InterceptMouseWheel = GlobalSettings.InterceptMode,
                        Margin = new Padding(3, 0, 3, 0),
                        Maximum = DecimalExtensions.NinetyNine,
                        MinimumSize = new Size(35, 0),
                        Name = "nudKarma"
                    };
                    nudKarma.BeforeValueIncrement += nudKarma_BeforeValueIncrement;
                    nudKarma.ValueChanged += nudKarma_ValueChanged;
                    nudBase = new NumericUpDownEx
                    {
                        Anchor = AnchorStyles.Right,
                        AutoSize = true,
                        InterceptMouseWheel = GlobalSettings.InterceptMode,
                        Margin = new Padding(3, 0, 3, 0),
                        Maximum = DecimalExtensions.NinetyNine,
                        MinimumSize = new Size(35, 0),
                        Name = "nudBase"
                    };
                    nudBase.BeforeValueIncrement += nudBase_BeforeValueIncrement;
                    nudBase.ValueChanged += nudBase_ValueChanged;

                    flpRight.Controls.Add(nudKarma);
                    flpRight.Controls.Add(nudBase);
                }

                this.UpdateLightDarkMode(token: objMyToken);
                this.TranslateWinForm(token: objMyToken);
            }
            finally
            {
                ResumeLayout();
            }
        }

        private async Task OnAttributePropertyChanged(object sender, PropertyChangedEventArgs e,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            switch (e.PropertyName)
            {
                case nameof(CharacterAttrib.DisplayNameFormatted):
                    string strName = await AttributeObject.GetDisplayNameFormattedAsync(token).ConfigureAwait(false);
                    await lblName.DoThreadSafeAsync(x => x.Text = strName, token).ConfigureAwait(false);
                    break;
                case nameof(CharacterAttrib.DisplayValue):
                    string strValue = await AttributeObject.GetDisplayValueAsync(token).ConfigureAwait(false);
                    await lblValue.DoThreadSafeAsync(x => x.Text = strValue, token).ConfigureAwait(false);
                    break;
                case nameof(CharacterAttrib.AugmentedMetatypeLimits):
                    string strAugmentedMetatypeLimits = await AttributeObject.GetAugmentedMetatypeLimitsAsync(token).ConfigureAwait(false);
                    await lblLimits.DoThreadSafeAsync(x => x.Text = strAugmentedMetatypeLimits, token).ConfigureAwait(false);
                    break;
                case nameof(CharacterAttrib.ToolTip):
                    string strToolTip = await AttributeObject.GetToolTipAsync(token).ConfigureAwait(false);
                    await lblValue.DoThreadSafeAsync(x => x.ToolTipText = strToolTip, token).ConfigureAwait(false);
                    break;
                case nameof(CharacterAttrib.UpgradeToolTip):
                    if (await _objCharacter.GetCreatedAsync(token).ConfigureAwait(false))
                    {
                        string strUpgradeToolTip =
                            await AttributeObject.GetUpgradeToolTipAsync(token).ConfigureAwait(false);
                        await cmdImproveATT.DoThreadSafeAsync(x => x.ToolTipText = strUpgradeToolTip, token)
                            .ConfigureAwait(false);
                    }
                    break;
                case nameof(CharacterAttrib.CanUpgradeCareer):
                    if (await _objCharacter.GetCreatedAsync(token).ConfigureAwait(false))
                    {
                        bool blnCanUpgradeCareer =
                            await AttributeObject.GetCanUpgradeCareerAsync(token).ConfigureAwait(false);
                        await cmdImproveATT.DoThreadSafeAsync(x => x.Enabled = blnCanUpgradeCareer, token)
                            .ConfigureAwait(false);
                    }
                    break;
                case nameof(CharacterAttrib.PriorityMaximum):
                    if (!await _objCharacter.GetCreatedAsync(token).ConfigureAwait(false))
                    {
                        int intPriorityMaximum =
                            await AttributeObject.GetPriorityMaximumAsync(token).ConfigureAwait(false);
                        await nudBase.DoThreadSafeAsync(x => x.Maximum = intPriorityMaximum, token)
                            .ConfigureAwait(false);
                    }
                    break;
                case nameof(CharacterAttrib.BaseUnlocked):
                    if (!await _objCharacter.GetCreatedAsync(token).ConfigureAwait(false))
                    {
                        bool blnBaseUnlocked =
                            await AttributeObject.GetBaseUnlockedAsync(token).ConfigureAwait(false);
                        await nudBase.DoThreadSafeAsync(x => x.Enabled = blnBaseUnlocked, token)
                            .ConfigureAwait(false);
                    }
                    break;
                case nameof(CharacterAttrib.KarmaMaximum):
                    if (!await _objCharacter.GetCreatedAsync(token).ConfigureAwait(false))
                    {
                        int intKarmaMaximum =
                            await AttributeObject.GetKarmaMaximumAsync(token).ConfigureAwait(false);
                        await nudKarma.DoThreadSafeAsync(x => x.Maximum = intKarmaMaximum, token)
                            .ConfigureAwait(false);
                    }
                    break;
                case nameof(CharacterAttrib.Base):
                    if (_intChangingBase == 0 && !await _objCharacter.GetCreatedAsync(token).ConfigureAwait(false))
                    {
                        int intBase =
                            await AttributeObject.GetBaseAsync(token).ConfigureAwait(false);
                        await nudBase.DoThreadSafeAsync(x => x.Value = intBase, token)
                            .ConfigureAwait(false);
                    }
                    break;
                case nameof(CharacterAttrib.Karma):
                    if (_intChangingKarma == 0 && !await _objCharacter.GetCreatedAsync(token).ConfigureAwait(false))
                    {
                        int intKarma =
                            await AttributeObject.GetBaseAsync(token).ConfigureAwait(false);
                        await nudKarma.DoThreadSafeAsync(x => x.Value = intKarma, token)
                            .ConfigureAwait(false);
                    }
                    break;
            }
        }

        private async void AttributeControl_Load(object sender, EventArgs e)
        {
            try
            {
                await this.DoThreadSafeAsync(x => x.SuspendLayout(), _objMyToken).ConfigureAwait(false);
                try
                {
                    CharacterAttrib objAttrib = await GetAttributeObjectAsync(_objMyToken).ConfigureAwait(false);
                    IAsyncDisposable objLocker = await objAttrib.LockObject.EnterUpgradeableReadLockAsync(_objMyToken)
                        .ConfigureAwait(false);
                    try
                    {
                        _objMyToken.ThrowIfCancellationRequested();
                        string strName = await objAttrib.GetDisplayNameFormattedAsync(_objMyToken).ConfigureAwait(false);
                        await lblName.DoThreadSafeAsync(x => x.Text = strName, _objMyToken).ConfigureAwait(false);
                        string strValue = await objAttrib.GetDisplayValueAsync(_objMyToken).ConfigureAwait(false);
                        await lblValue.DoThreadSafeAsync(x => x.Text = strValue, _objMyToken).ConfigureAwait(false);
                        string strAugmentedMetatypeLimits =
                            await objAttrib.GetAugmentedMetatypeLimitsAsync(_objMyToken).ConfigureAwait(false);
                        await lblLimits.DoThreadSafeAsync(x => x.Text = strAugmentedMetatypeLimits, _objMyToken)
                            .ConfigureAwait(false);
                        string strToolTip = await objAttrib.GetToolTipAsync(_objMyToken).ConfigureAwait(false);
                        await lblValue.DoThreadSafeAsync(x => x.ToolTipText = strToolTip, _objMyToken).ConfigureAwait(false);
                        if (await _objCharacter.GetCreatedAsync(_objMyToken).ConfigureAwait(false))
                        {
                            string strUpgradeToolTip =
                                await objAttrib.GetUpgradeToolTipAsync(_objMyToken).ConfigureAwait(false);
                            await cmdImproveATT.DoThreadSafeAsync(x => x.ToolTipText = strUpgradeToolTip, _objMyToken)
                                .ConfigureAwait(false);
                            bool blnCanUpgradeCareer =
                                await objAttrib.GetCanUpgradeCareerAsync(_objMyToken).ConfigureAwait(false);
                            await cmdImproveATT.DoThreadSafeAsync(x => x.Enabled = blnCanUpgradeCareer, _objMyToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            int intKarmaMaximum;
                            IAsyncDisposable objLocker2 = await objAttrib.LockObject.EnterUpgradeableReadLockAsync(_objMyToken)
                                .ConfigureAwait(false);
                            try
                            {
                                _objMyToken.ThrowIfCancellationRequested();
                                while (await objAttrib.GetBaseAsync(_objMyToken).ConfigureAwait(false) > 0 &&
                                       await objAttrib.GetKarmaMaximumAsync(_objMyToken).ConfigureAwait(false) < 0)
                                {
                                    await objAttrib.ModifyBaseAsync(-1, _objMyToken).ConfigureAwait(false);
                                }

                                // Very rough fix for when Karma values somehow exceed KarmaMaximum after loading in. This shouldn't happen in the first place, but this ad-hoc patch will help fix crashes.
                                intKarmaMaximum = await objAttrib.GetKarmaMaximumAsync(_objMyToken).ConfigureAwait(false);
                                if (await objAttrib.GetKarmaAsync(_objMyToken).ConfigureAwait(false) > intKarmaMaximum)
                                    await objAttrib.SetKarmaAsync(intKarmaMaximum, _objMyToken).ConfigureAwait(false);
                            }
                            finally
                            {
                                await objLocker2.DisposeAsync().ConfigureAwait(false);
                            }

                            int intPriorityMaximum =
                                await objAttrib.GetPriorityMaximumAsync(_objMyToken).ConfigureAwait(false);
                            await nudBase.DoThreadSafeAsync(x => x.Maximum = intPriorityMaximum, _objMyToken)
                                .ConfigureAwait(false);
                            bool blnBaseUnlocked =
                                await objAttrib.GetBaseUnlockedAsync(_objMyToken).ConfigureAwait(false);
                            await nudBase.DoThreadSafeAsync(x => x.Enabled = blnBaseUnlocked, _objMyToken)
                                .ConfigureAwait(false);
                            await nudBase.RegisterOneWayAsyncDataBindingAsync((x, y) => x.Visible = y, _objCharacter,
                                nameof(Character.EffectiveBuildMethodUsesPriorityTables),
                                x => x.GetEffectiveBuildMethodUsesPriorityTablesAsync(_objMyToken), token: _objMyToken).ConfigureAwait(false);
                            await nudKarma.DoThreadSafeAsync(x => x.Maximum = intKarmaMaximum, _objMyToken)
                                .ConfigureAwait(false);
                            int intBase =
                                await objAttrib.GetBaseAsync(_objMyToken).ConfigureAwait(false);
                            await nudBase.DoThreadSafeAsync(x => x.Value = intBase, _objMyToken)
                                .ConfigureAwait(false);
                            int intKarma =
                                await objAttrib.GetKarmaAsync(_objMyToken).ConfigureAwait(false);
                            await nudKarma.DoThreadSafeAsync(x => x.Value = intKarma, _objMyToken)
                                .ConfigureAwait(false);
                        }

                        _objCharacter.AttributeSection.RegisterAsyncPropertyChangedForActiveAttribute(AttributeName,
                            OnAttributePropertyChanged);
                    }
                    finally
                    {
                        await objLocker.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await this.DoThreadSafeAsync(x => x.ResumeLayout(true), _objMyToken).ConfigureAwait(false);
                    Interlocked.CompareExchange(ref _intChangingBase, 0, 1);
                    Interlocked.CompareExchange(ref _intChangingKarma, 0, 1);
                    _blnLoading = false;
                }
            }
            catch (OperationCanceledException)
            {
                // swalllow this
            }
        }

        public void UpdateWidths(int intNameWidth, int intNudKarmaWidth, int intValueWidth, int intLimitsWidth, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            tlpMain.DoThreadSafe(x => x.SuspendLayout(), token);
            try
            {
                if (intNameWidth >= 0)
                {
                    lblName.DoThreadSafe(x =>
                    {
                        if (x.MinimumSize.Width > intNameWidth)
                            x.MinimumSize = new Size(intNameWidth, x.MinimumSize.Height);
                        if (x.MaximumSize.Width != intNameWidth)
                            x.MaximumSize = new Size(intNameWidth, x.MinimumSize.Height);
                        if (x.MinimumSize.Width < intNameWidth)
                            x.MinimumSize = new Size(intNameWidth, x.MinimumSize.Height);
                    }, token);
                }

                if (intNudKarmaWidth >= 0 && nudBase?.DoThreadSafeFunc(x => x.Visible, token) == true)
                {
                    nudKarma?.DoThreadSafe(x =>
                    {
                        if (x.Visible)
                        {
                            x.Margin = new Padding(
                                x.Margin.Right + Math.Max(intNudKarmaWidth - x.Width, 0),
                                x.Margin.Top,
                                x.Margin.Right,
                                x.Margin.Bottom);
                        }
                    }, token);
                }

                if (intValueWidth >= 0)
                {
                    lblValue.DoThreadSafe(x =>
                    {
                        if (x.MinimumSize.Width > intValueWidth)
                            x.MinimumSize = new Size(intValueWidth, x.MinimumSize.Height);
                        if (x.MaximumSize.Width != intValueWidth)
                            x.MaximumSize = new Size(intValueWidth, x.MinimumSize.Height);
                        if (x.MinimumSize.Width < intValueWidth)
                            x.MinimumSize = new Size(intValueWidth, x.MinimumSize.Height);
                    }, token);
                }

                if (intLimitsWidth >= 0)
                {
                    lblLimits.DoThreadSafe(x =>
                    {
                        if (x.MinimumSize.Width > intLimitsWidth)
                            x.MinimumSize = new Size(intLimitsWidth, x.MinimumSize.Height);
                        if (x.MaximumSize.Width != intLimitsWidth)
                            x.MaximumSize = new Size(intLimitsWidth, x.MinimumSize.Height);
                        if (x.MinimumSize.Width < intLimitsWidth)
                            x.MinimumSize = new Size(intLimitsWidth, x.MinimumSize.Height);
                    }, token);
                }
            }
            finally
            {
                tlpMain.DoThreadSafe(x => x.ResumeLayout(), token);
            }
        }

        private void UnbindAttributeControl()
        {
            _objCharacter.AttributeSection.DeregisterAsyncPropertyChangedForActiveAttribute(AttributeName, OnAttributePropertyChanged);
        }

        private async void cmdImproveATT_Click(object sender, EventArgs e)
        {
            try
            {
                CursorWait objCursorWait = await CursorWait.NewAsync(this, token: _objMyToken).ConfigureAwait(false);
                try
                {
                    CharacterAttrib objAttribute = await GetAttributeObjectAsync(_objMyToken).ConfigureAwait(false);
                    IAsyncDisposable objLocker = await objAttribute.LockObject
                        .EnterUpgradeableReadLockAsync(_objMyToken).ConfigureAwait(false);
                    try
                    {
                        _objMyToken.ThrowIfCancellationRequested();
                        int intUpgradeKarmaCost =
                            await objAttribute.GetUpgradeKarmaCostAsync(_objMyToken).ConfigureAwait(false);

                        if (intUpgradeKarmaCost == -1) return; //TODO: more descriptive
                        if (intUpgradeKarmaCost > await _objCharacter.GetKarmaAsync(_objMyToken).ConfigureAwait(false))
                        {
                            Program.ShowScrollableMessageBox(
                                await LanguageManager.GetStringAsync("Message_NotEnoughKarma", token: _objMyToken)
                                    .ConfigureAwait(false),
                                await LanguageManager.GetStringAsync("MessageTitle_NotEnoughKarma", token: _objMyToken)
                                    .ConfigureAwait(false),
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        string strConfirm = string.Format(GlobalSettings.CultureInfo,
                            await LanguageManager
                                .GetStringAsync("Message_ConfirmKarmaExpense", token: _objMyToken)
                                .ConfigureAwait(false),
                            await objAttribute.GetDisplayNameFormattedAsync(_objMyToken)
                                .ConfigureAwait(false),
                            await objAttribute.GetValueAsync(_objMyToken).ConfigureAwait(false) + 1,
                            intUpgradeKarmaCost);
                        if (!await CommonFunctions.ConfirmKarmaExpenseAsync(strConfirm, _objMyToken)
                                .ConfigureAwait(false))
                            return;

                        await objAttribute.Upgrade(token: _objMyToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        await objLocker.DisposeAsync().ConfigureAwait(false);
                    }

                    if (ValueChanged != null)
                        await ValueChanged.Invoke(this, e, _objMyToken).ConfigureAwait(false);
                }
                finally
                {
                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // swallow this
            }
        }

        private int _intChangingBase = 1;
        private int _intChangingKarma = 1;

        private async void nudBase_ValueChanged(object sender, EventArgs e)
        {
            if (_blnLoading)
                return;
            try
            {
                int intValue = await ((NumericUpDownEx)sender).DoThreadSafeFuncAsync(x => x.ValueAsInt, _objMyToken)
                    .ConfigureAwait(false);
                if (Interlocked.Exchange(ref _oldBase, intValue) == intValue)
                    return;
                CursorWait objCursorWait = await CursorWait.NewAsync(this, token: _objMyToken).ConfigureAwait(false);
                Interlocked.Increment(ref _intChangingBase);
                try
                {
                    CharacterAttrib objAttribute = await GetAttributeObjectAsync(_objMyToken).ConfigureAwait(false);
                    IAsyncDisposable objLocker = await objAttribute.LockObject
                        .EnterUpgradeableReadLockAsync(_objMyToken).ConfigureAwait(false);
                    try
                    {
                        _objMyToken.ThrowIfCancellationRequested();
                        if (!await CanBeMetatypeMax(
                                    Math.Max(
                                        await nudKarma.DoThreadSafeFuncAsync(x => x.ValueAsInt, token: _objMyToken)
                                            .ConfigureAwait(false) +
                                        await objAttribute.GetFreeBaseAsync(_objMyToken).ConfigureAwait(false)
                                        + await objAttribute.GetRawMinimumAsync(_objMyToken).ConfigureAwait(false) +
                                        await objAttribute.GetAttributeValueModifiersAsync(_objMyToken)
                                            .ConfigureAwait(false),
                                        await objAttribute.GetTotalMinimumAsync(_objMyToken).ConfigureAwait(false)) +
                                    intValue, _objMyToken)
                                .ConfigureAwait(false))
                        {
                            await nudBase.DoThreadSafeAsync(x =>
                            {
                                decimal newValue = Math.Max(x.Value - 1, 0);
                                if (newValue > x.Maximum)
                                {
                                    newValue = x.Maximum;
                                }

                                if (newValue < x.Minimum)
                                {
                                    newValue = x.Minimum;
                                }

                                x.Value = newValue;
                            }, token: _objMyToken).ConfigureAwait(false);
                            return;
                        }

                        await objAttribute.SetBaseAsync(intValue, _objMyToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        await objLocker.DisposeAsync().ConfigureAwait(false);
                    }

                    if (ValueChanged != null)
                        await ValueChanged.Invoke(this, e, _objMyToken).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Decrement(ref _intChangingBase);
                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // swallow this
            }
        }

        private async void nudKarma_ValueChanged(object sender, EventArgs e)
        {
            if (_blnLoading)
                return;
            try
            {
                int intValue = await ((NumericUpDownEx)sender).DoThreadSafeFuncAsync(x => x.ValueAsInt, _objMyToken)
                    .ConfigureAwait(false);
                int intOldKarma = Interlocked.Exchange(ref _oldKarma, intValue);
                if (intOldKarma == intValue)
                    return;
                CursorWait objCursorWait = await CursorWait.NewAsync(this, token: _objMyToken).ConfigureAwait(false);
                Interlocked.Increment(ref _intChangingKarma);
                try
                {
                    CharacterAttrib objAttribute = await GetAttributeObjectAsync(_objMyToken).ConfigureAwait(false);
                    IAsyncDisposable objLocker = await objAttribute.LockObject
                        .EnterUpgradeableReadLockAsync(_objMyToken).ConfigureAwait(false);
                    try
                    {
                        _objMyToken.ThrowIfCancellationRequested();
                        if (!await CanBeMetatypeMax(
                                    Math.Max(
                                        await nudBase.DoThreadSafeFuncAsync(x => x.ValueAsInt, token: _objMyToken)
                                            .ConfigureAwait(false) +
                                        await objAttribute.GetFreeBaseAsync(_objMyToken).ConfigureAwait(false)
                                        + await objAttribute.GetRawMinimumAsync(_objMyToken).ConfigureAwait(false) +
                                        await objAttribute.GetAttributeValueModifiersAsync(_objMyToken)
                                            .ConfigureAwait(false),
                                        await objAttribute.GetTotalMinimumAsync(_objMyToken).ConfigureAwait(false)) +
                                    intValue, _objMyToken)
                                .ConfigureAwait(false))
                        {
                            // It's possible that the attribute maximum was reduced by an improvement, so confirm the appropriate value to bounce up/down to.
                            int intKarmaMaximum =
                                await objAttribute.GetKarmaMaximumAsync(_objMyToken).ConfigureAwait(false);
                            if (intOldKarma > intKarmaMaximum)
                            {
                                if (Interlocked.CompareExchange(ref _oldKarma, intKarmaMaximum - 1, intValue) ==
                                    intValue)
                                    intValue = intKarmaMaximum - 1;
                                intOldKarma = intKarmaMaximum - 1;
                            }

                            if (intOldKarma < 0)
                            {
                                Interlocked.CompareExchange(ref _oldKarma, 0, intValue);
                                int intOldKarmaLocal = intOldKarma;
                                await nudBase.DoThreadSafeAsync(x =>
                                {
                                    decimal newValue = Math.Max(x.Value - intOldKarmaLocal, 0);
                                    if (newValue > x.Maximum)
                                    {
                                        newValue = x.Maximum;
                                    }

                                    if (newValue < x.Minimum)
                                    {
                                        newValue = x.Minimum;
                                    }

                                    x.Value = newValue;
                                }, token: _objMyToken).ConfigureAwait(false);
                                intOldKarma = 0;
                            }

                            await nudKarma.DoThreadSafeAsync(x => x.Value = intOldKarma, token: _objMyToken)
                                .ConfigureAwait(false);
                            return;
                        }

                        await objAttribute.SetKarmaAsync(intValue, _objMyToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        await objLocker.DisposeAsync().ConfigureAwait(false);
                    }

                    if (ValueChanged != null)
                        await ValueChanged.Invoke(this, e, _objMyToken).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Decrement(ref _intChangingKarma);
                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // swallow this
            }
        }

        /// <summary>
        /// Is the attribute allowed to reach the Metatype Maximum?
        /// SR5 66: Characters at character creation may only have 1 Mental or Physical
        /// attribute at their natural maximum limit; the special attributes of Magic, Edge,
        /// and Resonance are not included in this limitation.
        /// </summary>
        private async Task<bool> CanBeMetatypeMax(int intValue, CancellationToken token = default)
        {
            CharacterAttrib objAttribute = await GetAttributeObjectAsync(token).ConfigureAwait(false);
            IAsyncDisposable objLocker = await objAttribute.LockObject.EnterUpgradeableReadLockAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                int intTotalMaximum = await objAttribute.GetTotalMaximumAsync(token).ConfigureAwait(false);
                if (intValue < intTotalMaximum || intTotalMaximum == 0)
                    return true;

                if (await _objCharacter.AttributeSection.CanRaiseAttributeToMetatypeMax(objAttribute, token)
                        .ConfigureAwait(false))
                    return true;

                Program.ShowScrollableMessageBox(
                    string.Format(GlobalSettings.CultureInfo,
                        await LanguageManager.GetStringAsync("Message_AttributeMaximum", token: token)
                            .ConfigureAwait(false),
                        _objCharacter.Settings.MaxNumberMaxAttributesCreate),
                    await LanguageManager.GetStringAsync("MessageTitle_Attribute", token: token).ConfigureAwait(false),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }
            finally
            {
                await objLocker.DisposeAsync().ConfigureAwait(false);
            }
        }

        public string AttributeName => _strAttributeName;

        public CharacterAttrib AttributeObject => _objCharacter.AttributeSection.GetAttributeByName(AttributeName);

        public Task<CharacterAttrib> GetAttributeObjectAsync(CancellationToken token = default) =>
            _objCharacter.AttributeSection.GetAttributeByNameAsync(AttributeName, token);

        [UsedImplicitly]
        public int NameWidth => lblName.DoThreadSafeFunc(x => x.PreferredWidth);

        [UsedImplicitly]
        public Task<int> GetNameWidthAsync(CancellationToken token = default) => lblName.DoThreadSafeFuncAsync(x => x.PreferredWidth, token);

        private async void cmdBurnEdge_Click(object sender, EventArgs e)
        {
            try
            {
                CursorWait objCursorWait = await CursorWait.NewAsync(this, token: _objMyToken).ConfigureAwait(false);
                try
                {
                    // Edge cannot go below 1.
                    CharacterAttrib objAttribute = await GetAttributeObjectAsync(_objMyToken).ConfigureAwait(false);
                    IAsyncDisposable objLocker = await objAttribute.LockObject
                        .EnterUpgradeableReadLockAsync(_objMyToken).ConfigureAwait(false);
                    try
                    {
                        _objMyToken.ThrowIfCancellationRequested();
                        if (await objAttribute.GetValueAsync(_objMyToken).ConfigureAwait(false) <= 0)
                        {
                            Program.ShowScrollableMessageBox(
                                await LanguageManager.GetStringAsync("Message_CannotBurnEdge", token: _objMyToken)
                                    .ConfigureAwait(false),
                                await LanguageManager.GetStringAsync("MessageTitle_CannotBurnEdge", token: _objMyToken)
                                    .ConfigureAwait(false),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                            return;
                        }

                        // Verify that the user wants to Burn a point of Edge.
                        if (Program.ShowScrollableMessageBox(
                                await LanguageManager.GetStringAsync("Message_BurnEdge", token: _objMyToken)
                                    .ConfigureAwait(false),
                                await LanguageManager.GetStringAsync("MessageTitle_BurnEdge", token: _objMyToken)
                                    .ConfigureAwait(false),
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.No)
                            return;

                        await objAttribute.Degrade(1, _objMyToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        await objLocker.DisposeAsync().ConfigureAwait(false);
                    }

                    if (ValueChanged != null)
                        await ValueChanged.Invoke(this, e, _objMyToken).ConfigureAwait(false);
                }
                finally
                {
                    await objCursorWait.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // swallow this
            }
        }

        private void nudBase_BeforeValueIncrement(object sender, CancelEventArgs e)
        {
            if (_blnLoading)
                return;
            if (nudBase.Value + Math.Max(nudKarma.Value, 0) != AttributeObject.TotalMaximum || nudKarma.Value == nudKarma.Minimum)
                return;
            if (nudKarma.Value - nudBase.Increment >= 0)
            {
                nudKarma.Value -= nudBase.Increment;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void nudKarma_BeforeValueIncrement(object sender, CancelEventArgs e)
        {
            if (_blnLoading)
                return;
            if (nudBase.Value + nudKarma.Value != AttributeObject.TotalMaximum || nudBase.Value == nudBase.Minimum)
                return;
            if (nudBase.Value - nudKarma.Increment >= 0)
            {
                nudBase.Value -= nudKarma.Increment;
            }
            else
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// I'm not super pleased with how this works, but it's functional so w/e.
        /// The goal is for controls to retain the ability to display tooltips even while disabled. IT DOES NOT WORK VERY WELL.
        /// </summary>

        #region ButtonWithToolTip Visibility workaround

        private ButtonWithToolTip _activeButton;

        protected ButtonWithToolTip ActiveButton
        {
            get => _activeButton;
            set
            {
                ButtonWithToolTip objOldValue = Interlocked.Exchange(ref _activeButton, value);
                if (objOldValue == value)
                    return;
                objOldValue?.ToolTipObject.Hide(this);
                if (value?.Visible == true)
                {
                    value.ToolTipObject.Show(value.ToolTipText, this);
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
    }
}
