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
            InitializeComponent();
            this.plugin = plugin;

            UrlInput.Text = plugin.Settings.stripUrl;
            PortInput.Text = plugin.Settings.stripPort.ToString();
            LedAmountInput.Text = plugin.Settings.ledAmount.ToString();
            LedOffsetInput.Text = plugin.Settings.offset.ToString();
            MirrorInput.IsChecked = plugin.Settings.mirror;
            CenterInput.IsChecked = plugin.Settings.center;
            RiseColorInput.Text = plugin.Settings.RiseColor;
            MaxColorInput.Text = plugin.Settings.MaxColor;
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            plugin.Settings.stripUrl = UrlInput.Text;
            plugin.Settings.stripPort = ParseInt(PortInput.Text, 21324);
            plugin.Settings.ledAmount = ParseInt(LedAmountInput.Text, 60);
            plugin.Settings.offset = ParseInt(LedOffsetInput.Text, 0);
            plugin.Settings.mirror = MirrorInput.IsChecked == true;
            plugin.Settings.center = CenterInput.IsChecked == true;
            plugin.Settings.RiseColor = RiseColorInput.Text;
            plugin.Settings.MaxColor = MaxColorInput.Text;

            plugin.SaveCommonSettings("GeneralSettings", plugin.Settings);
        }

        private int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, out var result) ? result : fallback;
        }
    }
}
