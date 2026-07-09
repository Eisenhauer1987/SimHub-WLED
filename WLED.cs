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

        // Blink states consolidated into a small helper
        private class BlinkState
        {
            public bool IsOn;
            public DateTime LastToggle;
            public TimeSpan Interval;
            public byte R, G, B;

            public BlinkState(TimeSpan interval, byte r, byte g, byte b)
            {
                Interval = interval;
                R = r; G = g; B = b;
                LastToggle = DateTime.MinValue;
                IsOn = false;
            }
        }

        // Helper to read flag-like properties from StatusDataBase via reflection with multiple candidate names
        private bool GetFlagValue(object statusData, params string[] candidatePropertyNames)
        {
            if (statusData == null) return false;
            var t = statusData.GetType();
            foreach (var name in candidatePropertyNames)
            {
                var p = t.GetProperty(name);
                if (p == null) continue;
                var val = p.GetValue(statusData);
                if (val is double dv) return dv > 0.5;
                if (val is float fv) return fv > 0.5f;
                if (val is bool bv) return bv;
                if (val is int iv) return iv != 0;
            }
            return false;
        }

        private readonly BlinkState _redBlink = new BlinkState(TimeSpan.FromMilliseconds(200), 255, 0, 0);
        private readonly BlinkState _checkeredBlink = new BlinkState(TimeSpan.FromMilliseconds(150), 255, 255, 255);
        private readonly BlinkState _yellowBlink = new BlinkState(TimeSpan.FromMilliseconds(250), 255, 200, 0);
        private readonly BlinkState _blueBlink = new BlinkState(TimeSpan.FromMilliseconds(300), 0, 0, 255);

        public PluginManager PluginManager { get; set; }

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "WLED";

        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            Settings = this.ReadCommonSettings<WLEDSettings>("GeneralSettings", () => new WLEDSettings());

            try
            {
                // If WLEDHelper has multiple constructors, prefer the one matching your implementation
                LedHelper = new WLEDHelper(
                    Settings.stripUrl,
                    Settings.stripPort,
                    Settings.SegmentColors,
                    Settings.MaxColor
                );

                ApplySettingsToHelper(force: true);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error($"WLED: error initializing LedHelper: {ex.Message}");
                LedHelper = null;
            }
        }

        // Apply settings to helper only when needed (or forced)
        private void ApplySettingsToHelper(bool force = false)
        {
            if (LedHelper == null) return;

            // Only assign when changed to avoid unnecessary writes each frame
            if (force || LedHelper.LedAmount != Settings.ledAmount) LedHelper.LedAmount = Settings.ledAmount;
            if (force || LedHelper.Offset != Settings.offset) LedHelper.Offset = Settings.offset;
            if (force || LedHelper.Mirror != Settings.mirror) LedHelper.Mirror = Settings.mirror;
            if (force || LedHelper.Center != Settings.center) LedHelper.Center = Settings.center;
            // Update color hex settings if changed
            try
            {
                LedHelper.UpdateMaxColor(Settings.MaxColor);
                LedHelper.UpdateSegments(Settings.SegmentColors, 3);
            }
            catch
            {
                // Swallow; UpdateColors is tolerant, but avoid breaking the update loop
            }
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            // Ensure helper exists
            if (LedHelper == null)
            {
                // Try to recover if Settings changed and helper can be (re)created later
                return;
            }

            // If settings are changed from UI they may be updated here; only apply if different
            ApplySettingsToHelper();

            if (!data.GameRunning)
            {
                LedHelper.TurnOff();
                return;
            }

            // Treat flags as true when > 0.5 (robust vs. exact eq to 1.0)
            bool redFlag = false, checkeredFlag = false, yellowFlag = false, blueFlag = false, whiteFlag = false, greenFlag = false;
            var newDataObj = data.NewData;
            if (newDataObj != null)
            {
                var ndType = newDataObj.GetType();
                object val;
                var p = ndType.GetProperty("Flag_Red") ?? ndType.GetProperty("FlagRed");
                if (p != null && (val = p.GetValue(newDataObj)) != null)
                    redFlag = val is double dv1 ? dv1 > 0.5 : val is bool bv1 ? bv1 : val is int iv1 && iv1 != 0;

                p = ndType.GetProperty("Flag_Checkered") ?? ndType.GetProperty("FlagCheckered");
                if (p != null && (val = p.GetValue(newDataObj)) != null)
                    checkeredFlag = val is double dv2 ? dv2 > 0.5 : val is bool bv2 ? bv2 : val is int iv2 && iv2 != 0;

                p = ndType.GetProperty("Flag_Yellow") ?? ndType.GetProperty("FlagYellow");
                if (p != null && (val = p.GetValue(newDataObj)) != null)
                    yellowFlag = val is double dv3 ? dv3 > 0.5 : val is bool bv3 ? bv3 : val is int iv3 && iv3 != 0;

                p = ndType.GetProperty("Flag_Blue") ?? ndType.GetProperty("FlagBlue");
                if (p != null && (val = p.GetValue(newDataObj)) != null)
                    blueFlag = val is double dv4 ? dv4 > 0.5 : val is bool bv4 ? bv4 : val is int iv4 && iv4 != 0;

                p = ndType.GetProperty("Flag_White") ?? ndType.GetProperty("FlagWhite");
                if (p != null && (val = p.GetValue(newDataObj)) != null)
                    whiteFlag = val is double dv5 ? dv5 > 0.5 : val is bool bv5 ? bv5 : val is int iv5 && iv5 != 0;

                p = ndType.GetProperty("Flag_Green") ?? ndType.GetProperty("FlagGreen");
                if (p != null && (val = p.GetValue(newDataObj)) != null)
                    greenFlag = val is double dv6 ? dv6 > 0.5 : val is bool bv6 ? bv6 : val is int iv6 && iv6 != 0;
            }

            try
            {
                if (redFlag)
                {
                    HandleBlink(_redBlink);
                    return;
                }

                if (checkeredFlag)
                {
                    HandleBlink(_checkeredBlink);
                    return;
                }

                if (yellowFlag)
                {
                    HandleBlink(_yellowBlink);
                    return;
                }

                if (blueFlag)
                {
                    HandleBlink(_blueBlink);
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
            catch (Exception ex)
            {
                // Guard against WLED communication failures at runtime
                SimHub.Logging.Current.Error($"WLED: exception in DataUpdate: {ex.Message}");
            }
        }

        private void HandleBlink(BlinkState state)
        {
            // Use UTC for interval comparisons to avoid DST/timezone issues
            var now = DateTime.UtcNow;
            if ((now - state.LastToggle) >= state.Interval)
            {
                state.IsOn = !state.IsOn;
                state.LastToggle = now;
            }

            LedHelper.ShowSolidOrBlink(state.IsOn, state.R, state.G, state.B);
        }

        public void End(PluginManager pluginManager)
        {
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this);
        }
    }
}