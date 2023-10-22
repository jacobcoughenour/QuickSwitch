using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace QuickSwitch
{
    /// <summary>
    /// Interaction logic for SwitchWindow.xaml
    /// </summary>
    public partial class SwitchWindow : DialogWindow
    {
        public SwitchItem SelectedItem => DocumentsList?.SelectedItem as SwitchItem;

        private SettingsManager _settingsManager;

        public SwitchWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            InitializeComponent();

            Activated += SwitchWindow_Activated;
            Deactivated += SwitchWindow_Deactivated;
            KeyDown += SwitchWindow_KeyDown;
            KeyUp += SwitchWindow_KeyUp;
            ContentRendered += Window_ContentRendered;
            MouseDown += DialogWindow_MouseDown;
            Closed += SwitchWindow_Closed;

            VersionNumberText.Text = $"v{QuickSwitchPackage.VersionNumber}";

            LoadPosition();
        }

        private void LoadPosition()
        {
            // load the window position and size
            _settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            WritableSettingsStore store = _settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!store.CollectionExists("QuickSwitch"))
                store.CreateCollection("QuickSwitch");
            Width = store.GetInt64("QuickSwitch", "Width", (int)Width);
            Height = store.GetInt64("QuickSwitch", "Height", (int)Height);

            // get default values
            var area = SystemParameters.WorkArea;
            var centerLeft = area.Left + area.Width / 2;
            var centerTop = area.Top + area.Height / 2;
            Left = (double)store.GetInt64("QuickSwitch", "Left", (int)centerLeft);
            Top = (double)store.GetInt64("QuickSwitch", "Top", (int)centerTop);

            // todo position it relative to the main visual studio window
        }

        private void SwitchWindow_Closed(object sender, EventArgs e)
        {
            // save the window position and size
            WritableSettingsStore store = _settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!store.CollectionExists("QuickSwitch"))
                store.CreateCollection("QuickSwitch");
            store.SetInt64("QuickSwitch", "Width", (int)Width);
            store.SetInt64("QuickSwitch", "Height", (int)Height);
            store.SetInt64("QuickSwitch", "Left", (int)Left);
            store.SetInt64("QuickSwitch", "Top", (int)Top);
        }

        private void SwitchWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.None && e.Key == Key.LeftAlt)
            {
                Close();
                e.Handled = true;
                return;
            }
        }

        private void SwitchWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }

            // todo get the keybind settings

            var isDown = Keyboard.GetKeyStates(Key.O).HasFlag(KeyStates.Down);
            if (isDown && e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
            {
                // select next
                DocumentsList.SelectedIndex = (DocumentsList.SelectedIndex + 1) % DocumentsList.Items.Count;
                e.Handled = true;
                return;
            }
            if (isDown && e.KeyboardDevice.Modifiers == (ModifierKeys.Shift | ModifierKeys.Alt))
            {
                // select previous
                DocumentsList.SelectedIndex = (DocumentsList.Items.Count + (DocumentsList.SelectedIndex - 1)) % DocumentsList.Items.Count;
                e.Handled = true;
                return;
            }
        }

        private void SwitchWindow_Activated(object sender, EventArgs e)
        {

            FocusList();
        }

        private void SwitchWindow_Deactivated(object sender, EventArgs e)
        {
            try
            {
                // close the window when it loses focus
                Close();
            }
            catch (Exception)
            {
            }
        }

        public void PopulateItems(IEnumerable<SwitchItem> items)
        {
            DocumentsList.ItemsSource = items;
            int i = 0;
            foreach (var item in items)
            {
                i++;
                if (item.IsCurrentFile)
                    break;
            }

            DocumentsList.SelectedIndex = i % DocumentsList.Items.Count;
        }

        public void FocusList()
        {
            DocumentsList.Focus();
            Keyboard.Focus(DocumentsList);
        }

        private void DialogWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // makes window draggable
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #region window styling stuff

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        [Flags]
        public enum DwmWindowAttribute : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_CAPTION_COLOR = 35,
            DWMWA_MICA_EFFECT = 1029
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual((Visual)sender) as HwndSource;
            UpdateStyleAttributes(hwndSource);
        }

        public static void UpdateStyleAttributes(HwndSource hwnd)
        {
            if (hwnd is null)
                return;

            //int trueValue = 0x01;
            //// set dark mode
            //DwmSetWindowAttribute(hwnd.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
            //// set mica effect
            //DwmSetWindowAttribute(hwnd.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));

            int color = 0x26292f;
            color = (color & 0xFF) << 16 | (color & 0xFF00) | (color & 0xFF0000) >> 16;
            DwmSetWindowAttribute(hwnd.Handle, DwmWindowAttribute.DWMWA_CAPTION_COLOR, ref color, 4);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Get PresentationSource
            PresentationSource presentationSource = PresentationSource.FromVisual((Visual)sender);

            // Subscribe to PresentationSource's ContentRendered event
            presentationSource.ContentRendered += Window_ContentRendered;
        }

        #endregion

    }
}
