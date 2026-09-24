using Daybreak.Helpers;
using Daybreak.UI;
using SDL;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// GAME MAP HANDLER
/// </summary>
public class TilesetConfig
{
    [JsonPropertyName("texturePath")]
    public string TexturePath { get; set; } = "textures/basic-farming.png";

    [JsonPropertyName("tileWidth")]
    public float TileWidth { get; set; } = 16.0f;

    [JsonPropertyName("tileHeight")]
    public float TileHeight { get; set; } = 16.0f;

    [JsonPropertyName("textureWidth")]
    public float TextureWidth { get; set; } = 16.0f;

    [JsonPropertyName("textureHeight")]
    public float TextureHeight { get; set; } = 16.0f;

    [JsonPropertyName("tiles")]
    public Dictionary<string, TileCoord> Tiles { get; set; } = new();
}

public struct TileCoord
{
    [JsonPropertyName("x")] public int X { get; set; }
    [JsonPropertyName("y")] public int Y { get; set; }
}

public static unsafe class TilemapHandler
{
    public static IntPtr TileMapTexture { get; private set; }
    public static readonly SDL_FRect[] SpriteRects = new SDL_FRect[(int)SpriteId.Count];

    public static float TileWidth { get; private set; } = 16.0f;
    public static float TileHeight { get; private set; } = 16.0f;
    public static float TextureHeight { get; private set; } = 16.0f;
    public static float TextureWidth { get; private set; } = 16.0f;
    public static string CurrentTexturePath { get; private set; } = string.Empty;

    /// <summary>
    /// Initialize with any JSON tileset definition
    /// </summary>
    public static void Initialize(string configFilePath = @"textures/basic-farming.json")
    {
        if (!File.Exists(configFilePath))
        {
            Console.WriteLine($"[Error] Tileset config file not found: {configFilePath}");
            return;
        }

        string json = File.ReadAllText(configFilePath);
        var config = JsonSerializer.Deserialize<TilesetConfig>(json);
        if (config == null) return;

        TileWidth = config.TileWidth;
        TileHeight = config.TileHeight;
        TextureHeight = config.TextureHeight;
        TextureWidth = config.TextureWidth;
        CurrentTexturePath = config.TexturePath;

        // Load SDL Texture dynamically
        SDL_Texture* tex = TextureHelper.LoadTexture(config.TexturePath);
        if (tex == null)
        {
            Console.WriteLine($"[Error] Failed to load texture: {config.TexturePath}");
            return;
        }

        TileMapTexture = (IntPtr)tex;
        SDL3.SDL_SetTextureScaleMode(tex, SDL_ScaleMode.SDL_SCALEMODE_PIXELART);

        // Clear previous rects
        Array.Clear(SpriteRects, 0, SpriteRects.Length);

        // Dynamically plot all tile rects using Enum.TryParse
        foreach (var (spriteName, coord) in config.Tiles)
        {
            if (Enum.TryParse<SpriteId>(spriteName, out var spriteId) && (int)spriteId >= 0 && (int)spriteId < SpriteRects.Length)
            {
                SpriteRects[(int)spriteId] = new SDL_FRect
                {
                    x = coord.X * TileWidth,
                    y = coord.Y * TileHeight,
                    w = TileWidth,
                    h = TileHeight
                };
            }
        }
    }
}

/// <summary>
/// GAME MAP
/// </summary>
public enum SpriteId
{
    Empty = -1,

    // Characters (Row 0)
    Char_IdleDown = 0,
    Char_WalkDown1,
    Char_WalkDown2,
    Char_IdleUp,
    Char_WalkUp1,
    Char_WalkUp2,
    Char_IdleSide,
    Char_WalkSide1,
    Char_WalkSide2,

    // Terrain & Environment
    PlainGrass,          // Solid green (Row 5, Col 12)
    GrassFlowers,        // Green with tiny yellow flowers (Row 6, Col 1)
    TilledSoil,          // Dark brown soil center (Row 7, Col 4)
    DirtPath_Center,     // Sandy/dirt path
    Fence_HR,
    Fence_RH,
    Fence_R,
    Fence_H,             // Horizontal fence
    Fence_V,             // Vertical fence

