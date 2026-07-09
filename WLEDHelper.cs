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

        private (int, int, int) RiseColor { get; set; }
        private (int, int, int) MaxColor { get; set; }

        private UdpClient LedClient { get; set; }

        public WLEDHelper(string host, int port, string riseColorHex, string maxColorHex)
        {
            this.host = host;
            this.port = port;
            LedClient = new UdpClient(host, port);
            RiseColor = ParseColor(riseColorHex, (0, 0, 255));
            MaxColor = ParseColor(maxColorHex, (255, 0, 0));
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

            (int, int, int) outerColor = Mirror ? RiseColor : (0, 0, 0);
            (int, int, int) innerColor = Mirror ? (0, 0, 0) : RiseColor;

            var data = new List<byte>();
            data.Add(0x01);
            data.Add(0x01);

            if (shouldShift)
            {
                for (int i = left; i < right; i++)
                {
                    data.Add(BitConverter.GetBytes(i)[0]);
                    data.AddRange(ColorToByteList(MaxColor));
                }
            }
            else
            {
                if (Center)
                {
                    var center = right / 2;
                    int leftBorder = center - scaledAmount / 2;
                    int rightBorder = center + scaledAmount / 2;

                    if (Mirror)
                    {
                        leftBorder = left + scaledAmount / 2;
                        rightBorder = right - scaledAmount / 2;
                    }

                    for (int i = left; i < leftBorder; i++)
                    {
                        data.Add(BitConverter.GetBytes(i)[0]);
                        data.AddRange(ColorToByteList(outerColor));
                    }
                    for (int i = leftBorder; i < rightBorder; i++)
                    {
                        data.Add(BitConverter.GetBytes(i)[0]);
                        data.AddRange(ColorToByteList(innerColor));
                    }
                    for (int i = rightBorder; i < right; i++)
                    {
                        data.Add(BitConverter.GetBytes(i)[0]);
                        data.AddRange(ColorToByteList(outerColor));
                    }
                }
                else
                {
                    var border = left + scaledAmount;
                    if (Mirror)
                    {
                        border = right - scaledAmount;
                    }

                    for (int i = left; i < border + Offset; i++)
                    {
                        data.Add(BitConverter.GetBytes(i)[0]);
                        data.AddRange(ColorToByteList(innerColor));
                    }

                    for (int i = border; i < right; i++)
                    {
                        data.Add(BitConverter.GetBytes(i)[0]);
                        data.AddRange(ColorToByteList(outerColor));
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
    }
}
