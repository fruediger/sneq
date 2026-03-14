#:package Sdl3Sharp@0.0.1-test9

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Sdl3Sharp;
using Sdl3Sharp.Events;
using Sdl3Sharp.Input;
using Sdl3Sharp.IO;
using SdlTimer = Sdl3Sharp.Timing.Timer;
using Sdl3Sharp.Video;
using Sdl3Sharp.Video.Blending;
using Sdl3Sharp.Video.Coloring;
using Sdl3Sharp.Video.Drawing;
using Sdl3Sharp.Video.Rendering;
using Sdl3Sharp.Video.Windowing;

using var sdl = new Sdl(builder => builder
    .SetAppName("Snake")
    .InitializeSubSystems(SubSystems.Video)
);

return sdl.Run(new App(), args);

file enum Scene { Title, Playing, GameOver }

file enum Direction { Up, Down, Left, Right }

file sealed class App : AppBase
{
    const int CellSize = 32;
    const int GridWidth = 20;
    const int GridHeight = 20;
    const int WindowWidth = GridWidth * CellSize;   // 640
    const int WindowHeight = GridHeight * CellSize; // 640
    const int SpriteSize = 64; // sprite atlas cell size
    const float BaseMoveInterval = 150f; // ms between moves
    const float MinMoveInterval = 60f;
    const float SpeedUpPerApple = 2f;

    Window _window = null!;
    Renderer _renderer = null!;
    Texture _atlas = null!;

    Scene _scene = Scene.Title;
    int _score;
    int _highScore;

    // Snake state
    List<(int X, int Y)> _snake = [];
    Direction _direction;
    Direction _nextDirection;
    (int X, int Y) _apple;
    float _moveTimer;
    float _moveInterval;
    ulong _lastTick;
    bool _alive;

    // Random for apple placement
    Random _rng = new();

    protected override AppResult OnInitialize(Sdl sdl, string[] args)
    {
        if (!Window.TryCreateWithRenderer("Snake", WindowWidth, WindowHeight,
                out _window!, out _renderer!, WindowFlags.None))
            return Failure;

        if (!_renderer.TryLoadEmbeddedTexture("snake.png", out _atlas!))
            return Failure;

        _atlas.BlendMode = BlendMode.Blend;
        _lastTick = SdlTimer.MillisecondTicks;
        _scene = Scene.Title;

        return Continue;
    }

    protected override AppResult OnIterate(Sdl sdl)
    {
        ulong now = SdlTimer.MillisecondTicks;
        float dt = now - _lastTick;
        _lastTick = now;

        if (_scene == Scene.Playing && _alive)
        {
            _moveTimer += dt;
            while (_moveTimer >= _moveInterval)
            {
                _moveTimer -= _moveInterval;
                Tick();
            }
        }

        Render();
        return Continue;
    }

    protected override AppResult OnEvent(Sdl sdl, ref Event @event)
    {
        if (@event.Type == EventType.Quit || @event.Type == EventType.WindowCloseRequested)
            return Success;

        if (@event.Type == EventType.KeyDown)
        {
            var kb = (KeyboardEvent)@event;
            if (kb.IsRepeat) return Continue;
            var sc = kb.Scancode;

            switch (_scene)
            {
                case Scene.Title:
                    if (sc == Scancode.Return)
                        StartGame();
                    else if (sc == Scancode.Escape)
                        return Success;
                    break;

                case Scene.Playing:
                    if (sc is Scancode.W or Scancode.Up && _direction != Direction.Down)
                        _nextDirection = Direction.Up;
                    else if (sc is Scancode.S or Scancode.Down && _direction != Direction.Up)
                        _nextDirection = Direction.Down;
                    else if (sc is Scancode.A or Scancode.Left && _direction != Direction.Right)
                        _nextDirection = Direction.Left;
                    else if (sc is Scancode.D or Scancode.Right && _direction != Direction.Left)
                        _nextDirection = Direction.Right;
                    else if (sc == Scancode.Escape)
                        _scene = Scene.Title;
                    break;

                case Scene.GameOver:
                    if (sc is Scancode.Return or Scancode.Escape)
                        _scene = Scene.Title;
                    break;
            }
        }

        return Continue;
    }

    protected override void OnQuit(Sdl sdl, AppResult result)
    {
        _atlas?.Dispose();
        _renderer?.Dispose();
        _window?.Dispose();
    }

    void StartGame()
    {
        _snake.Clear();
        int cx = GridWidth / 2;
        int cy = GridHeight / 2;
        // Start with 3 segments, heading right
        _snake.Add((cx, cy));
        _snake.Add((cx - 1, cy));
        _snake.Add((cx - 2, cy));
        _direction = Direction.Right;
        _nextDirection = Direction.Right;
        _score = 0;
        _moveTimer = 0;
        _moveInterval = BaseMoveInterval;
        _alive = true;
        _scene = Scene.Playing;
        PlaceApple();
        _window.Title = "Snake";
    }

    void PlaceApple()
    {
        // Collect all empty cells (excluding border walls)
        var free = new List<(int, int)>();
        for (int y = 1; y < GridHeight - 1; y++)
            for (int x = 1; x < GridWidth - 1; x++)
                if (!_snake.Contains((x, y)))
                    free.Add((x, y));

        if (free.Count > 0)
            _apple = free[_rng.Next(free.Count)];
    }

    void Tick()
    {
        _direction = _nextDirection;

        var (hx, hy) = _snake[0];
        (hx, hy) = _direction switch
        {
            Direction.Up    => (hx, hy - 1),
            Direction.Down  => (hx, hy + 1),
            Direction.Left  => (hx - 1, hy),
            Direction.Right => (hx + 1, hy),
            _ => (hx, hy)
        };

        // Wall collision (border cells)
        if (hx <= 0 || hx >= GridWidth - 1 || hy <= 0 || hy >= GridHeight - 1)
        {
            Die();
            return;
        }

        // Self collision
        if (_snake.Contains((hx, hy)))
        {
            Die();
            return;
        }

        _snake.Insert(0, (hx, hy));

        if ((hx, hy) == _apple)
        {
            _score++;
            if (_score > _highScore)
                _highScore = _score;
            _moveInterval = Math.Max(MinMoveInterval, BaseMoveInterval - _score * SpeedUpPerApple);
            _window.Title = $"Snake  |  Score: {_score}";
            PlaceApple();
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }
    }

    void Die()
    {
        _alive = false;
        _scene = Scene.GameOver;
        _window.Title = $"Snake  |  Game Over  |  Score: {_score}";
    }

    // ── Rendering ────────────────────────────────────────────────────

    void Render()
    {
        _renderer.DrawColorFloat = Color.From(0.12f, 0.12f, 0.14f, 1f);
        _renderer.TryClear();

        switch (_scene)
        {
            case Scene.Title:    RenderTitle(); break;
            case Scene.Playing:  RenderPlaying(); break;
            case Scene.GameOver: RenderPlaying(); RenderGameOver(); break;
        }

        _renderer.TryRenderPresent();
    }

    void RenderTitle()
    {
        _renderer.DrawColorFloat = Color.From(0.3f, 0.85f, 0.3f, 1f);
        _renderer.TryRenderDebugText(WindowWidth / 2f - 5 * 8 / 2f, WindowHeight / 2f - 40, "SNAKE");

        _renderer.DrawColorFloat = Color.From(0.9f, 0.9f, 0.9f, 1f);
        _renderer.TryRenderDebugText(WindowWidth / 2f - 17 * 8 / 2f, WindowHeight / 2f,
            "Press ENTER to play");

        _renderer.DrawColorFloat = Color.From(0.6f, 0.6f, 0.6f, 1f);
        _renderer.TryRenderDebugText(WindowWidth / 2f - 18 * 8 / 2f, WindowHeight / 2f + 24,
            "WASD / Arrows: Move");

        if (_highScore > 0)
        {
            var hs = $"High Score: {_highScore}";
            _renderer.DrawColorFloat = Color.From(1f, 0.8f, 0.2f, 1f);
            _renderer.TryRenderDebugText(WindowWidth / 2f - hs.Length * 8 / 2f, WindowHeight / 2f + 60, hs);
        }
    }

    void RenderPlaying()
    {
        // Draw walls (border cells)
        _renderer.DrawColorFloat = Color.From(0.25f, 0.25f, 0.3f, 1f);
        for (int x = 0; x < GridWidth; x++)
        {
            _renderer.TryRenderFilledRect(new Rect<float>(x * CellSize, 0, CellSize, CellSize));
            _renderer.TryRenderFilledRect(new Rect<float>(x * CellSize, (GridHeight - 1) * CellSize, CellSize, CellSize));
        }
        for (int y = 1; y < GridHeight - 1; y++)
        {
            _renderer.TryRenderFilledRect(new Rect<float>(0, y * CellSize, CellSize, CellSize));
            _renderer.TryRenderFilledRect(new Rect<float>((GridWidth - 1) * CellSize, y * CellSize, CellSize, CellSize));
        }

        // Draw apple
        DrawSprite(0, 3, _apple.X, _apple.Y);

        // Draw snake
        for (int i = 0; i < _snake.Count; i++)
        {
            var (sx, sy) = GetSpriteForSegment(i);
            DrawSprite(sx, sy, _snake[i].X, _snake[i].Y);
        }

        // Score overlay
        _renderer.DrawColorFloat = Color.From(1f, 1f, 1f, 1f);
        _renderer.TryRenderDebugText(8, 8, $"Score: {_score}");
    }

    void RenderGameOver()
    {
        // Semi-transparent overlay
        _renderer.DrawBlendMode = BlendMode.Blend;
        _renderer.DrawColorFloat = Color.From(0f, 0f, 0f, 0.6f);
        _renderer.TryRenderFilledRect(new Rect<float>(0, 0, WindowWidth, WindowHeight));
        _renderer.DrawBlendMode = BlendMode.None;

        _renderer.DrawColorFloat = Color.From(0.9f, 0.2f, 0.2f, 1f);
        _renderer.TryRenderDebugText(WindowWidth / 2f - 9 * 8 / 2f, WindowHeight / 2f - 40, "GAME OVER");

        var scoreText = $"Score: {_score}";
        _renderer.DrawColorFloat = Color.From(1f, 1f, 1f, 1f);
        _renderer.TryRenderDebugText(WindowWidth / 2f - scoreText.Length * 8 / 2f, WindowHeight / 2f, scoreText);

        _renderer.DrawColorFloat = Color.From(0.7f, 0.7f, 0.7f, 1f);
        _renderer.TryRenderDebugText(WindowWidth / 2f - 22 * 8 / 2f, WindowHeight / 2f + 24,
            "Press ENTER to restart");
    }

    void DrawSprite(int spriteCol, int spriteRow, int gridX, int gridY)
    {
        var src = new Rect<float>(spriteCol * SpriteSize, spriteRow * SpriteSize, SpriteSize, SpriteSize);
        var dst = new Rect<float>(gridX * CellSize, gridY * CellSize, CellSize, CellSize);
        _renderer.TryRenderTexture(dst, _atlas, src);
    }

    // ── Sprite atlas mapping ─────────────────────────────────────────
    // Atlas layout (5×4, 64×64 per cell):
    //   Row 0: (0)turn R↔D  (1)horiz  (2)turn L↔D  (3)head Up  (4)head Right
    //   Row 1: (0)turn R↔U  (1)blank  (2)vert      (3)head Left (4)head Down
    //   Row 2: (0)blank     (1)blank  (2)turn L↔U  (3)tail Down (4)tail Left
    //   Row 3: (0)apple     (1)blank  (2)blank     (3)tail Right (4)tail Up

    (int Col, int Row) GetSpriteForSegment(int index)
    {
        if (index == 0) return GetHeadSprite();
        if (index == _snake.Count - 1) return GetTailSprite(index);
        return GetBodySprite(index);
    }

    (int, int) GetHeadSprite()
    {
        return _direction switch
        {
            Direction.Up    => (3, 0),
            Direction.Right => (4, 0),
            Direction.Left  => (3, 1),
            Direction.Down  => (4, 1),
            _ => (3, 0)
        };
    }

    (int, int) GetTailSprite(int index)
    {
        // Tail direction = from tail toward the segment before it
        var (tx, ty) = _snake[index];
        var (px, py) = _snake[index - 1];
        int dx = px - tx;
        int dy = py - ty;

        return (dx, dy) switch
        {
            (0, -1) => (3, 2), // body is above  → tail tip points down
            (0, 1)  => (4, 3), // body is below  → tail tip points up
            (-1, 0) => (3, 3), // body is left   → tail tip points right
            (1, 0)  => (4, 2), // body is right  → tail tip points left
            _ => (4, 3)
        };
    }

    (int, int) GetBodySprite(int index)
    {
        var (px, py) = _snake[index - 1]; // toward head
        var (cx, cy) = _snake[index];
        var (nx, ny) = _snake[index + 1]; // toward tail

        int dx1 = px - cx;
        int dy1 = py - cy;
        int dx2 = nx - cx;
        int dy2 = ny - cy;

        // Straight segments
        if ((dx1 != 0 && dx2 != 0) && dy1 == 0 && dy2 == 0)
            return (1, 0); // horizontal
        if ((dy1 != 0 && dy2 != 0) && dx1 == 0 && dx2 == 0)
            return (2, 1); // vertical

        // Corner/turn segments – determine which two directions connect
        // We want to map the pair of relative directions to the correct turn sprite.
        // Normalize: make a set of the two relative directions
        bool hasUp    = (dx1 == 0 && dy1 == -1) || (dx2 == 0 && dy2 == -1);
        bool hasDown  = (dx1 == 0 && dy1 == 1)  || (dx2 == 0 && dy2 == 1);
        bool hasLeft  = (dx1 == -1 && dy1 == 0) || (dx2 == -1 && dy2 == 0);
        bool hasRight = (dx1 == 1 && dy1 == 0)  || (dx2 == 1 && dy2 == 0);

        if (hasRight && hasDown)  return (0, 0); // turn R↔D
        if (hasLeft && hasDown)   return (2, 0); // turn L↔D
        if (hasRight && hasUp)    return (0, 1); // turn R↔U
        if (hasLeft && hasUp)     return (2, 2); // turn L↔U

        return (1, 0); // fallback horizontal
    }
}

