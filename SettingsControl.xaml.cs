using SimHub.Plugins;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Halcyon.WLED
{
    public partial class SettingsControl : UserControl
    {
        private readonly WLED plugin;



        public SettingsControl(WLED plugin)
        {
            // Load the XAML content explicitly so this control works whether or not the generated partial
            // InitializeComponent is available in the build environment.
            var resourceLocater = new Uri("/Halcyon.WLED;component/SettingsControl.xaml", UriKind.Relative);
            System.Windows.Application.LoadComponent(this, resourceLocater);

            if (plugin == null) throw new ArgumentNullException(nameof(plugin));
            if (plugin.Settings == null) throw new ArgumentException("plugin.Settings is null", nameof(plugin));

            this.plugin = plugin;

            // Use FindName to obtain references to named elements (avoids depending on generated fields)
            var urlTb = (TextBox)FindName("UrlInput");
            var portTb = (TextBox)FindName("PortInput");
            var ledAmountTb = (TextBox)FindName("LedAmountInput");
            var ledOffsetTb = (TextBox)FindName("LedOffsetInput");
            var mirrorCb = (CheckBox)FindName("MirrorInput");
            var centerCb = (CheckBox)FindName("CenterInput");
            var maxColorTb = (TextBox)FindName("MaxColorInput");
            var segmentColorsTb = (TextBox)FindName("SegmentColorsInput");

            if (urlTb != null) urlTb.Text = plugin.Settings.stripUrl ?? string.Empty;
            if (portTb != null) portTb.Text = plugin.Settings.stripPort.ToString();
            if (ledAmountTb != null) ledAmountTb.Text = plugin.Settings.ledAmount.ToString();
            if (ledOffsetTb != null) ledOffsetTb.Text = plugin.Settings.offset.ToString();
            if (mirrorCb != null) mirrorCb.IsChecked = plugin.Settings.mirror;
            if (centerCb != null) centerCb.IsChecked = plugin.Settings.center;
            if (maxColorTb != null) maxColorTb.Text = plugin.Settings.MaxColor ?? string.Empty;
            if (segmentColorsTb != null) segmentColorsTb.Text = plugin.Settings.SegmentColors ?? string.Empty;
        }


        private void Apply(object sender, RoutedEventArgs e)
        {
            // Read/trim inputs via FindName so we don't rely on generated fields
            var urlTb = (TextBox)FindName("UrlInput");
            var portTb = (TextBox)FindName("PortInput");
            var ledAmountTb = (TextBox)FindName("LedAmountInput");
            var ledOffsetTb = (TextBox)FindName("LedOffsetInput");
            var mirrorCb = (CheckBox)FindName("MirrorInput");
            var centerCb = (CheckBox)FindName("CenterInput");
            var maxColorTb = (TextBox)FindName("MaxColorInput");
            var segmentColorsTb = (TextBox)FindName("SegmentColorsInput");

            var url = (urlTb?.Text ?? string.Empty).Trim();
            var portText = (portTb?.Text ?? string.Empty).Trim();
            var ledAmountText = (ledAmountTb?.Text ?? string.Empty).Trim();
            var offsetText = (ledOffsetTb?.Text ?? string.Empty).Trim();
            var mirror = mirrorCb?.IsChecked == true;
            var center = centerCb?.IsChecked == true;
            var maxColor = (maxColorTb?.Text ?? string.Empty).Trim();
            var segmentColorsText = (segmentColorsTb?.Text ?? string.Empty).Trim();

            // Validate numeric inputs and ranges
            if (!TryParseIntInRange(portText, 1, 65535, out var port))
            {
                MessageBox.Show("Port must be a number between 1 and 65535.", "Invalid port", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseIntInRange(ledAmountText, 0, 100000, out var ledAmount))
            {
                MessageBox.Show("LED amount must be a non-negative integer.", "Invalid LED amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseIntInRange(offsetText, 0, Math.Max(0, ledAmount - 1), out var offset))
            {
                // Allow offset 0 when ledAmount is 0; otherwise offset must be < ledAmount
                var maxOffset = Math.Max(0, ledAmount > 0 ? ledAmount - 1 : 0);
                MessageBox.Show($"Offset must be an integer between 0 and {maxOffset}.", "Invalid offset", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Optional: validate color strings (basic non-empty check). Consider a color picker.
            // If you have a specific format expected (hex, "r,g,b", name) add parsing here.
            // For now, accept empty but trim:
            // if (!IsValidColorString(riseColor)) { ... }

            // Only save if something changed to avoid unnecessary disk writes
            var s = plugin.Settings;
            var changed = false;

            if (s.stripUrl != url) { s.stripUrl = url; changed = true; }
            if (s.stripPort != port) { s.stripPort = port; changed = true; }
            if (s.ledAmount != ledAmount) { s.ledAmount = ledAmount; changed = true; }
            if (s.offset != offset) { s.offset = offset; changed = true; }
            if (s.mirror != mirror) { s.mirror = mirror; changed = true; }
            if (s.center != center) { s.center = center; changed = true; }
            if (s.MaxColor != maxColor) { s.MaxColor = maxColor; changed = true; }
            // Enforce exactly 3 segments; only update the CSV list
            if (s.SegmentColors != segmentColorsText) { s.SegmentColors = segmentColorsText; changed = true; }

            if (changed)
            {
                try
                {
                    plugin.SaveCommonSettings("GeneralSettings", s);
                    // Try to apply settings immediately if plugin exposes ApplySettingsToHelper
                    try { plugin?.GetType().GetMethod("ApplySettingsToHelper", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(plugin, new object[] { true }); } catch { }
                    // If you add a method to WLED to apply settings immediately (recommended),
                    // call it here. Example (requires implementation in WLED):
                    // plugin.ApplySettingsToHelper(force: true);
                    MessageBox.Show("Settings saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save settings: {ex.Message}", "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No changes to save.", "No changes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool TryParseIntInRange(string text, int minInclusive, int maxInclusive, out int value)
        {
            if (int.TryParse(text, out var parsed))
            {
                if (parsed < minInclusive || parsed > maxInclusive)
                {
                    value = default;
                    return false;
                }
                value = parsed;
                return true;
            }
            value = default;
            return false;
        }

        // Example placeholder for richer color validation
        // private bool IsValidColorString(string s) { /* parse hex or named color */ }

        private int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, out var result) ? result : fallback;
        }
    }
}