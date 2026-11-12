using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MSFS_AP_Panel
{
    public class CenterProgressBar : Control
    {
        private int _minimum = -1000;
        private int _maximum = 1000;
        private int _value = 0;

        [DefaultValue(-1000)]
        public int Minimum { get => _minimum; set { _minimum = value; Invalidate(); } }
        [DefaultValue(1000)]
        public int Maximum { get => _maximum; set { _maximum = value; Invalidate(); } }

        [DefaultValue(0)]
        public int Value
        {
            get => _value;
            set
            {
                // Store the raw value so the displayed text can show the real value.
                // The rendered bar will be clamped to Minimum..Maximum when painting.
                if (value == _value) return;
                _value = value;
                Invalidate();
            }
        }

        [DefaultValue(typeof(Color), "LimeGreen")]
        public Color PositiveColor { get; set; } = Color.LimeGreen;
        [DefaultValue(typeof(Color), "OrangeRed")]
        public Color NegativeColor { get; set; } = Color.OrangeRed;
        [DefaultValue(typeof(Color), "DimGray")]
        public Color BorderColor { get; set; } = Color.DimGray;
        [DefaultValue(typeof(Color), "Black")]
        public Color ZeroMarkColor { get; set; } = Color.Black;

        // Optionale Einheit, die neben dem Wert angezeigt wird (z. B. "ft/min")
        [DefaultValue("")]
        public string Unit { get; set; } = string.Empty;

        public CenterProgressBar()
        {
            DoubleBuffered = true;
            BackColor = SystemColors.Control;
            ForeColor = Color.Black;
            Font = new Font("Segoe UI", 8F);
            Height = 16;
            Width = 70;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle rect = ClientRectangle;
            g.Clear(BackColor);

            // draw border
            using var pen = new Pen(BorderColor);
            g.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);

            // calc zero position relative to range
            float range = Maximum - Minimum;
            if (range <= 0) return;
            float zeroPosNorm = (0f - Minimum) / range; // 0..1
            int zeroX = (int)(rect.Width * zeroPosNorm);

            // draw filled part (clamped for rendering, raw value shown as text)
            int drawVal = Math.Max(Minimum, Math.Min(Maximum, _value));
            if (drawVal > 0)
            {
                float posRatio = (float)drawVal / (float)Math.Max(1, Maximum);
                posRatio = Math.Min(1f, Math.Max(0f, posRatio));
                int fillW = (int)((rect.Width - zeroX - 1) * posRatio);
                if (fillW > 0)
                {
                    var fillRect = new Rectangle(zeroX + 1, 1, fillW, rect.Height - 2);
                    using var b = new SolidBrush(PositiveColor);
                    g.FillRectangle(b, fillRect);
                }
            }
            else if (drawVal < 0)
            {
                float negRatio = (float)Math.Abs(drawVal) / (float)Math.Max(1, Math.Abs(Minimum));
                negRatio = Math.Min(1f, Math.Max(0f, negRatio));
                int fillW = (int)((zeroX - 1) * negRatio);
                if (fillW > 0)
                {
                    var fillRect = new Rectangle(zeroX - fillW, 1, fillW, rect.Height - 2);
                    using var b = new SolidBrush(NegativeColor);
                    g.FillRectangle(b, fillRect);
                }
            }

            // optional value text center
            // Show the raw value (not clamped) as text, optional with Einheit
            string txt = _value.ToString();
            if (!string.IsNullOrEmpty(Unit)) txt = txt + " " + Unit;
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var brush = new SolidBrush(ForeColor);
            g.DrawString(txt, Font, brush, rect, sf);
        }
    }
}
