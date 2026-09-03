using System;
using UnityEngine;

namespace ShopPrototype.EditorTools
{
    public enum IconShape
    {
        Sword,
        Shield,
        Bow,
        Potion,
        Bread,
        Cheese,
        Torch,
        Rope,
        Map,
        Amulet
    }

    public static class IconPainter
    {
        private const int Size = 128;
        private const float Half = Size * 0.5f;

        public static byte[] RenderPng(IconShape shape, Color32 baseColor)
        {
            Color32 bg = Tint(baseColor, 0.30f);
            Color32 border = Tint(baseColor, 0.75f);
            Color32 glyph = Color.Lerp(baseColor, Color.white, 0.62f);

            Func<Vector2, float> sdf = GetShapeSdf(shape);
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - Half, Size - 0.5f - y - Half);

                    float dBack = SdRoundRect(p, new Vector2(60f, 60f), 26f);
                    float dBorder = Mathf.Max(dBack, -(dBack + 5f));
                    float dGlyph = sdf(p);

                    var color = Over(default, bg, Coverage(dBack));
                    color = Over(color, border, Coverage(dBorder));
                    color = Over(color, glyph, Coverage(dGlyph));

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture.EncodeToPNG();
        }

        public static byte[] RenderRoundRectPng(int size, float radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f);
                    float d = SdRoundRect(p, new Vector2(size * 0.5f - 1f, size * 0.5f - 1f), radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Coverage(d)));
                }
            }

            texture.Apply();
            return texture.EncodeToPNG();
        }

        private static Func<Vector2, float> GetShapeSdf(IconShape shape)
        {
            switch (shape)
            {
                case IconShape.Sword:
                    return p => Min
                    (
                        SdCapsule(p, new Vector2(-14, 18), new Vector2(32, -28), 6.5f),
                        SdCapsule(p, new Vector2(-25.5f, 12.5f), new Vector2(-8.5f, 29.5f), 4.5f),
                        SdCapsule(p, new Vector2(-24, 28), new Vector2(-17, 21), 4f),
                        SdCircle(p, new Vector2(-28, 32), 4.5f)
                    );

                case IconShape.Shield:
                    return p => Min
                    (
                        SdRoundRect(p - new Vector2(0f, 6f), new Vector2(30f, 26f), 10f),
                        SdTriangle(p, new Vector2(-30, -2), new Vector2(30, -2), new Vector2(0, -46))
                    );

                case IconShape.Bow:
                {
                    Vector2 center = new Vector2(22f, 0f);
                    float r = 34f;
                    Vector2 end1 = center + new Vector2(Mathf.Cos(115f * Mathf.Deg2Rad), Mathf.Sin(115f * Mathf.Deg2Rad)) * r;
                    Vector2 end2 = center + new Vector2(Mathf.Cos(245f * Mathf.Deg2Rad), Mathf.Sin(245f * Mathf.Deg2Rad)) * r;
                    return p => Min
                    (
                        SdArc(p, center, r, 5f, 115f, 245f),
                        SdCircle(p, end1, 2.5f),
                        SdCircle(p, end2, 2.5f),
                        SdCapsule(p, end1, end2, 2f),
                        SdCapsule(p, new Vector2(-26, 0), new Vector2(36, 0), 2.8f),
                        SdCapsule(p, new Vector2(27, 8), new Vector2(37, 0), 2.8f),
                        SdCapsule(p, new Vector2(27, -8), new Vector2(37, 0), 2.8f)
                    );
                }

                case IconShape.Potion:
                    return p => Min
                    (
                        SdCircle(p, new Vector2(0f, -14f), 24f),
                        SdRoundRect(p - new Vector2(0f, 22f), new Vector2(8f, 9f), 3f),
                        SdRoundRect(p - new Vector2(0f, 33f), new Vector2(10f, 5f), 2.5f)
                    );

                case IconShape.Bread:
                {
                    Vector2 slashDir = new Vector2(Mathf.Cos(65f * Mathf.Deg2Rad), Mathf.Sin(65f * Mathf.Deg2Rad)) * 9f;
                    return p =>
                    {
                        float loaf = SdEllipse(p, Vector2.zero, new Vector2(42f, 22f));
                        float slashes = Min
                        (
                            SdCapsule(p, new Vector2(-16, 4) - slashDir, new Vector2(-16, 4) + slashDir, 3.5f),
                            SdCapsule(p, new Vector2(0, 0) - slashDir, new Vector2(0, 0) + slashDir, 3.5f),
                            SdCapsule(p, new Vector2(16, -4) - slashDir, new Vector2(16, -4) + slashDir, 3.5f)
                        );
                        return Mathf.Max(loaf, -slashes);
                    };
                }

                case IconShape.Cheese:
                    return p => Mathf.Max
                    (
                        SdTriangle(p, new Vector2(-34, 0), new Vector2(34, -19), new Vector2(34, 19)),
                        -Min
                        (
                            SdCircle(p, new Vector2(8, -3), 6f),
                            SdCircle(p, new Vector2(21, 6), 4.5f),
                            SdCircle(p, new Vector2(2, 8), 3.5f)
                        )
                    );

                case IconShape.Torch:
                    return p => Min
                    (
                        SdCapsule(p, new Vector2(0, -40), new Vector2(0, 12), 7f),
                        SdCircle(p, new Vector2(0f, 26f), 12f),
                        SdTriangle(p, new Vector2(-9, 22), new Vector2(9, 22), new Vector2(0, 48))
                    );

                case IconShape.Rope:
                    return p => Min
                    (
                        Mathf.Abs((p - Vector2.zero).magnitude - 26f) - 7f,
                        SdCapsule(p, new Vector2(26, 0), new Vector2(42, -10), 6f)
                    );

                case IconShape.Map:
                    return p => Min
                    (
                        Mathf.Abs(SdRoundRect(p, new Vector2(40f, 30f), 6f)) - 3.5f,
                        SdCircle(p, new Vector2(-22, 14), 4f),
                        SdCircle(p, new Vector2(-8, 4), 4f),
                        SdCircle(p, new Vector2(4, 12), 4f),
                        SdCircle(p, new Vector2(16, -2), 4f),
                        SdCapsule(p, new Vector2(19, -25), new Vector2(33, -11), 3.2f),
                        SdCapsule(p, new Vector2(19, -11), new Vector2(33, -25), 3.2f)
                    );

                case IconShape.Amulet:
                    return p => Min
                    (
                        Mathf.Abs((p - new Vector2(0f, 2f)).magnitude - 30f) - 6f,
                        SdRoundRect(Rotate(p - new Vector2(0f, 2f), 45f), new Vector2(11f, 11f), 3f)
                    );

                default:
                    return p => SdCircle(p, Vector2.zero, 30f);
            }
        }

        private static float SdCircle(Vector2 p, Vector2 center, float radius)
        {
            return (p - center).magnitude - radius;
        }

        private static float SdCapsule(Vector2 p, Vector2 a, Vector2 b, float radius)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - radius;
        }

        private static float SdRoundRect(Vector2 p, Vector2 half, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x) - half.x + radius, Mathf.Abs(p.y) - half.y + radius);
            Vector2 max = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return new Vector2(max.x, max.y).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static float SdEllipse(Vector2 p, Vector2 center, Vector2 radii)
        {
            Vector2 q = (p - center);
            q = new Vector2(q.x / radii.x, q.y / radii.y);
            return (q.magnitude - 1f) * Mathf.Min(radii.x, radii.y);
        }

        private static float SdArc(Vector2 p, Vector2 center, float radius, float thickness, float fromDeg, float toDeg)
        {
            Vector2 q = p - center;
            float angle = Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg;
            if (angle < 0f)
                angle += 360f;

            bool inRange = fromDeg <= toDeg
                ? angle >= fromDeg && angle <= toDeg
                : angle >= fromDeg || angle <= toDeg;

            if (!inRange)
                return float.MaxValue;

            return Mathf.Abs(q.magnitude - radius) - thickness * 0.5f;
        }

        private static float SdTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

            bool inside = Cross(b - a, p - a) >= 0f && Cross(c - b, p - b) >= 0f && Cross(a - c, p - c) >= 0f;
            float d = Min
            (
                DistanceToSegment(p, a, b),
                DistanceToSegment(p, b, c),
                DistanceToSegment(p, c, a)
            );

            return inside ? -d : d;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(p - a, ba) / Vector2.Dot(ba, ba));
            return (p - a - ba * h).magnitude;
        }

        private static Vector2 Rotate(Vector2 p, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
        }

        private static float Min(params float[] values)
        {
            float best = float.MaxValue;
            foreach (float v in values)
                if (v < best)
                    best = v;
            return best;
        }

        private static float Coverage(float sdf, float feather = 1.4f)
        {
            return Mathf.Clamp01(0.5f - sdf / feather);
        }

        private static Color32 Over(Color32 backdrop, Color32 source, float sourceAlpha)
        {
            float a = source.a / 255f * sourceAlpha;
            float outA = backdrop.a / 255f + a * (1f - backdrop.a / 255f);
            if (outA <= 0f)
                return default;

            float r = (source.r * a + backdrop.r * (backdrop.a / 255f) * (1f - a)) / outA;
            float g = (source.g * a + backdrop.g * (backdrop.a / 255f) * (1f - a)) / outA;
            float b = (source.b * a + backdrop.b * (backdrop.a / 255f) * (1f - a)) / outA;
            return new Color32((byte)Mathf.RoundToInt(r), (byte)Mathf.RoundToInt(g), (byte)Mathf.RoundToInt(b), (byte)Mathf.RoundToInt(outA * 255f));
        }

        private static Color32 Tint(Color32 color, float factor)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(color.r * factor),
                (byte)Mathf.RoundToInt(color.g * factor),
                (byte)Mathf.RoundToInt(color.b * factor),
                color.a);
        }
    }
}
