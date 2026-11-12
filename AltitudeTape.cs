using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MSFS_AP_Panel
{
    // Horizontal tape-like control für Höhe: zeigt Tausender-Beschriftungen (2 => 2000 ft) und Skala
    public class AltitudeTape : Control
    {
        private int valueFeet = 0;
        private int visibleSpanFeet = 6000; // wie viele Fuß insgesamt sichtbar (z.B. 6000 => ähnlich visuelle Dichte wie HeadingTape)
        private readonly int majorTickFeet = 1000;
        private readonly int minorTickFeet = 100;

        public AltitudeTape()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.FromArgb(34, 34, 34);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        }

        [Category("Appearance")]
        [Description("Aktuelle Höhe in Fuß")]
        [DefaultValue(0)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Value
        {
            get => valueFeet;
            set
            {
                if (value < 0) value = 0;
                if (valueFeet != value)
                {
                    valueFeet = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [Description("Angezeigter Bereich in Fuß")]
        [DefaultValue(10000)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int VisibleSpanFeet
        {
            get => visibleSpanFeet;
            set
            {
                if (value < 1000) value = 1000;
                visibleSpanFeet = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(BackColor);

            int w = Width;
            int h = Height;
            int cx = w / 2;

            // Hintergrund
            using var penFrame = new Pen(Color.FromArgb(80, 80, 80), 1f);
            using var brushBg = new SolidBrush(Color.FromArgb(28, 28, 28));
            g.FillRectangle(brushBg, 0, 0, w, h);
            g.DrawRectangle(penFrame, 0, 0, w - 1, h - 1);

            // Fixed layout: top half = numeric area, bottom half = scale
            int topTextHeight = h / 2; // upper half (0..h/2)
            int scaleTop = topTextHeight; // scale starts at h/2
            int scaleBottom = h; // scale runs to bottom
            int scaleHeight = scaleBottom - scaleTop; // should be h/2

            // Pixel per foot across visible span
            float pxPerFoot = (float)w / visibleSpanFeet;
            int halfFeet = visibleSpanFeet / 2;

            // center reference rounded to nearest 1000 ft so labels and top number align
            //int centerRef = ((Value + 500) / 1000) * 1000;
            int centerRef = ((Value + 50) / 100) * 100;

            // Draw ticks across the scale area using centerRef as center value
            int minorLength = Math.Max(4, scaleHeight / 4);
            int majorLength = minorLength + 5;
            for (int A = centerRef - halfFeet; A <= centerRef + halfFeet; A += minorTickFeet)
            {
                int currentFt = A;

                float x = cx + (currentFt - centerRef) * pxPerFoot;
                if (x < -10 || x > w + 10) continue;

                bool isMajor = (currentFt % majorTickFeet) == 0;

                if (isMajor)
                {
                    // major ticks: full height over scale
                    int y1 = scaleTop + (scaleHeight - majorLength);
                    int y2 = scaleBottom - 2;
                    using var pen = new Pen(ForeColor, 2f);
                    g.DrawLine(pen, x, y1, x, y2);

                    // thousands label: only draw for non-negative heights (no label left of 0 ft)
                    if (currentFt >= 0)
                    {
                        int thousands = currentFt / 1000;
                        string label = thousands.ToString();
                        using var lblFont = new Font(Font.FontFamily, 9F, FontStyle.Bold);
                        var sz = g.MeasureString(label, lblFont);
                        using var brush = new SolidBrush(ForeColor);
                        float labelY = y1 - sz.Height - 2; // directly above tick
                        g.DrawString(label, lblFont, brush, x - sz.Width / 2f, labelY);
                    }
                }
                else
                {
                    // minor ticks: short ticks at lower part of scale
                    int y1 = scaleBottom - minorLength - 2;
                    int y2 = scaleBottom - 2;
                    using var pen = new Pen(ForeColor, 1f);
                    g.DrawLine(pen, x, y1, x, y2);
                }
            }

            // center marker: small green triangle at the bottom edge of the scale (tip pointing up)
            using var brushTri = new SolidBrush(Color.LightGreen);
            using var penTri = new Pen(Color.FromArgb(0, 128, 0));
            int triWidth = Math.Max(8, scaleHeight / 3);
            int triHeight = Math.Max(6, scaleHeight / 3);
            // place base just below the scale bottom and tip pointing up into the scale
            float baseY = scaleBottom + 1f;
            var triPoints = new[] {
                new PointF(cx - triWidth, baseY),
                new PointF(cx + triWidth, baseY),
                new PointF(cx, baseY - triHeight) // tip points up
            };
            g.FillPolygon(brushTri, triPoints);
            g.DrawPolygon(penTri, triPoints);

            // central numeric value above the center (exact current value)
            string centerText = Value.ToString() + " ft";
            using var centerFont = new Font(Font.FontFamily, Math.Max(10, Font.Size + 2), FontStyle.Bold);
            var centerSize = g.MeasureString(centerText, centerFont);
            using var brushCenter = new SolidBrush(ForeColor);
            // place numeric centered horizontally and vertically in the topText area
            float centerY = Math.Max(2, (scaleTop - centerSize.Height) / 2f);
            g.DrawString(centerText, centerFont, brushCenter, cx - centerSize.Width / 2, centerY);

            // draw x1000 box bottom-right overlapping the scale (hides part of scale)
            string xLabel = "x1000";
            using var xFont = new Font(Font.FontFamily, 8F, FontStyle.Regular);
            var xSz = g.MeasureString(xLabel, xFont);
            int boxW = (int)xSz.Width + 7;
            int boxH = (int)xSz.Height + 1;//        scaleHeight + 1;
            int boxX = w - boxW;
            int boxY = scaleTop - 4;
            using var boxBrush = new SolidBrush(Color.FromArgb(20, 20, 20));
            using var boxPen = new Pen(Color.FromArgb(80, 80, 80));
            g.FillRectangle(boxBrush, boxX, boxY, boxW, boxH);
            // draw x1000 text inside box (bottom-right)
            using var xBrush = new SolidBrush(ForeColor);
            g.DrawString(xLabel, xFont, xBrush, boxX + 6, boxY + (boxH - xSz.Height) / 2f);
        }
    }
}
