using Microsoft.Maui.Graphics;

namespace Monotp.Views;

public class RingDrawable : IDrawable
{
    public double Fraction { get; set; }
    public int Remaining { get; set; }
    public Color RingColor { get; set; } = Colors.White;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        float radius = size / 2f - 3f;
        var center = new PointF(dirtyRect.Width / 2f, dirtyRect.Height / 2f);

        canvas.StrokeColor = RingColor.WithAlpha(0.22f);
        canvas.StrokeSize = 2.5f;
        canvas.DrawCircle(center, radius);

        canvas.StrokeColor = RingColor;
        canvas.StrokeSize = 3f;
        float start = -90f;
        float sweep = (float)(Fraction * 360.0);
        var rect = new RectF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        canvas.DrawArc(rect, start, start - sweep, true, false);

        canvas.FontColor = RingColor;
        canvas.FontSize = 11;
        canvas.DrawString(Remaining.ToString(), dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
