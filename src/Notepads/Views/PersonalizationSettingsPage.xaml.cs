﻿namespace Notepads.Views
{
    using Notepads.Services;
    using Notepads.Settings;
    using Notepads.Utilities;
    using Windows.System.Power;
    using Windows.UI;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Media;

    public sealed partial class PersonalizationSettingsPage : Page
    {
        private readonly UISettings UISettings = new UISettings();

        public PersonalizationSettingsPage()
        {
            InitializeComponent();

            switch (ThemeSettingsService.ThemeMode)
            {
                case ElementTheme.Default:
                    ThemeModeDefaultButton.IsChecked = true;
                    break;
                case ElementTheme.Light:
                    ThemeModeLightButton.IsChecked = true;
                    break;
                case ElementTheme.Dark:
                    ThemeModeDarkButton.IsChecked = true;
                    break;
            }

            AccentColorToggle.IsOn = ThemeSettingsService.UseWindowsAccentColor;
            AccentColorPicker.IsEnabled = !ThemeSettingsService.UseWindowsAccentColor;
            BackgroundTintOpacitySlider.Value = ThemeSettingsService.AppBackgroundPanelTintOpacity * 100;
            AccentColorPicker.Color = ThemeSettingsService.AppAccentColor;

            if (App.IsGameBarWidget)
            {
                // Game Bar widgets do not support transparency, disable this setting
                BackgroundTintOpacityTitle.Visibility = Visibility.Collapsed;
                BackgroundTintOpacityControls.Visibility = Visibility.Collapsed;
            }
            else
            {
                BackgroundTintOpacitySlider.IsEnabled = UISettings.AdvancedEffectsEnabled &&
                                                        PowerManager.EnergySaverStatus != EnergySaverStatus.On;
            }

            Loaded += PersonalizationSettings_Loaded;
            Unloaded += PersonalizationSettings_Unloaded;
        }

        private async void ThemeSettingsService_OnAccentColorChanged(object sender, Color color)
        {
            await ThreadUtility.CallOnUIThreadAsync(Dispatcher, () =>
            {
                BackgroundTintOpacitySlider.Foreground = new SolidColorBrush(color);
                AccentColorPicker.ColorChanged -= AccentColorPicker_OnColorChanged;
                AccentColorPicker.Color = color;
                AccentColorPicker.ColorChanged += AccentColorPicker_OnColorChanged;
            });
        }

        private void PersonalizationSettings_Loaded(object sender, RoutedEventArgs e)
        {
            ThemeModeDefaultButton.Checked += ThemeRadioButton_OnChecked;
            ThemeModeLightButton.Checked += ThemeRadioButton_OnChecked;
            ThemeModeDarkButton.Checked += ThemeRadioButton_OnChecked;
            BackgroundTintOpacitySlider.ValueChanged += BackgroundTintOpacitySlider_OnValueChanged;
            AccentColorToggle.Toggled += WindowsAccentColorToggle_OnToggled;
            AccentColorPicker.ColorChanged += AccentColorPicker_OnColorChanged;
            ThemeSettingsService.OnAccentColorChanged += ThemeSettingsService_OnAccentColorChanged;
            if (!App.IsGameBarWidget)
            {
                UISettings.AdvancedEffectsEnabledChanged += UISettings_AdvancedEffectsEnabledChanged;
                PowerManager.EnergySaverStatusChanged += PowerManager_EnergySaverStatusChanged;
            }
        }

        private void PersonalizationSettings_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeModeDefaultButton.Checked -= ThemeRadioButton_OnChecked;
            ThemeModeLightButton.Checked -= ThemeRadioButton_OnChecked;
            ThemeModeDarkButton.Checked -= ThemeRadioButton_OnChecked;
            BackgroundTintOpacitySlider.ValueChanged -= BackgroundTintOpacitySlider_OnValueChanged;
            AccentColorToggle.Toggled -= WindowsAccentColorToggle_OnToggled;
            AccentColorPicker.ColorChanged -= AccentColorPicker_OnColorChanged;
            ThemeSettingsService.OnAccentColorChanged -= ThemeSettingsService_OnAccentColorChanged;
            if (!App.IsGameBarWidget)
            {
                UISettings.AdvancedEffectsEnabledChanged -= UISettings_AdvancedEffectsEnabledChanged;
                PowerManager.EnergySaverStatusChanged -= PowerManager_EnergySaverStatusChanged;
            }
        }

        private async void PowerManager_EnergySaverStatusChanged(object sender, object e)
        {
            await ThreadUtility.CallOnUIThreadAsync(Dispatcher, () =>
            {
                BackgroundTintOpacitySlider.IsEnabled = UISettings.AdvancedEffectsEnabled &&
                                                        PowerManager.EnergySaverStatus != EnergySaverStatus.On;
            });
        }

        private async void UISettings_AdvancedEffectsEnabledChanged(UISettings sender, object args)
        {
            await ThreadUtility.CallOnUIThreadAsync(Dispatcher, () =>
            {
                BackgroundTintOpacitySlider.IsEnabled = UISettings.AdvancedEffectsEnabled &&
                                                        PowerManager.EnergySaverStatus != EnergySaverStatus.On;
            });
        }

        private void ThemeRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                switch (radioButton.Tag)
                {
                    case "Light":
                        ThemeSettingsService.ThemeMode = ElementTheme.Light;
                        break;
                    case "Dark":
                        ThemeSettingsService.ThemeMode = ElementTheme.Dark;
                        break;
                    case "Default":
                        ThemeSettingsService.ThemeMode = ElementTheme.Default;
                        break;
                }
                InteropService.SyncSettings(SettingsKey.RequestedThemeStr, ThemeSettingsService.ThemeMode);
            }
        }

        private void AccentColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (AccentColorPicker.IsEnabled)
            {
                ThemeSettingsService.AppAccentColor = args.NewColor;
                if (!AccentColorToggle.IsOn)
                {
                    ThemeSettingsService.CustomAccentColor = args.NewColor;
                    InteropService.SyncSettings(SettingsKey.CustomAccentColorHexStr, ThemeSettingsService.CustomAccentColor);
                }
                InteropService.SyncSettings(SettingsKey.AppAccentColorHexStr, ThemeSettingsService.AppAccentColor);
            }
        }

        private void BackgroundTintOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ThemeSettingsService.AppBackgroundPanelTintOpacity = e.NewValue / 100;
            InteropService.SyncSettings(SettingsKey.AppBackgroundTintOpacityDouble, ThemeSettingsService.AppBackgroundPanelTintOpacity);
        }

        private void WindowsAccentColorToggle_OnToggled(object sender, RoutedEventArgs e)
        {
            AccentColorPicker.IsEnabled = !AccentColorToggle.IsOn;
            ThemeSettingsService.UseWindowsAccentColor = AccentColorToggle.IsOn;
            AccentColorPicker.Color = AccentColorToggle.IsOn ? ThemeSettingsService.AppAccentColor : ThemeSettingsService.CustomAccentColor;
            InteropService.SyncSettings(SettingsKey.UseWindowsAccentColorBool, ThemeSettingsService.UseWindowsAccentColor);
        }
    }
}