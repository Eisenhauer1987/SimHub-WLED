using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Windows.Media;

namespace Halcyon.WLED
{
    public class WLEDHelper
    {
        private string host;
        private int port;
        public int LedAmount { get; set; }
        public int Offset { get; set; }

        public bool Mirror { get; set; }
        public bool Center { get; set; }

        private (int, int, int) MaxColor { get; set; }

        // Expose the last-set hex values so callers can detect changes
        public string CurrentMaxHex { get; private set; }

        // We support exactly 3 intermediate color segments before the final MaxColor
        private const int FixedSegments = 3;
        private List<(int, int, int)> SegmentColorsList { get; set; } = new List<(int, int, int)>();

        // Number of physical segments on the strip (e.g., 2 for left/right)
        public int PhysicalSegments { get; private set; } = 2;

        private UdpClient LedClient { get; set; }

        // segmentColorsCsv: CSV list of hex strings for the three segments (left-to-right)
        public WLEDHelper(string host, int port, string segmentColorsCsv, string maxColorHex)
        {
            this.host = host;
            this.port = port;
            LedClient = new UdpClient(host, port);
            CurrentMaxHex = maxColorHex ?? string.Empty;
            MaxColor = ParseColor(CurrentMaxHex, (255, 0, 0));
            // Initialize segments (expect 3 segments)
            UpdateSegments(segmentColorsCsv ?? string.Empty, FixedSegments);
        }

        // Update segment configuration: csv hex colors and count of segments (count is expected to be 3)
        public void UpdateSegments(string segmentColorsCsv, int segmentsCount)
        {
            var count = Math.Max(0, Math.Min(FixedSegments, segmentsCount));
            SegmentColorsList.Clear();
            if (string.IsNullOrWhiteSpace(segmentColorsCsv) || count == 0)
                return;

            var parts = segmentColorsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Math.Min(parts.Length, count); i++)
            {
                var p = parts[i].Trim();
                var parsed = ParseColor(p, (0, 0, 255));
                SegmentColorsList.Add(parsed);
            }

            // If fewer colors provided than required, repeat the last provided color or add a default
            while (SegmentColorsList.Count < count)
            {
                if (SegmentColorsList.Count > 0)
                    SegmentColorsList.Add(SegmentColorsList[SegmentColorsList.Count - 1]);
                else
                    SegmentColorsList.Add((0, 0, 255));
            }
        }

        // Update number of physical segments
        public void UpdatePhysicalSegments(int segments)
        {
            PhysicalSegments = Math.Max(1, segments);
        }

        // Update only the Max color at runtime
        public void UpdateMaxColor(string maxColorHex)
        {
            if (maxColorHex == null) maxColorHex = string.Empty;
            if (CurrentMaxHex != maxColorHex)
            {
                CurrentMaxHex = maxColorHex;
                MaxColor = ParseColor(CurrentMaxHex, MaxColor);
            }
        }