file static class Extensions
{
    private static readonly Assembly mProgramAssembly = Assembly.GetAssembly(typeof(Program))!;
    private static readonly string mProgramAssemblyName = mProgramAssembly.GetName().Name!;

    extension(Renderer renderer)
    {
        public bool TryLoadEmbeddedTexture(string resourceName, [NotNullWhen(true)] out Texture? texture)
        {
            texture = null;

            var resourceNameSpan = resourceName.AsSpan().Trim();

            bool isBmp;
            if (resourceNameSpan.IsWhiteSpace()
                || !((isBmp = resourceNameSpan.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                    || resourceNameSpan.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            
            if (mProgramAssembly.GetManifestResourceStream($"{mProgramAssemblyName}.{resourceNameSpan}") is not {} resourceStream)
            {
                return false;
            }

            using var resourceSdlStream = resourceStream.ToSdlStream(leaveOpen: false);
            
            Surface surface;
            if (isBmp)
            {
                if (!Surface.TryLoadBmp(resourceSdlStream, out surface!))
                {
                    return false;
                }
            }
            else
            {
                if (!Surface.TryLoadPng(resourceSdlStream, out surface!))
                {
                    return false;
                }
            }

            using (surface)
            {
                if (!renderer.TryCreateTextureFromSurface(surface, out texture!))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
