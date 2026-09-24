using SDL;
using Daybreak.UI;
using System.Runtime.CompilerServices;

namespace Daybreak.Helpers
{
    public class FrameLimiter
    {
        private readonly ulong _targetFrameTimeNs;
        private ulong _frameStartNs;
        
        private const ulong SleepBufferNs = 1_500_000UL;

        public FrameLimiter(double targetFps)
        {
            // Calculate nanoseconds per frame: 1,000,000,000 ns / FPS
            // e.g., 60 FPS = 16,666,666 ns
            _targetFrameTimeNs = (ulong)(1_000_000_000.0 / targetFps);
        }

        public void BeginFrame()
        {
            _frameStartNs = SDL3.SDL_GetTicksNS();
        }

        public void EndFrame()
        {
            ulong elapsedNs = SDL3.SDL_GetTicksNS() - _frameStartNs;

            if (elapsedNs < _targetFrameTimeNs)
            {
                ulong remainingNs = _targetFrameTimeNs - elapsedNs;

                if (remainingNs > SleepBufferNs)
                {
                    ulong sleepTime = remainingNs - SleepBufferNs;
                    SDL3.SDL_DelayNS(sleepTime);
                }

                while (SDL3.SDL_GetTicksNS() - _frameStartNs < _targetFrameTimeNs)
                {
                    Thread.SpinWait(10);
                }
            }
        }
    }
    public unsafe class DrawRectBatch
    {
        public static SDL_Renderer* Renderer { get; set; }
        public static SDL_Window* Window { get; set; }
        public static int WindowWidth { get; private set; }
        public static int WindowHeight { get; private set; }

        private SDL_Vertex[] _vertices;
        private int[] _indices;
        private int _vertexCount;
        private int _indexCount;

        private SDL_Texture* _currentTexture = null;

        const float bleedInset = 0.02f;

        public DrawRectBatch(SDL_Renderer* renderer, SDL_Window* window, int initialRectCapacity = 512)
        {
            Renderer = renderer;
            Window = window;

            _vertices = new SDL_Vertex[initialRectCapacity * 4];
            _indices = new int[initialRectCapacity * 6];
        }

