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

        public string RiseColor { get; set; } = "#0000FF";
        public string MaxColor { get; set; } = "#FF0000";
    }
}
