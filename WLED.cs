using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;

namespace Halcyon.WLED
{
    [PluginDescription("WLED things")]
    [PluginAuthor("Halcyon")]
    [PluginName("WLED")]
    public class WLED : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public WLEDSettings Settings;

        private WLEDHelper LedHelper { get; set; }
        private bool yellowBlinkState = false;
        private DateTime lastYellowBlinkToggle = DateTime.MinValue;

        public PluginManager PluginManager { get; set; }

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "WLED";

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            LedHelper.LedAmount = Settings.ledAmount;
            LedHelper.Offset = Settings.offset;
            LedHelper.Mirror = Settings.mirror;
            LedHelper.Center = Settings.center;

            if (!data.GameRunning)
            {
                return;
            }

            var yellowFlag = data.NewData.Flag_Yellow == 1.0;

            if (yellowFlag)
            {
                if ((DateTime.Now - lastYellowBlinkToggle).TotalMilliseconds >= 250)
                {
                    yellowBlinkState = !yellowBlinkState;
                    lastYellowBlinkToggle = DateTime.Now;
                }

                if (yellowBlinkState)
                {
                    LedHelper.SetSolidColor(Colors.Yellow);
                }
                else
                {
                    LedHelper.SetSolidColor(Colors.Black);
                }

                return;
            }

            var rpm = data.NewData.Rpms;
            var maxRpm = data.NewData.MaxRpm;
            var shouldShift = data.NewData.CarSettings_RPMShiftLight1 == 1.0;

            LedHelper.ShowRpm(rpm, maxRpm, shouldShift);
        }

        public void End(PluginManager pluginManager)
        {
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            Settings = this.ReadCommonSettings<WLEDSettings>("GeneralSettings", () => new WLEDSettings());

            LedHelper = new WLEDHelper(Settings.stripUrl, Settings.stripPort);
            LedHelper.LedAmount = Settings.ledAmount;
        }
    }
}
