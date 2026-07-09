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
        private bool blueBlinkState = false;
        private DateTime lastBlueBlinkToggle = DateTime.MinValue;
        private bool redBlinkState = false;
        private DateTime lastRedBlinkToggle = DateTime.MinValue;
        private bool checkeredBlinkState = false;
        private DateTime lastCheckeredBlinkToggle = DateTime.MinValue;

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
                LedHelper.TurnOff();
                return;
            }

            bool redFlag = data.NewData.Flag_Red == 1.0;
            bool checkeredFlag = data.NewData.Flag_Checkered == 1.0;
            bool yellowFlag = data.NewData.Flag_Yellow == 1.0;
            bool blueFlag = data.NewData.Flag_Blue == 1.0;
            bool whiteFlag = data.NewData.Flag_White == 1.0;
            bool greenFlag = data.NewData.Flag_Green == 1.0;

            if (redFlag)
            {
                if ((DateTime.Now - lastRedBlinkToggle).TotalMilliseconds >= 200)
                {
                    redBlinkState = !redBlinkState;
                    lastRedBlinkToggle = DateTime.Now;
                }

                LedHelper.ShowSolidOrBlink(redBlinkState, 255, 0, 0);
                return;
            }

            if (checkeredFlag)
            {
                if ((DateTime.Now - lastCheckeredBlinkToggle).TotalMilliseconds >= 150)
                {
                    checkeredBlinkState = !checkeredBlinkState;
                    lastCheckeredBlinkToggle = DateTime.Now;
                }

                LedHelper.ShowSolidOrBlink(checkeredBlinkState, 255, 255, 255);
                return;
            }

            if (yellowFlag)
            {
                if ((DateTime.Now - lastYellowBlinkToggle).TotalMilliseconds >= 250)
                {
                    yellowBlinkState = !yellowBlinkState;
                    lastYellowBlinkToggle = DateTime.Now;
                }

                LedHelper.ShowSolidOrBlink(yellowBlinkState, 255, 200, 0);
                return;
            }

            if (blueFlag)
            {
                if ((DateTime.Now - lastBlueBlinkToggle).TotalMilliseconds >= 300)
                {
                    blueBlinkState = !blueBlinkState;
                    lastBlueBlinkToggle = DateTime.Now;
                }

                LedHelper.ShowSolidOrBlink(blueBlinkState, 0, 0, 255);
                return;
            }

            if (whiteFlag)
            {
                LedHelper.ShowSolid(255, 255, 255);
                return;
            }

            if (greenFlag)
            {
                LedHelper.ShowSolid(0, 255, 0);
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

            LedHelper = new WLEDHelper(
                Settings.stripUrl,
                Settings.stripPort,
                Settings.RiseColor,
                Settings.MaxColor
            );
            LedHelper.LedAmount = Settings.ledAmount;
        }
    }
}
            LedHelper.ShowRpm(rpm, maxRpm, shouldShift);
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            Settings = this.ReadCommonSettings<WLEDSettings>("GeneralSettings", () => new WLEDSettings());

            LedHelper = new WLEDHelper(Settings.stripUrl, Settings.stripPort);
            LedHelper.LedAmount = Settings.ledAmount;
        }
    }
}
