using UnityEngine;

namespace BeyProject.Core
{
    /// <summary>
    /// Generates a simple solid-color square sprite at runtime. Used as a fallback so
    /// gameplay objects render as *something* even before the editor bootstrapper has
    /// generated real placeholder art (or in tests/prefabs with no sprite assigned).
    /// </summary>
    public static class PlaceholderSprite
    {
        private const int Size = 16;
        private const float PixelsPerUnit = 16f;

        private static Sprite sharedSquare;
        private static Sprite sharedCircle;
        private static Sprite[] sharedBoltFrames;

        /// <summary>
        /// A single white square shared by every caller, tinted per-instance via
        /// SpriteRenderer.color. Anything spawned repeatedly at runtime (projectiles, hit
        /// sparks, explosions) must use this rather than CreateSquare - CreateSquare
        /// allocates a fresh Texture2D per call, which leaks once it's on a hot path.
        /// </summary>
        public static Sprite SharedSquare()
        {
            if (sharedSquare == null)
            {
                sharedSquare = CreateSquare(Color.white);
            }

            return sharedSquare;
        }

        /// <summary>Shared white circle - same tint-per-instance rule as SharedSquare.</summary>
        public static Sprite SharedCircle()
        {
            if (sharedCircle == null)
            {
                sharedCircle = CreateCircle(Color.white);
            }

            return sharedCircle;
        }

        /// <summary>
        /// A looping 4-frame "energy bolt" animation - a bright core with a spark rotating
        /// around it - shared by every caller and cached exactly like SharedCircle/SharedSquare,
        /// so cycling through frames on a hot-fired projectile never allocates a new Texture2D.
        /// White pixels, same as the other Shared* sprites, so SpriteRenderer.color still tints
        /// each instance independently.
        /// </summary>
        public static Sprite[] SharedBoltFrames()
        {
            if (sharedBoltFrames == null)
            {
                const int frameCount = 4;
                sharedBoltFrames = new Sprite[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    sharedBoltFrames[i] = CreateBoltFrame(i, frameCount);
                }
            }

            return sharedBoltFrames;
        }

        private static Sprite CreateBoltFrame(int frameIndex, int frameCount)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((Size - 1) / 2f, (Size - 1) / 2f);
            float radius = Size / 2f - 0.5f;
            float coreRadius = radius * 0.45f;
            float angle = frameIndex * (360f / frameCount) * Mathf.Deg2Rad;
            var sparkDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var transparent = new Color(0f, 0f, 0f, 0f);
            var dim = new Color(1f, 1f, 1f, 0.35f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var point = new Vector2(x, y);
                    float distance = Vector2.Distance(point, center);

                    if (distance <= coreRadius)
                    {
                        texture.SetPixel(x, y, Color.white);
                        continue;
                    }

                    if (distance <= radius)
                    {
                        float alongSpark = Vector2.Dot((point - center).normalized, sparkDirection);
                        texture.SetPixel(x, y, alongSpark > 0.6f ? Color.white : dim);
                        continue;
                    }

                    texture.SetPixel(x, y, transparent);
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        public static Sprite CreateCircle(Color color)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((Size - 1) / 2f, (Size - 1) / 2f);
            float radius = Size / 2f - 0.5f;
            var transparent = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    bool inside = Vector2.Distance(new Vector2(x, y), center) <= radius;
                    texture.SetPixel(x, y, inside ? color : transparent);
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        public static Sprite CreateSquare(Color color)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
