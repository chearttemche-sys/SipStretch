using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SipStretch
{
    public static class Art
    {
        private static readonly Color Mint = Color.FromArgb(136, 250, 205);
        private static readonly Color Sea = Color.FromArgb(41, 183, 172);
        private static readonly Color Foam = Color.FromArgb(222, 255, 238);
        private static readonly Color Apricot = Color.FromArgb(255, 175, 120);

        // Draw in illustration space, preserving the caller's scale, clip and transform.
        public static void Draw(Graphics g, Rectangle rect, bool water, double phase)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            GraphicsState saved = g.Save();
            try
            {
                g.SetClip(rect, CombineMode.Intersect);
                float scale = Math.Min(rect.Width / 200f, rect.Height / 150f);
                g.TranslateTransform(rect.X + (rect.Width - 200 * scale) / 2f,
                                     rect.Y + (rect.Height - 150 * scale) / 2f);
                g.ScaleTransform(scale, scale);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                double t = (phase - Math.Floor(phase)) * Math.PI * 2;
                DrawBackdrop(g, water ? Mint : Apricot, t);
                if (water) DrawBottle(g, t);
                else DrawSquat(g, t);
            }
            finally { g.Restore(saved); }
        }

        private static void DrawBackdrop(Graphics g, Color accent, double t)
        {
            using (SolidBrush glow = new SolidBrush(Color.FromArgb(15, accent)))
                g.FillEllipse(glow, 41, 13, 118, 118);
            using (Pen rim = new Pen(Color.FromArgb(49, accent), 1f))
            {
                g.DrawArc(rim, 33, 7, 134, 134, 203, 109);
                g.DrawArc(rim, 33, 7, 134, 134, 21, 39);
            }
            for (int i = 0; i < 3; i++)
            {
                float x = i == 0 ? 39 : i == 1 ? 163 : 148;
                float y = (i == 0 ? 58 : i == 1 ? 85 : 28) + 2 * (float)Math.Sin(t + i * 2);
                float r = i == 0 ? 3.6f : 2.6f;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(170, accent)))
                    g.FillPolygon(b, new[] { new PointF(x, y-r), new PointF(x+r*.5f, y),
                        new PointF(x,y+r), new PointF(x-r*.5f,y) });
            }
        }

        private static void DrawBottle(Graphics g, double t)
        {
            float wave = (float)Math.Sin(t);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(53, 1, 22, 26)))
                g.FillEllipse(shadow, 68 + wave, 132, 66 - wave * 2, 6);
            GraphicsState bottle = g.Save();
            try
            {
                g.TranslateTransform(100, 76 + 2 * wave);
                g.RotateTransform(8 + wave * 2.5f);
                using (GraphicsPath glass = new GraphicsPath())
                {
                    glass.StartFigure();
                    glass.AddLine(-11, -43, -11, -38);
                    glass.AddBezier(-11,-38,-11,-34,-25,-33,-25,-21);
                    glass.AddLine(-25,-21,-25,39);
                    glass.AddBezier(-25,39,-25,49,-21,53,-14,53);
                    glass.AddLine(-14,53,14,53);
                    glass.AddBezier(14,53,21,53,25,49,25,39);
                    glass.AddLine(25,39,25,-21);
                    glass.AddBezier(25,-21,25,-33,11,-34,11,-38);
                    glass.AddLine(11,-38,11,-43);
                    glass.CloseFigure();
                    using (LinearGradientBrush tint = new LinearGradientBrush(new Point(-26, 0),
                        new Point(26, 0), Color.FromArgb(170, 84, 168, 163), Color.FromArgb(200, 30, 96, 104)))
                        g.FillPath(tint, glass);
                    GraphicsState inner = g.Save();
                    g.SetClip(glass, CombineMode.Intersect);
                    using (GraphicsPath water = new GraphicsPath())
                    {
                        float surface = -4 + wave * 2.5f;
                        water.AddBezier(-29,surface,-15,surface-9*wave,5,surface+10*wave,29,surface-2);
                        water.AddLine(29,surface-2,29,56);
                        water.AddLine(29,56,-29,56);
                        water.CloseFigure();
                        using (LinearGradientBrush liquid = new LinearGradientBrush(new Point(0,-10),
                            new Point(0,54), Mint, Color.FromArgb(15, 138, 153)))
                            g.FillPath(liquid, water);
                        using (Pen edge = new Pen(Foam, 1.4f))
                            g.DrawBezier(edge,-29,surface,-15,surface-9*wave,5,surface+10*wave,29,surface-2);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        double progress = (t / (2 * Math.PI) + i * .25) % 1;
                        float x = (float)(3 + (i % 2)*9 + 1.2 * Math.Sin(t + i));
                        float y = 46 - (float)progress * 41;
                        int alpha = (int)(150 * Math.Sin(progress * Math.PI));
                        using (Pen pearl = new Pen(Color.FromArgb(alpha, Foam), 1.1f))
                            g.DrawEllipse(pearl, x,y,2.5f,2.5f);
                    }
                    g.Restore(inner);
                    using (Pen outline = new Pen(Color.FromArgb(205,Foam), 2))
                        g.DrawPath(outline, glass);
                    Stroke(g,Color.FromArgb(195,Foam),2.8f, -18,-19,-18,11);
                    Stroke(g,Color.FromArgb(100,Foam),2.3f, -18,17,-18,23);
                    Stroke(g,Color.FromArgb(105,Foam),1.6f, 18,32,18,39);
                }
                using (GraphicsPath cap = Round(-14,-53,28,12,3))
                using (SolidBrush b = new SolidBrush(Mint))
                    g.FillPath(b,cap);
                for (int i=-8; i<=8; i+=4) Stroke(g, Sea, 1.2f, i,-50,i,-45);
            }
            finally { g.Restore(bottle); }
        }

        private static void DrawSquat(Graphics g, double t)
        {
            float bend = (1 - (float)Math.Cos(t)) / 2;
            bend = bend * bend * (3 - 2 * bend);
            PointF hip = new PointF(99 - 22*bend, 81 + 23*bend);
            PointF shoulder = new PointF(100 + 1*bend, 52 + 27*bend);
            PointF head = new PointF(101 + 4*bend, 37 + 26*bend);
            PointF knee = new PointF(97 + 16*bend, 106 - 6*bend);
            PointF elbow = new PointF(107 + 13*bend, 67 + 9*bend);
            PointF hand = new PointF(114 + 25*bend, 79 - 4*bend);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(65, 2, 25, 28)))
                g.FillEllipse(shadow,63,133,80,6);

            Color farLeg = Color.FromArgb(42, 126, 123);
            Limb(g, farLeg, 10, new PointF(hip.X-4,hip.Y),new PointF(knee.X-13,knee.Y+1),new PointF(82,126));
            Stroke(g, Sea, 6,80,130,90,130);
            Limb(g, Mint, 11, hip, knee, new PointF(99,127));
            Stroke(g, Foam, 6.5f,97,131,109,131);

            using (GraphicsPath shirt = new GraphicsPath())
            {
                shirt.AddBezier(shoulder.X-3, shoulder.Y-5,shoulder.X+15,shoulder.Y-5,
                                shoulder.X+9,shoulder.Y+9,hip.X+7,hip.Y+4);
                shirt.AddLine(hip.X+7,hip.Y+4,hip.X-10,hip.Y+2);
                shirt.AddBezier(hip.X-10,hip.Y+2,hip.X-9,hip.Y-6,shoulder.X-15,shoulder.Y+5,
                                shoulder.X-3,shoulder.Y-5);
                using (SolidBrush b = new SolidBrush(Apricot)) g.FillPath(b,shirt);
            }
            Color skin = Color.FromArgb(255,211,173);
            Stroke(g,skin,5,head.X-1,head.Y+6,shoulder.X,shoulder.Y-2);
            using (SolidBrush b = new SolidBrush(skin))
            {
                g.FillEllipse(b,head.X-8,head.Y-9,17,20);
                g.FillEllipse(b,head.X+5,head.Y,5,4);
            }
            using (GraphicsPath hair = new GraphicsPath())
            using (SolidBrush b = new SolidBrush(Color.FromArgb(27, 63, 67)))
            {
                hair.AddBezier(head.X-8,head.Y+2,head.X-13,head.Y-13,head.X+5,head.Y-16,head.X+10,head.Y-6);
                hair.AddBezier(head.X+10,head.Y-6,head.X+8,head.Y-2,head.X+2,head.Y-5,head.X,head.Y-3);
                hair.AddLine(head.X,head.Y-3,head.X-5,head.Y+3);
                hair.CloseFigure();
                g.FillPath(b,hair);
            }
            Limb(g, skin, 5.8f, new PointF(shoulder.X+2,shoulder.Y+4), elbow, hand);
            Stroke(g,Color.FromArgb(249,149,101),8,shoulder.X,shoulder.Y+1,
                shoulder.X+(elbow.X-shoulder.X)*.46f, shoulder.Y+(elbow.Y-shoulder.Y)*.46f);
        }

        private static void Stroke(Graphics g, Color c, float width, float x1, float y1, float x2, float y2)
        {
            using (Pen p = new Pen(c,width))
            {
                p.StartCap = p.EndCap = LineCap.Round;
                g.DrawLine(p,x1,y1,x2,y2);
            }
        }

        private static void Limb(Graphics g, Color c, float width, PointF a, PointF b, PointF d)
        {
            using (Pen p = new Pen(c,width))
            {
                p.StartCap = p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p,new[] {a,b,d});
            }
        }

        private static GraphicsPath Round(float x,float y,float w,float h,float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius*2;
            path.AddArc(x,y,d,d,180,90);
            path.AddArc(x+w-d,y,d,d,270,90);
            path.AddArc(x+w-d,y+h-d,d,d,0,90);
            path.AddArc(x,y+h-d,d,d,90,90);
            path.CloseFigure();
            return path;
        }
    }
}
