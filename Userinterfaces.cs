using SDL;
using Daybreak.Helpers;
using System.Runtime.CompilerServices;

namespace Daybreak.UI
{
    public struct Vector2D
    {
        public float x;
        public float y;

        public static Vector2D Zero => new(){ x=0, y=0 };
    }
    public struct UDim
    {
        public float xScale;
        public float yScale;
        public float xOffset;
        public float yOffset;

        public static UDim UseOffset(float xOffset, float yOffset) => new(){xScale=0, yScale=0, xOffset=xOffset, yOffset=yOffset};
        public static UDim UseScale(float xScale, float yScale) => new(){xScale=xScale, yScale=yScale, xOffset=0, yOffset=0};
        public static UDim Zero() => new(){xScale=0, yScale=0, xOffset=0, yOffset=0};

        public static UDim CalcAbs(UDim udim, int sw, int sh) => new()
            {
                xScale = 0,
                yScale = 0,
                xOffset = udim.xOffset + (Math.Clamp(udim.xScale, 0, 1) * sw),
                yOffset = udim.yOffset + (Math.Clamp(udim.yScale, 0, 1) * sh),
            };
    }
    public struct ColorRGBA
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public static ColorRGBA WHITE => new(){ R = 255, G = 255, B = 255, A = 255 };
        public static ColorRGBA BLACK => new(){ R = 0, G = 0, B = 0, A = 255 };
        public static ColorRGBA RED => new(){ R = 255, G = 0, B = 0, A = 255 };
        public static ColorRGBA GREEN => new(){ R = 0, G = 255, B = 0, A = 255 };
        public static ColorRGBA BLUE => new(){ R = 0, G = 0, B = 255, A = 255 };
        
        public static implicit operator SDL_FColor(ColorRGBA color) => new(){ r = color.R, g = color.G, b = color.B, a = color.A};
        public static implicit operator ColorRGBA(SDL_FColor fcolor) => new(){ R = fcolor.r, G = fcolor.g, B = fcolor.b, A = fcolor.a};
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RgbaToUint(ColorRGBA color)
        {
            return ((uint)color.R << 24) | 
                ((uint)color.G << 16) | 
                ((uint)color.B << 8)  | 
                (uint)color.A;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColorRGBA UintToRgba(uint color)
        {
            return new()
            {
                R = (byte)((color >> 24) & 0xFF),
                G = (byte)((color >> 16) & 0xFF),
                B = (byte)((color >> 8) & 0xFF),
                A = (byte)(color & 0xFF)
            };
        }
    }

    public abstract unsafe class Element
    {
        public static SDL_Renderer* renderer { get; set; }
        public UDim Position { get; set; }
        public UDim Size { get; set; }
        public Vector2D Origin { get; set; }
        public ColorRGBA Color { get; set; }
        public float Rotation { get; set; }

        public abstract void Update(float dt);
        public abstract void HandleEvent(SDL_Event* evnt);
    }
    public class EmptyElement : Element
    {
        public override unsafe void HandleEvent(SDL_Event* evnt)
        {
            
        }
        public override void Update(float dt)
        {

        }
    }
    public class Button : Element
    {
        public override unsafe void HandleEvent(SDL_Event* evnt)
        {
            float x, y;
            SDL_MouseButtonFlags buttons = SDL3.SDL_GetMouseState(&x, &y);

            UDim pos = Position;
            UDim sz = Size;

            if (pos.xScale != 0 || pos.yScale != 0) 
                pos = UDim.CalcAbs(pos, DrawRectBatch.WindowHeight, DrawRectBatch.WindowWidth);
            if (sz.xScale != 0 || sz.yScale != 0) 
                sz = UDim.CalcAbs(sz, DrawRectBatch.WindowHeight, DrawRectBatch.WindowWidth);

            float originX = Math.Clamp(Origin.x, 0, 1) * sz.xOffset;
            float originY = Math.Clamp(Origin.y, 0, 1) * sz.yOffset;

            float x0 = pos.xOffset - originX;
            float y0 = pos.yOffset - originY;
            float x1 = x0 + sz.xOffset;
            float y1 = y0 + sz.yOffset;

            if (x > x0 && y > y0 && x < x1 && y < y1)
            {
                if (evnt->type == (uint)SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN){
                    Console.WriteLine("Button Down");
                    if (evnt->button.button == SDL3.SDL_BUTTON_LEFT)
                    {
                        Console.WriteLine("Clicked");
                    }
                }
            }
        }
        public override void Update(float dt)
        {

        }
    }
}