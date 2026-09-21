using Daybreak.Helpers;
using Daybreak.UI;
using SDL;

public class Program
{
    const int ScreenHeight = 720;
    const int ScreenWidth = 1280;

    public unsafe static void Main()
    {
        if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO)) {Console.WriteLine("Cant initiate SDL"); return;}
        if (!SDL3_ttf.TTF_Init()) {Console.WriteLine("Cant initiate text rendering"); return;}

        SDL_Window* window = SDL3.SDL_CreateWindow("Daybreak", ScreenWidth, ScreenHeight, SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
        if (window == null) {Console.WriteLine("Cant create window"); SDL3.SDL_Quit(); return;}

        SDL_Renderer* renderer = SDL3.SDL_CreateRenderer(window, string.Empty);
        if (renderer == null) {Console.WriteLine("Cant create renderer"); SDL3.SDL_Quit(); return;}
        SDL3.SDL_SetRenderDrawBlendMode(renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);

        ulong lastTick = SDL3.SDL_GetTicksNS();
        ulong last_counter = SDL3.SDL_GetPerformanceCounter();
        ulong frequency = SDL3.SDL_GetPerformanceFrequency();

        TextureHelper.Renderer = renderer;
        TilemapHandler.Initialize();

        // Game variables
        FrameLimiter limiter = new(targetFps: 120);
        DrawRectBatch batch1 = new(renderer, window);
        batch1.UpdateWindowSize();
        
        Button newButton = new(){ Position = UDim.UseScale(0.5f, 0.5f), Origin = new(){ x=0.5f, y=0.5f }, Size = UDim.UseScale(0.5f, 0.5f), Color = ColorRGBA.GREEN };
        EmptyElement newSprite = new(){ Position = UDim.UseScale(0.5f, 0.5f), Origin = new(){ x=0.5f, y=0.5f }, Size = UDim.UseOffset(128, 128), Color = ColorRGBA.WHITE };
        
        const float frameDuration = 0.2f;
        const int frameCount = 4;
        const float totalLoopDuration = frameDuration * frameCount;

        float spriteTimeAnimation = 0;
        bool spriteIsFlipped = false;
        SpriteId currentSpriteId = SpriteId.Char_IdleDown;
        SDL_Keycode? currentKeyPressed = null;

        // Main loop
        bool isRunning = true;
        SDL_Event evnt;
        while (isRunning)
        {
            // Input Loop
            while (SDL3.SDL_PollEvent(&evnt))
            {
                newButton.HandleEvent(&evnt);
                switch (evnt.type)
                {
                    case (uint)SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                        batch1.UpdateWindowSize();
                        break;

                    case (uint)SDL_EventType.SDL_EVENT_QUIT:
                        isRunning = false;
                        break;

                    case (uint)SDL_EventType.SDL_EVENT_KEY_DOWN:
                        switch (evnt.key.key)
                        {
                            case SDL_Keycode.SDLK_SPACE:
                                Console.WriteLine("Space is Pressed");
                                break;
                            default:
                                if (currentKeyPressed is null) currentKeyPressed = evnt.key.key;
                                break;
                        }
                        break;
                    
                    case (uint)SDL_EventType.SDL_EVENT_KEY_UP:
                        if (currentKeyPressed is not null) currentKeyPressed = null;
                        break;
                }
            }

            // Update Loop
            ulong currentTick = SDL3.SDL_GetTicksNS();
            float dt = (float)((currentTick - lastTick) / 1_000_000_000.0);
            lastTick = currentTick;

            if (currentKeyPressed is not null)
            {
                spriteTimeAnimation += (float)dt;
                spriteTimeAnimation %= totalLoopDuration;
                int frameIndex = (int)(spriteTimeAnimation / frameDuration);
                switch (currentKeyPressed)
                {
                    case SDL_Keycode.SDLK_W:
                        currentSpriteId = frameIndex switch
                        {
                            0 => SpriteId.Char_WalkUp1,
                            1 => SpriteId.Char_IdleUp,
                            2 => SpriteId.Char_WalkUp2,
                            3 => SpriteId.Char_IdleUp,
                            _ => SpriteId.Char_IdleUp
                        };
                        break;
                    case SDL_Keycode.SDLK_A:
                        spriteIsFlipped = true;
                        currentSpriteId = frameIndex switch
                        {
                            0 => SpriteId.Char_WalkSide1,
                            1 => SpriteId.Char_IdleSide,
                            2 => SpriteId.Char_WalkSide2,
                            3 => SpriteId.Char_IdleSide,
                            _ => SpriteId.Char_IdleSide
                        };
                        break;
                    case SDL_Keycode.SDLK_S:
                        currentSpriteId = frameIndex switch
                        {
                            0 => SpriteId.Char_WalkDown1,
                            1 => SpriteId.Char_IdleDown,
                            2 => SpriteId.Char_WalkDown2,
                            3 => SpriteId.Char_IdleDown,
                            _ => SpriteId.Char_IdleDown
                        };
                        break;
                    case SDL_Keycode.SDLK_D:
                        spriteIsFlipped = false;
                        currentSpriteId = frameIndex switch
                        {
                            0 => SpriteId.Char_WalkSide1,
                            1 => SpriteId.Char_IdleSide,
                            2 => SpriteId.Char_WalkSide2,
                            3 => SpriteId.Char_IdleSide,
                            _ => SpriteId.Char_IdleSide
                        };
                        break;
                }
            };

            newButton.Update((float)dt);

            // Draw Loop
            limiter.BeginFrame();
            SDL3.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL3.SDL_RenderClear(renderer);

            SDL_FRect srcRectChar = TilemapHandler.SpriteRects[
                currentKeyPressed is null ? (int)SpriteId.Char_IdleDown :
                (int)currentSpriteId
            ];

            batch1.Begin();
            batch1.Push(newButton);
            batch1.Push(newSprite, (SDL_Texture*)TilemapHandler.TileMapTexture, srcRectChar, 208f, 208f, flipMode: spriteIsFlipped? SDL_FlipMode.SDL_FLIP_NONE : SDL_FlipMode.SDL_FLIP_HORIZONTAL);
            batch1.End();

            // SDL3.SDL_RenderTexture(renderer, (SDL_Texture*)TilemapHandler.TileMapTexture, &srcRect, &dstRect);

            SDL3.SDL_RenderPresent(renderer);
            limiter.EndFrame();
        }

        TextureHelper.Cleanup();
        SDL3.SDL_DestroyRenderer(renderer);
        SDL3.SDL_DestroyWindow(window);
        SDL3.SDL_Quit();
    }

    public enum SpriteId
    {
        Char_IdleDown,
        Char_WalkDown1,
        Char_WalkDown2,

        Char_IdleUp,
        Char_WalkUp1,
        Char_WalkUp2,

        Char_IdleSide,
        Char_WalkSide1,
        Char_WalkSide2,

        Water,
        Count
    }
    public static unsafe class TilemapHandler
    {
        public static IntPtr TileMapTexture { get; private set; }
        public static readonly SDL_FRect[] SpriteRects = new SDL_FRect[(int)SpriteId.Count];

        private const float _srcRectHeight = 16.0f;
        private const float _srcRectWidth = 16.0f;

        public static void Initialize()
        {
            SDL_Texture* tex = TextureHelper.LoadTexture(@"textures/basic-farming.png");
            if (tex == null) return;

            TileMapTexture = (IntPtr)tex;
            SDL3.SDL_SetTextureScaleMode(tex, SDL_ScaleMode.SDL_SCALEMODE_PIXELART);

            // Plot Tilemaps
            SpriteRects[(int)SpriteId.Char_IdleDown] = GetTileRect(0, 0);
            SpriteRects[(int)SpriteId.Char_WalkDown1] = GetTileRect(1, 0);
            SpriteRects[(int)SpriteId.Char_WalkDown2] = GetTileRect(2, 0);

            SpriteRects[(int)SpriteId.Char_IdleUp] = GetTileRect(4, 0);
            SpriteRects[(int)SpriteId.Char_WalkUp1] = GetTileRect(3, 0);
            SpriteRects[(int)SpriteId.Char_WalkUp2] = GetTileRect(5, 0);

            SpriteRects[(int)SpriteId.Char_IdleSide] = GetTileRect(6, 0);
            SpriteRects[(int)SpriteId.Char_WalkSide1] = GetTileRect(7, 0);
            SpriteRects[(int)SpriteId.Char_WalkSide2] = GetTileRect(8, 0);
        }

        private static SDL_FRect GetTileRect(int tileX, int tileY) => new SDL_FRect
        {
            x = tileX * _srcRectWidth,
            y = tileY * _srcRectHeight,
            w = _srcRectWidth,
            h = _srcRectHeight
        };
    }
}