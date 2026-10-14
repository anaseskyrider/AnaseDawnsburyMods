using Dawnsbury.Display.Illustrations;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithClass;

public record SmallerIllustration(Illustration InnerIllustration, float SizeMultiplier = 0.5f) : Illustration
{
    public override string IllustrationAsIconString => InnerIllustration.IllustrationAsIconString;
    
    public override void DrawImage(Rectangle rectangle, Color? color, bool scale, bool scaleUp, Color? scaleBgColor)
    {
        int wDown = (int)Math.Round(rectangle.Width * SizeMultiplier);
        int hDown = (int)Math.Round(rectangle.Height * SizeMultiplier);
        var smallerRectangle = new Rectangle(
            rectangle.X + ((rectangle.Width - wDown) / 2),
            rectangle.Y + ((rectangle.Height - hDown) / 2),
            wDown,
            hDown
        );
        InnerIllustration.DrawImage(smallerRectangle, color, scale, scaleUp, scaleBgColor);
    }

    public override void DrawImageNative(Rectangle rectangle, Color? color, bool scale, bool scaleUp, Color? scaleBgColor)
    {
        int wDown = (int)Math.Round(rectangle.Width * SizeMultiplier);
        int hDown = (int)Math.Round(rectangle.Height * SizeMultiplier);
        var smallerRectangle = new Rectangle(
            rectangle.X + ((rectangle.Width - wDown) / 2),
            rectangle.Y + ((rectangle.Height - hDown) / 2),
            wDown,
            hDown
        );
        InnerIllustration.DrawImageNative(smallerRectangle, color, scale, scaleUp, scaleBgColor);
    }
}