        public void ShowRpm(double rpm, double maxRpm, bool shouldShift)
        {
            if (LedAmount <= 0 || maxRpm <= 0)
            {
                return;
            }

            int scaledAmount = (int)rpm / Math.Max(1, ((int)maxRpm / LedAmount));
            int left = 0 + Offset;
            int right = LedAmount + Offset;

            (int, int, int) outerColor = (0, 0, 0);

            var data = new List<byte>();
            data.Add(0x01);
            data.Add(0x01);

            // Render each physical segment in parallel: same effect applied per-segment
            for (int seg = 0; seg < PhysicalSegments; seg++)
            {
                int baseIndex = Offset + seg * LedAmount;

                if (shouldShift)
                {
                    for (int i = 0; i < LedAmount; i++)
                    {
                        int ledIndex = baseIndex + i;
                        data.Add(BitConverter.GetBytes(ledIndex)[0]);
                        data.AddRange(ColorToByteList(MaxColor));
                    }
                    continue;
                }

                if (Center)
                {
                    var center = LedAmount / 2;
                    int leftBorder = center - scaledAmount / 2;
                    int rightBorder = center + scaledAmount / 2;

                    if (Mirror)
                    {
                        leftBorder = 0 + scaledAmount / 2;
                        rightBorder = LedAmount - scaledAmount / 2;
                    }

                    for (int i = 0; i < leftBorder; i++)
                    {
                        int ledIndex = baseIndex + i;
                        data.Add(BitConverter.GetBytes(ledIndex)[0]);
                        data.AddRange(ColorToByteList(outerColor));
                    }

                    for (int i = leftBorder; i < rightBorder; i++)
                    {
                        int ledIndex = baseIndex + i;
                        data.Add(BitConverter.GetBytes(ledIndex)[0]);
                        var color = GetColorForPosition(i, LedAmount);
                        if (Mirror) color = InvertColor(color);
                        data.AddRange(ColorToByteList(color));
                    }

                    for (int i = rightBorder; i < LedAmount; i++)
                    {
                        int ledIndex = baseIndex + i;
                        data.Add(BitConverter.GetBytes(ledIndex)[0]);
                        data.AddRange(ColorToByteList(outerColor));
                    }
                }
                else
                {
                    var border = scaledAmount;
                    if (Mirror)
                    {
                        // For mirror mode, lit LEDs are at the end of the segment
                        for (int i = 0; i < LedAmount - border; i++)
                        {
                            int ledIndex = baseIndex + i;
                            data.Add(BitConverter.GetBytes(ledIndex)[0]);
                            data.AddRange(ColorToByteList(outerColor));
                        }
                        for (int i = LedAmount - border; i < LedAmount; i++)
                        {
                            int ledIndex = baseIndex + i;
                            data.Add(BitConverter.GetBytes(ledIndex)[0]);
                            var color = GetColorForPosition(i, LedAmount);
                            data.AddRange(ColorToByteList(color));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < border; i++)
                        {
                            int ledIndex = baseIndex + i;
                            data.Add(BitConverter.GetBytes(ledIndex)[0]);
                            var color = GetColorForPosition(i, LedAmount);
                            data.AddRange(ColorToByteList(color));
                        }
                        for (int i = border; i < LedAmount; i++)
                        {
                            int ledIndex = baseIndex + i;
                            data.Add(BitConverter.GetBytes(ledIndex)[0]);
                            data.AddRange(ColorToByteList(outerColor));
                        }
                    }
                }
            }

            SendData(data);
        }

        // Render RPM per-segment but allow spotter override on specific physical segments.
        // activeSpotterSegments: true = show red on this physical segment, false = show RPM on this segment
        public void ShowRpmWithSpotter(double rpm, double maxRpm, bool shouldShift, bool[] activeSpotterSegments)
        {
            if (LedAmount <= 0 || maxRpm <= 0)
            {
                return;
            }

            int scaledAmount = (int)rpm / Math.Max(1, ((int)maxRpm / LedAmount));
            (int, int, int) outerColor = (0, 0, 0);

            var data = new List<byte>();
            data.Add(0x01);
            data.Add(0x01);

            for (int seg = 0; seg < PhysicalSegments; seg++)
            {
                int baseIndex = Offset + seg * LedAmount;

                bool spotterActive = activeSpotterSegments != null && seg < activeSpotterSegments.Length && activeSpotterSegments[seg];

                if (spotterActive)
                {
                    // entire physical segment red
                    for (int i = 0; i < LedAmount; i++)
                    {
                        int ledIndex = baseIndex + i;
                        data.Add(BitConverter.GetBytes(ledIndex)[0]);
                        data.AddRange(ColorToByteList((255, 0, 0)));
                    }
                    continue;
                }

                if (shouldShift)
                {
                    for (int i = 0; i < LedAmount; i++)
                    {
                        int ledIndex = baseIndex + i;
                        data.Add(BitConverter.GetBytes(ledIndex)[0]);
                        data.AddRange(ColorToByteList(MaxColor));
                    }
                    continue;
                }

                if (Center)
                {
                    var center = LedAmount / 2;
                    int leftBorder = center - scaledAmount / 2;
                    int rightBorder = center + scaledAmount / 2;

                    if (Mirror)
                    {
                        leftBorder = 0 + scaledAmount / 2;
                        rightBorder = LedAmount - scaledAmount / 2;
                    }

                    for (int i = 0; i < leftBorder; i++)
                    {
                        int ledIndex = baseIndex + i;
                        data.Add(BitConverter.GetBytes(ledIndex)[0]);
                        data.AddRange(ColorToByteList(outerColor));
                    }

                    for (int i = leftBorder; i < rightBorder; i++)
                    {
                        int ledIndex = baseIndex + i;
                        data.Add(BitConverter.GetBytes(ledIndex)[0]);
                        var color = GetColorForPosition(i, LedAmount);
                        if (Mirror) color = InvertColor(color);
                        data.AddRange(ColorToByteList(color));
                    }

                    for (int i = rightBorder; i < LedAmount; i++)
                    {
                        int ledIndex = baseIndex + i;
                        data.Add(BitConverter.GetBytes(ledIndex)[0]);
                        data.AddRange(ColorToByteList(outerColor));
                    }
                }
                else
                {
                    var border = scaledAmount;
                    if (Mirror)
                    {
                        for (int i = 0; i < LedAmount - border; i++)
                        {
                            int ledIndex = baseIndex + i;
                            data.Add(BitConverter.GetBytes(ledIndex)[0]);
                            data.AddRange(ColorToByteList(outerColor));
                        }
                        for (int i = LedAmount - border; i < LedAmount; i++)
                        {
                            int ledIndex = baseIndex + i;
                            data.Add(BitConverter.GetBytes(ledIndex)[0]);
                            var color = GetColorForPosition(i, LedAmount);
                            data.AddRange(ColorToByteList(color));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < border; i++)
                        {
                            int ledIndex = baseIndex + i;
                            data.Add(BitConverter.GetBytes(ledIndex)[0]);
                            var color = GetColorForPosition(i, LedAmount);
                            data.AddRange(ColorToByteList(color));
                        }
                        for (int i = border; i < LedAmount; i++)
                        {
                            int ledIndex = baseIndex + i;
                            data.Add(BitConverter.GetBytes(ledIndex)[0]);
                            data.AddRange(ColorToByteList(outerColor));
                        }
                    }
                }
            }

            SendData(data);
        }

