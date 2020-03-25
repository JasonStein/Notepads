﻿namespace Notepads.Controls.FindAndReplace
{
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    class QueryBoxCore : TextBox
    {
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);

            // By default, TextBox toggles case when user hit "Shift + F3"
            // This should be restricted
            if (shift.HasFlag(CoreVirtualKeyStates.Down) && e.Key == VirtualKey.F3)
            {
                return;
            }

            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }
    }
}