    // Tools & Crops (Row 2, 3)
    Egg,
    Carrot,
    Cabbage,

    // Custom
    Tile_R7_C5,          // Auto mapped Row 7, Col 5
    Tile_R7_C3,          // Auto mapped Row 7, Col 3
    Tile_R6_C4,          // Auto mapped Row 6, Col 4
    Tile_R8_C4,          // Auto mapped Row 8, Col 4
    Tile_R6_C3,          // Auto mapped Row 6, Col 3
    Tile_R6_C5,          // Auto mapped Row 6, Col 5
    Tile_R8_C3,          // Auto mapped Row 8, Col 3
    Tile_R8_C5,          // Auto mapped Row 8, Col 5
    Tile_R3_C10,         // Auto mapped Row 3, Col 10
    Tile_R4_C5,          // Auto mapped Row 4, Col 5
    Tile_R7_C12,         // Auto mapped Row 7, Col 12
    Tile_R6_C12,         // Auto mapped Row 6, Col 12
    Tile_R6_C10,         // Auto mapped Row 6, Col 10

    Count
}
public static class MapData
{
    // Layer 0: Ground aliases
    private const SpriteId G = SpriteId.PlainGrass;
    private const SpriteId J = SpriteId.Tile_R6_C3;
    private const SpriteId L = SpriteId.Tile_R6_C4;
    private const SpriteId K = SpriteId.Tile_R6_C5;
    private const SpriteId I = SpriteId.Tile_R7_C3;
    private const SpriteId S = SpriteId.TilledSoil;
    private const SpriteId T = SpriteId.Tile_R7_C5;
    private const SpriteId M = SpriteId.Tile_R8_C3;
    private const SpriteId A = SpriteId.Tile_R8_C4;
    private const SpriteId N = SpriteId.Tile_R8_C5;

    // Layer 1: Object aliases
    private const SpriteId _ = SpriteId.Empty; // Transparent slot
    private const SpriteId O = SpriteId.Tile_R3_C10;
    private const SpriteId H = SpriteId.Fence_HR;
    private const SpriteId R = SpriteId.Fence_R;
    private const SpriteId X = SpriteId.Tile_R6_C12;
    private const SpriteId W = SpriteId.Fence_H;
    private const SpriteId D = SpriteId.Fence_RH;
    private const SpriteId Y = SpriteId.Tile_R6_C10;
    private const SpriteId U = SpriteId.Tile_R7_C12;

    // Base Terrain (drawn first)
    public static readonly SpriteId[,] GroundLayer =
    {
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, J, L, L, K, G, J, L, L, K, G, G, G, G, G, G, G, G, G, G },
        { G, I, S, S, T, G, I, S, S, T, G, G, G, G, G, G, G, G, G, G },
        { G, I, S, S, T, G, I, S, S, T, G, G, G, G, G, G, G, G, G, G },
        { G, M, A, A, N, G, M, A, A, N, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
        { G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G, G },
    };