        public void ShowSolid(int r, int g, int b)
        {
            var data = new List<byte>();
            data.Add(0x01);
            data.Add(0x01);

            for (int i = 0; i < LedAmount; i++)
            {
                int ledIndex = i + Offset;
                data.Add(BitConverter.GetBytes(ledIndex)[0]);
                data.AddRange(ColorToByteList((r, g, b)));
            }

            SendData(data);
        }

        public void ShowSolidOrBlink(bool blinkOn, int r, int g, int b)
        {
            if (blinkOn)
            {
                ShowSolid(r, g, b);
            }
            else
            {
                TurnOff();
            }
        }

        public void TurnOff()
        {
            ShowSolid(0, 0, 0);
        }

        // Show spotter warning per physical segment. activeSegments length should be >= PhysicalSegments.
        public void ShowSpotterSegments(bool[] activeSegments)
        {
            if (activeSegments == null) return;
            if (LedAmount <= 0) return;

            var data = new List<byte>();
            data.Add(0x01);
            data.Add(0x01);

            bool any = false;
            for (int seg = 0; seg < PhysicalSegments; seg++)
            {
                bool active = seg < activeSegments.Length && activeSegments[seg];
                if (!active) continue;
                any = true;
                int baseIndex = Offset + seg * LedAmount;
                for (int i = 0; i < LedAmount; i++)
                {
                    int ledIndex = baseIndex + i;
                    data.Add(BitConverter.GetBytes(ledIndex)[0]);
                    data.AddRange(ColorToByteList((255, 0, 0)));
                }
            }

            if (any)
            {
                SendData(data);
            }
        }

        private void SendData(List<byte> data)
        {
            var dataArray = data.ToArray();
            LedClient.Send(dataArray, dataArray.Length);
        }

        private (int, int, int) ParseColor(string hex, (int, int, int) fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex))
                    return fallback;

                hex = hex.Trim().TrimStart('#');

                if (hex.Length != 6)
                    return fallback;

                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);

                return (r, g, b);
            }
            catch
            {
                return fallback;
            }
        }

        private List<byte> ColorToByteList((int, int, int) color)
        {
            return new List<byte>
            {
                BitConverter.GetBytes(color.Item1)[0],
                BitConverter.GetBytes(color.Item2)[0],
                BitConverter.GetBytes(color.Item3)[0]
            };
        }

        private (int, int, int) GetColorForPosition(int positionFromStart, int total)
        {
            // positionFromStart in [0, total-1]
            if (total <= 0) return SegmentColorsList.Count > 0 ? SegmentColorsList[0] : (0, 0, 255);

            // fraction of the way along the strip
            double fraction = positionFromStart / (double)Math.Max(1, total);

            // determine segment index across FixedSegments + final max segment
            int segCount = FixedSegments;
            int segIndex = (int)(fraction * (segCount + 1));
            if (segIndex < 0) segIndex = 0;
            if (segIndex >= segCount) return MaxColor;
            if (SegmentColorsList.Count > segIndex) return SegmentColorsList[segIndex];
            // fallback: first segment color if available, else a default blue
            if (SegmentColorsList.Count > 0) return SegmentColorsList[0];
            return (0,0,255);
        }

        private (int, int, int) InvertColor((int, int, int) c)
        {
            // simple inversion around black/white not needed; for mirror we swap roles externally
            return c;
        }
    }
}
