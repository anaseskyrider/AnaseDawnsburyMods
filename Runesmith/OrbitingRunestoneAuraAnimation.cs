using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithClass;

public class OrbitingRunestoneAuraAnimation : RotatingIllusionsAnimation
{
    public override Illustration IllusionIllustration(QEffect qf)
    {
        return qf.Illustration!;
    }

    public override void Draw(Rectangle rectThisTile)
    {
        float opacity = Math.Min(this.MaximumOpacity / (float)(1.0 + this.SizeMultiplier / 2.0), 1f);
        QEffect qf = this.OwnerQEffect!;
        Illustration illustration = this.IllusionIllustration(qf);
        double angle = Flicker.FlickerRotation * 1.0 % (2.0 * Math.PI) - Math.PI;
        for (int i = 0; i < 1; i++)
        {
            Vector2 vectorAngle = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            float furtherAway = 1.4f;
            float smaller = 0.5f;
            float offset =
                ((float)rectThisTile.Width / qf.Owner.Space.SizeInSquares)
                * (this.SizeMultiplier - 0.5f)
                * furtherAway;
            if (offset > 0.0)
            {
                float width = rectThisTile.Width * smaller;
                float height = rectThisTile.Height * smaller;
                Rectangle rectangle = new Rectangle(
                    (int)(rectThisTile.X + (width/2f) + (vectorAngle.X * offset) - (offset / (2 * furtherAway)) + (width / 2)),
                    (int)(rectThisTile.Y + (height/2f) + (vectorAngle.Y * offset) - (offset / (2 * furtherAway)) + (height / 2)),
                    (int)width,
                    (int)height
                );
                Primitives.DrawImageNative(illustration, rectangle, this.IllusionColor(i, opacity));
                angle += 2.0 * Math.PI / 3.0;
            }
        }
    }
}