    // Overlay Objects (drawn directly on top of ground)
    public static readonly SpriteId[,] ObjectLayer =
    {
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, O, _, O, H, R, R, R, R, X, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, W, _, _, _, _, W, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, W, _, _, _, _, W, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, W, _, _, _, _, W, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, W, _, _, _, _, W, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, D, R, R, Y, R, U, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
        { _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _ },
    };
}

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

        TextureHelper.Renderer = renderer;
        TilemapHandler.Initialize();

        // Game variables
        FrameLimiter limiter = new(targetFps: 120);
        DrawRectBatch batch1 = new(renderer, window);
        batch1.UpdateWindowSize();
        
        Button newButton = new(){ Position = UDim.UseScale(0.5f, 0.5f), Origin = new(){ x=0.5f, y=0.5f }, Size = UDim.UseScale(0.5f, 0.5f), Color = ColorRGBA.GREEN };
        EmptyElement newSprite = new(){ Position = UDim.UseScale(0f, 0f), Origin = new(){ x=0.5f, y=0.5f }, Size = UDim.UseOffset(64, 64), Color = ColorRGBA.WHITE };
        
        const float spriteSpeed = 200f;
        const float frameDuration = 0.2f;
        const int frameCount = 4;
        const float totalLoopDuration = frameDuration * frameCount;
        const float characterPosPadding = 32f;

        UDim spriteAbsPos = newSprite.Position;

        float spriteTimeAnimation = 0;
        bool spriteIsFlipped = false;
        SpriteId currentSpriteId = SpriteId.Char_IdleDown;
        SDL_Keycode? currentKeyPressed = null;
        UDim mapOffset = UDim.Zero;

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
                        spriteAbsPos.yOffset = Math.Clamp(spriteAbsPos.yOffset - (spriteSpeed * dt), characterPosPadding, ScreenHeight - characterPosPadding);
                        newSprite.Position = spriteAbsPos;
                        if (spriteAbsPos.yOffset <= characterPosPadding)
                        {
                            mapOffset.yOffset -= spriteSpeed * dt;
                        }

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
                        spriteAbsPos.xOffset = Math.Clamp(spriteAbsPos.xOffset - (spriteSpeed * dt), characterPosPadding, ScreenWidth - characterPosPadding);
                        newSprite.Position = spriteAbsPos;
                        spriteIsFlipped = true;
                        if (spriteAbsPos.xOffset <= characterPosPadding)
                        {
                            mapOffset.xOffset -= spriteSpeed * dt;
                        }

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
                        spriteAbsPos.yOffset = Math.Clamp(spriteAbsPos.yOffset + (spriteSpeed * dt), characterPosPadding, ScreenHeight - characterPosPadding);
                        newSprite.Position = spriteAbsPos;
                        if (spriteAbsPos.yOffset >= ScreenHeight - characterPosPadding)
                        {
                            mapOffset.yOffset += spriteSpeed * dt;
                        }

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
                        spriteAbsPos.xOffset = Math.Clamp(spriteAbsPos.xOffset + (spriteSpeed * dt), characterPosPadding, ScreenWidth - characterPosPadding);
                        newSprite.Position = spriteAbsPos;
                        spriteIsFlipped = false;
                        if (spriteAbsPos.xOffset >= ScreenWidth - characterPosPadding)
                        {
                            mapOffset.xOffset += spriteSpeed * dt;
                        }

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
                RenderMap(batch1, tileSize: 64f, camX: mapOffset.xOffset, camY: mapOffset.yOffset);
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
    public static unsafe void RenderMap(DrawRectBatch batch, float tileSize = 32.0f, float camX = 0, float camY = 0)
    {
        // 1. Guard against uninitialized texture
        if (TilemapHandler.TileMapTexture == IntPtr.Zero) return;

        int rows = MapData.GroundLayer.GetLength(0);
        int cols = MapData.GroundLayer.GetLength(1);

        // List of layers in the order they should be painted
        SpriteId[][,] layers = [MapData.GroundLayer, MapData.ObjectLayer];

        // Read dynamic texture dimensions loaded from JSON / SDL3
        float texWidth = TilemapHandler.TextureWidth;
        float texHeight = TilemapHandler.TextureHeight;
        var rects = TilemapHandler.SpriteRects;

        foreach (var currentLayer in layers)
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    SpriteId id = currentLayer[y, x];
                    int spriteIdx = (int)id;

                    // 2. Safe check: ignore Empty (-1) and out-of-bounds IDs
                    if (spriteIdx < 0 || spriteIdx >= rects.Length) continue;

                    SDL_FRect src = rects[spriteIdx];

                    // 3. Skip tiles not mapped in the JSON (width/height is 0)
                    if (src.w <= 0 || src.h <= 0) continue;

                    EmptyElement tileElem = new()
                    {
                        Position = new UDim { xOffset = (x * tileSize) - camX, yOffset = (y * tileSize) - camY },
                        Size = new UDim { xOffset = tileSize, yOffset = tileSize },
                        Color = new ColorRGBA { R = 255, G = 255, B = 255, A = 255 }
                    };

                    // 4. Dynamically pass the texture and its true dimensions!
                    batch.Push(
                        tileElem,
                        (SDL_Texture*)TilemapHandler.TileMapTexture,
                        src,
                        texWidth,
                        texHeight
                    );
                }
            }
        }
    }
}