        public void UpdateWindowSize()
        {
            int w = 0, h = 0;
            SDL3.SDL_GetWindowSize(Window, &w, &h);
            WindowWidth = w;
            WindowHeight = h;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin()
        {
            _vertexCount = 0;
            _indexCount = 0;
            _currentTexture = null;
        }

        public void Flush()
        {
            if (_vertexCount == 0) return;

            fixed (SDL_Vertex* pVertices = _vertices)
            fixed (int* pIndices = _indices)
            {
                SDL3.SDL_RenderGeometry(
                    Renderer,
                    _currentTexture, // bound texture, or null for solid shapes
                    pVertices,
                    _vertexCount,
                    pIndices,
                    _indexCount
                );
            }

            _vertexCount = 0;
            _indexCount = 0;
            _currentTexture = null;
        }

        public void Push(Element element, SDL_FlipMode flipMode = SDL_FlipMode.SDL_FLIP_NONE, float rotationDegrees = 0f)
        {
            PushInternal(element, null, default, 0, 0, hasTexture: false, rotationDegrees: rotationDegrees, flipMode: flipMode);
        }

        public void Push(Element element, SDL_Texture* texture, SDL_FRect srcRect, float texWidth, float texHeight, SDL_FlipMode flipMode = SDL_FlipMode.SDL_FLIP_NONE, float rotationDegrees = 0f)
        {
            if (texWidth <= 0 || texHeight <= 0)
            {
                throw new ArgumentException(
                    $"DrawRectBatch.Push: invalid texture dimensions ({texWidth}x{texHeight}). " +
                    "Texture likely failed to load or wasn't queried correctly.");
            }

            PushInternal(element, texture, srcRect, texWidth, texHeight, hasTexture: true, rotationDegrees: rotationDegrees, flipMode: flipMode);
        }

        private void PushInternal(Element element, SDL_Texture* texture, SDL_FRect srcRect, float texWidth, float texHeight, bool hasTexture, SDL_FlipMode? flipMode, float rotationDegrees = 0f)
        {
            // If texture changes mid-batch, flush existing geometry first.
            if (texture != _currentTexture)
            {
                Flush();
                _currentTexture = texture;
            }

            EnsureCapacity(1);

            UDim pos = element.Position;
            UDim sz = element.Size;

            if (pos.xScale != 0 || pos.yScale != 0)
                pos = UDim.CalcAbs(pos, WindowWidth, WindowHeight);
            if (sz.xScale != 0 || sz.yScale != 0)
                sz = UDim.CalcAbs(sz, WindowWidth, WindowHeight);

            float originX = Math.Clamp(element.Origin.x, 0, 1) * sz.xOffset;
            float originY = Math.Clamp(element.Origin.y, 0, 1) * sz.yOffset;

            float x0 = pos.xOffset - originX;
            float y0 = pos.yOffset - originY;
            float x1 = x0 + sz.xOffset;
            float y1 = y0 + sz.yOffset;

            SDL_FPoint p0, p1, p2, p3;
            if (element.Rotation != 0f || rotationDegrees != 0f) // Assuming element has Rotation in degrees
            {
                float rotation = element.Rotation != 0? element.Rotation : rotationDegrees;
                float cx = pos.xOffset;
                float cy = pos.yOffset;

                float rad = rotation * (MathF.PI / 180f);
                float cos = MathF.Cos(rad);
                float sin = MathF.Sin(rad);

                // Rotate the 4 corners
                p0 = RotatePoint(x0, y0, cx, cy, cos, sin); // Top-Left
                p1 = RotatePoint(x1, y0, cx, cy, cos, sin); // Top-Right
                p2 = RotatePoint(x1, y1, cx, cy, cos, sin); // Bottom-Right
                p3 = RotatePoint(x0, y1, cx, cy, cos, sin); // Bottom-Left
            }
            else
            {
                // Unrotated axis-aligned quad
                p0 = new() { x = x0, y = y0 };
                p1 = new() { x = x1, y = y0 };
                p2 = new() { x = x1, y = y1 };
                p3 = new() { x = x0, y = y1 };
            }

            float u0 = 0, v0 = 0, u1 = 0, v1 = 0;
            if (hasTexture)
            {
                u0 = (srcRect.x + bleedInset) / texWidth;
                v0 = (srcRect.y + bleedInset) / texHeight;
                u1 = (srcRect.x + srcRect.w - bleedInset) / texWidth;
                v1 = (srcRect.y + srcRect.h - bleedInset) / texHeight;
            }

            if (flipMode == SDL_FlipMode.SDL_FLIP_HORIZONTAL) (u1, u0) = (u0, u1);
            if (flipMode == SDL_FlipMode.SDL_FLIP_VERTICAL) (v1, v0) = (v0, v1);

            ColorRGBA c = element.Color;
            SDL_FColor color = new()
            {
                r = c.R / 255f,
                g = c.G / 255f,
                b = c.B / 255f,
                a = c.A / 255f
            };

            int baseVertex = _vertexCount;

            // Vertex layout matching UVs:
            // 0: Top-Left, 1: Top-Right, 2: Bottom-Right, 3: Bottom-Left
            _vertices[baseVertex + 0] = new SDL_Vertex { position = p0, color = color, tex_coord = new() { x = u0, y = v0 } };
            _vertices[baseVertex + 1] = new SDL_Vertex { position = p1, color = color, tex_coord = new() { x = u1, y = v0 } };
            _vertices[baseVertex + 2] = new SDL_Vertex { position = p2, color = color, tex_coord = new() { x = u1, y = v1 } };
            _vertices[baseVertex + 3] = new SDL_Vertex { position = p3, color = color, tex_coord = new() { x = u0, y = v1 } };

            int baseIndex = _indexCount;
            _indices[baseIndex + 0] = baseVertex + 0;
            _indices[baseIndex + 1] = baseVertex + 1;
            _indices[baseIndex + 2] = baseVertex + 2;
            _indices[baseIndex + 3] = baseVertex + 2;
            _indices[baseIndex + 4] = baseVertex + 3;
            _indices[baseIndex + 5] = baseVertex + 0;

            _vertexCount += 4;
            _indexCount += 6;
        }

        public void End()
        {
            Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int rectCountToAdd)
        {
            int neededVertices = _vertexCount + (rectCountToAdd * 4);
            int neededIndices = _indexCount + (rectCountToAdd * 6);

            if (neededVertices > _vertices.Length)
            {
                Array.Resize(ref _vertices, Math.Max(_vertices.Length * 2, neededVertices));
            }

            if (neededIndices > _indices.Length)
            {
                Array.Resize(ref _indices, Math.Max(_indices.Length * 2, neededIndices));
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SDL_FPoint RotatePoint(float px, float py, float cx, float cy, float cos, float sin)
        {
            float dx = px - cx;
            float dy = py - cy;
            return new SDL_FPoint
            {
                x = cx + (dx * cos - dy * sin),
                y = cy + (dx * sin + dy * cos)
            };
        }
    }
    public static class TextureHelper
    {
        public static unsafe SDL_Renderer* Renderer { get; set; }

        private static Dictionary<string, IntPtr> _textures = new();

        public static unsafe SDL_Texture* LoadTexture(string texturePath)
        {
            if (_textures.TryGetValue(texturePath, out IntPtr cachedPtr))
            {
                return (SDL_Texture*)cachedPtr;
            }

            SDL_Texture* texture = SDL3_image.IMG_LoadTexture(Renderer, texturePath);
            if (texture == null)
            {
                return null;
            }
            _textures[texturePath] = (IntPtr)texture;
            return texture;
        }

        public static unsafe void ReloadTexture(string texturePath)
        {
            if (_textures.TryGetValue(texturePath, out IntPtr oldPtr))
            {
                SDL3.SDL_DestroyTexture((SDL_Texture*)oldPtr);
            }

            SDL_Texture* newTexture = SDL3_image.IMG_LoadTexture(Renderer, texturePath);
            _textures[texturePath] = (IntPtr)newTexture;
        }

        public static unsafe void Cleanup()
        {
            foreach (IntPtr ptr in _textures.Values)
            {
                SDL3.SDL_DestroyTexture((SDL_Texture*)ptr);
            }
            _textures.Clear();
        }
    }
}