using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MSFS_AP_Panel
{
    // Einfacher, schmaler "Kompass-Tape"-Control: zeigt einen Ausschnitt des Kompasses mit
    // der aktuellen Heading-Zahl zentriert. Text-Eigenschaft akzeptiert Strings wie "123°".
    public class HeadingTape : Control
    {
        private int valueDegrees = 0;
        private int visibleSpan = 60; // Anzahl Grad, die insgesamt sichtbar sind (z.B. 60 => ±30)
        private readonly int majorTick = 10;
        private readonly int minorTick = 1;

        public HeadingTape()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.FromArgb(34, 34, 34);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        }

        [Category("Appearance")]
        [Description("Aktueller Heading-Wert in Grad (0..359)")]
        [DefaultValue(0)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Value
        {
            get => valueDegrees;
            set
            {
                int v = value % 360;
                if (v < 0) v += 360;
                if (valueDegrees != v)
                {
                    valueDegrees = v;
                    Invalidate();
                }
            }
        }

        // Hinweis: Text-Override entfernt, Value-Eigenschaft steuert das Control.

        [Category("Appearance")]
        [Description("Wie viele Grad insgesamt sichtbar sind (Breite des Tapes)")]
        [DefaultValue(60)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int VisibleSpan
        {
            get => visibleSpan;
            set
            {
                if (value < 10) value = 10;
                visibleSpan = value;
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

            // Mittlere X-Position
            int cx = w / 2;

            // Hintergrund-Rahmen leicht abgerundet
            using var penFrame = new Pen(Color.FromArgb(80, 80, 80), 1f);
            using var brushBg = new SolidBrush(Color.FromArgb(28, 28, 28));
            g.FillRectangle(brushBg, 0, 0, w, h);
            g.DrawRectangle(penFrame, 0, 0, w - 1, h - 1);

            // Pixel pro Grad
            float pxPerDeg = (float)w / visibleSpan;

            // Bereich: center - halfSpan .. center + halfSpan
            int half = visibleSpan / 2;

            // Draw ticks across visible range
            for (int d = -half; d <= half; d += minorTick)
            {
                float x = cx + d * pxPerDeg;
                if (x < -10 || x > w + 10) continue;

                int labelVal = Mod(Value + d, 360);
                bool isMajor = ((labelVal % majorTick) == 0);
                bool isCardinal = isMajor && (labelVal % 90 == 0);
                int tickH = isMajor ? Math.Max(10, h / 3) : Math.Max(4, h / 6);
                int y1 = h - tickH - 6;
                int y2 = h - 6;

                Color tickColor = isCardinal ? Color.Red : ForeColor;
                using var pen = new Pen(tickColor, isMajor ? 2f : 1f);
                g.DrawLine(pen, x, y1, x, y2);

                // Draw labels for major ticks
                if (isMajor)
                {
                    string label = labelVal.ToString();
                    using var lblFont = new Font(Font.FontFamily, 10F, FontStyle.Bold);
                    var sz = g.MeasureString(label, lblFont);
                    using var brush = new SolidBrush(ForeColor);
                    g.DrawString(label, lblFont, brush, x - sz.Width / 2f, y1 - sz.Height - 2);
                }
            }

            // center marker: kleines hellgrünes Dreieck am unteren Rand, Spitze nach oben
            using var brushTri = new SolidBrush(Color.LightGreen);
            using var penTri = new Pen(Color.FromArgb(0, 128, 0));
            int triWidth = Math.Max(6, h / 8);
            int triHeight = Math.Max(6, h / 8);
            float baseY = h - 6f; // Basis direkt an der unteren Skalenkante
            var triPoints = new[] {
                new PointF(cx - triWidth, baseY),
                new PointF(cx + triWidth, baseY),
                new PointF(cx, baseY - triHeight) // Spitze zeigt nach oben
            };
            g.FillPolygon(brushTri, triPoints);
            g.DrawPolygon(penTri, triPoints);
        }

        private static int Mod(int x, int m)
        {
            int r = x % m;
            if (r < 0) r += m;
            return r;
        }
    }
}
