namespace Halcyon.WLED
{
    public class WLEDSettings
    {
        public string stripUrl { get; set; } = "wled-table.local";
        public int stripPort { get; set; } = 21324;
        public int ledAmount { get; set; } = 60;
        public int offset { get; set; } = 0;
        public bool mirror { get; set; } = false;
        public bool center { get; set; } = false;

        public string MaxColor { get; set; } = "#FF0000";
        // Comma-separated list of hex colors for the intermediate segments (left-to-right)
        // Exactly three segment colors are expected.
        public string SegmentColors { get; set; } = "#0000FF,#00FF00,#FFFF00";
        // Number of physical segments on the strip (e.g., 2 for left/right)
        public int PhysicalSegments { get; set; } = 2;
        // If true, keep controlling the strip when no game is running; otherwise release (turn off)
        public bool KeepControlDuringIdle { get; set; } = true;
        // Splotter warming (visual warming state)
        public bool EnableSplotterWarming { get; set; } = false;
        // Spotter warning (opponent warning) support
        public bool EnableSpotterWarning { get; set; } = false;

        // Flags enable/disable
        public bool EnableFlagRed { get; set; } = true;
        public bool EnableFlagCheckered { get; set; } = true;
        public bool EnableFlagYellow { get; set; } = true;
        public bool EnableFlagBlue { get; set; } = true;
        public bool EnableFlagWhite { get; set; } = true;
        public bool EnableFlagGreen { get; set; } = true;
    }
}
