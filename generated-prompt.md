# Task: Implement a Complete Snake Game Clone

You are tasked with implementing a complete, polished, and fully working Snake game clone in a single C# file, using the **Sdl3Sharp** C# bindings for SDL3. Read every word of this prompt carefully before writing a single line of code. This is not a quick-and-dirty prototype — it must be a finished, issue-free product.

---

## Resources You Must Consult

Before writing any code, study the following resources thoroughly to understand the API you will be working with:

1. **Source code of the bindings** (the authoritative reference):  
   https://github.com/Sdl3Sharp/Sdl3Sharp  
   Pay particular attention to the following files/namespaces in `src/Sdl3Sharp/`:
   - `AppBase.cs` and `AppBase.Internal.cs` — the lifetime model
   - `Sdl.cs` and `Sdl.Builder.cs` — SDL initialization and the `Run` entry point
   - `AppResult.cs` — return value semantics
   - `SubSystems.cs` — subsystem flags
   - `Events/EventType.cs`, `Events/KeyboardEvent.cs`, `Events/WindowEvent.cs`, `Events/QuitEvent.cs`, `Events/Event.cs` (union struct) — event handling
   - `Input/Scancode.cs`, `Input/Keycode.cs` — key identifiers
   - `Video/Windowing/Window.cs` — window creation and the `Title` property
   - `Video/Rendering/Renderer.cs` and `Renderer_TDriver.cs` — rendering pipeline
   - `Video/Rendering/RendererExtensions.cs` — extension methods such as `TryRenderDebugText(in Point<float>, string)`
   - `Video/Rendering/Texture.cs` — texture management
   - `Timing/Timer.cs` — `Timer.MillisecondTicks` and `Timer.NanosecondTicks` static properties
   
2. **Official API documentation** (may be incomplete, but useful for browsing):  
   https://sdl3sharp.github.io/Sdl3Sharp/api/Sdl3Sharp.html

3. **XML API documentation file** (the single best machine-readable reference):  
   The file is named `Sdl3Sharp.xml` and is located in the **same folder as the skeleton file** you are modifying. You must open and read this file to understand the full API surface — method signatures, parameter names, types, and documented behavior.

4. **The skeleton file** (the single file you must modify):  
   You will be pointed to this file. It is a C# 14 / .NET 10 **top-level / single-file** application using the `AppBase` lifetime model. You must modify **only this file**. Do not create any additional source files.

---

## Technology Constraints

- **Target framework**: .NET 10 SDK, C# 14
- **API**: Use **Sdl3Sharp** exclusively for all window management, rendering, input handling, and timing. Do not use any other game framework, graphics library, or platform-specific API for game functionality.
- **Standard .NET**: You may freely use the full .NET 10 BCL (collections, math, random, etc.) as needed.
- **No audio**: Do not implement any audio or sound effects. The audio subsystem is not yet available. Do not reference it.
- **No gamepad/joystick**: Do not implement gamepad or joystick input. Do not reference those subsystems.
- **Single file**: All game code must live inside the single skeleton `.cs` file you are given. Do not modify `.csproj` files or create new files.

---

## The AppBase Lifetime Model — Do Not Deviate From It

The skeleton file already contains the correct scaffolding. Your job is to fill in the implementation **within** that scaffolding. You must **not** change how the app is started or how the lifetime model works.

The lifetime model works as follows (you will find the exact signatures in `AppBase.cs`):

```csharp
// Initialization — called once at startup
protected override AppResult OnInitialize(Sdl sdl, string[] args)

// Game loop — called every frame (repeatedly after OnInitialize returns Continue)
protected override AppResult OnIterate(Sdl sdl)

// Event handling — called once per event, possibly interleaved with OnIterate
protected override AppResult OnEvent(Sdl sdl, ref Event @event)

// Cleanup — called once before the app terminates, no matter what
protected override void OnQuit(Sdl sdl, AppResult result)
```

The return value semantics for `OnInitialize`, `OnIterate`, and `OnEvent`:
- `AppResult.Continue` — keep running
- `AppResult.Success` — clean exit (the app requested to close successfully)
- `AppResult.Failure` — exit with error

The protected static shorthand properties `Continue`, `Success`, and `Failure` are available on `AppBase` for convenience.

**Do not** add a `while(true)` loop, `Thread.Sleep`, or manual event polling. SDL manages all of this through the `AppBase` callbacks.

---

## SDL Initialization Pattern

The entry point in the skeleton file initializes SDL and runs the app like this:

```csharp
using var sdl = new Sdl(static builder => builder
    .SetAppName("Snake")
    .InitializeSubSystems(SubSystems.Video)
);
return sdl.Run(new SnakeApp(), args);
```

You may adjust the app name or add other builder options, but preserve the pattern. The `SubSystems.Video` flag is required. Do not add `SubSystems.Audio` or `SubSystems.Gamepad`/`SubSystems.Joystick`.

---

## Window and Renderer Setup

Inside `OnInitialize`, create the window and renderer using the appropriate `Window.TryCreate` and `Window.TryCreateRenderer` overloads (or the combined `Window.TryCreateWithRenderer` shorthand seen in the README example — check `Window.cs` for its signature).

A recommended window size for this game is **640 × 640 pixels** (or another sensible square size of your choosing). The window should not be resizable.

Store the `Window` and `Renderer` instances as fields on your `AppBase` subclass and dispose of them in `OnQuit`.

---

## Sprite Atlas — `snake.png`

A PNG sprite atlas named **`snake.png`** is embedded into the executable as a resource. You do not need to handle loading it from disk. A method called **`TryLoadEmbeddedTexture`** has already been added to the skeleton as an extension method on `Renderer`. Call it like this:

```csharp
if (!mRenderer.TryLoadEmbeddedTexture("snake.png", out mSpriteAtlas))
    return Failure;
```

The method returns `true` on success and populates the `out Texture` parameter. If it returns `false`, return `Failure` from `OnInitialize`.

### Sprite Atlas Layout

The atlas is **320 × 256 pixels** with a **5-column × 4-row** grid of sprites. Each sprite is exactly **64 × 64 pixels**. There is no padding between sprites and no border. The background is alpha-transparent.

You must compute sprite source rectangles yourself using the formula:
```
sourceX = column * 64
sourceY = row    * 64
width   = 64
height  = 64
```
where columns and rows are both 0-indexed.

#### Sprite Map (column, row) → content

| Row \ Col | 0 | 1 | 2 | 3 | 4 |
|-----------|---|---|---|---|---|
| **0** | Body turn: right↔down | Body straight horizontal | Body turn: left↔down | Head facing up | Head facing right |
| **1** | Body turn: right↔up | *(blank)* | Body straight vertical | Head facing left | Head facing down |
| **2** | *(blank)* | *(blank)* | Body turn: left↔up | Tail tip facing down | Tail tip facing left |
| **3** | Apple | *(blank)* | *(blank)* | Tail tip facing right | Tail tip facing up |

**Clarifications on "turns":**
- "Body turn right↔down" = the body segment connects the right side and the bottom side (i.e., the snake is turning from moving right to moving down, or from moving down to moving right).
- "Body turn left↔down" = connects the left side and the bottom side.
- "Body turn right↔up" = connects the right side and the top side.
- "Body turn left↔up" = connects the left side and the top side.

**Clarifications on "tail tip facing":**
- The tail tip is the very end of the snake's tail. "Facing" means the pointed/open end faces that direction; the other (closed/flat) end connects back to the body.
- Example: "Tail tip facing up" means the tip points upward — the snake is moving upward at its tail end.

You must correctly select and render the right sprite for each segment of the snake (head, body straights, body turns, tail) based on the actual direction the snake is travelling at that segment. Take care to correctly determine turn direction by looking at the directions of both the incoming and outgoing segment.

---

## Game Grid

The playable area is a discrete grid. Choose a cell size that fits neatly inside your window (e.g., **20 × 20 cells** with a 32-pixel cell size on a 640-pixel window). The entire window is used. There are no UI panels beside the play field — the score is rendered as an overlay on top of the game field.

**Death walls**: The border of the grid (all four edges) is treated as a wall. The playable area for the snake's head is strictly within the interior of the grid. If the head moves to a cell that is on the wall boundary (i.e., column 0, column max, row 0, or row max), or outside it, the snake dies. In other words, the "safe" zone is `[1, gridWidth-2]` × `[1, gridHeight-2]`.

Alternatively, you may render explicit wall tiles as the border row/column if you choose (using a solid colored rectangle or a tinted tile, since there is no explicit wall sprite in the atlas). The key requirement is that colliding with those border cells kills the snake.

---

## Game Rules

Implement standard Snake rules:

1. **Movement**: The snake moves one cell per tick in the current direction. The movement speed should be configurable via a constant (e.g., starting at ~8 ticks per second, i.e., one move every ~125 ms). Use `Timer.MillisecondTicks` to implement delta-time-based tick accumulation. Do **not** use SDL timer callbacks — just track elapsed time in `OnIterate` using `Timer.MillisecondTicks`.

2. **Direction input**: The player can change the snake's direction using either **WASD** or **Arrow keys**. Detect `EventType.KeyDown` events and read `@event.Key.Scancode`. The relevant `Scancode` values are: `Scancode.W`, `Scancode.A`, `Scancode.S`, `Scancode.D`, `Scancode.Up`, `Scancode.Down`, `Scancode.Left`, `Scancode.Right`.
   - The snake cannot reverse direction directly (e.g., if moving right, pressing left is ignored).
   - Queue the **most recently valid** input direction, applied only on the next game tick, so fast inputs aren't dropped but only one pending direction is held.

3. **Growth**: When the snake eats an apple, it grows by one segment on the next tick (the tail does not advance for that tick).

4. **Scoring**: The score starts at 0 and increments by 1 each time an apple is eaten. Display the score as an overlay on the game field (see the "Score Display" section below).

5. **Apple placement**: When the game starts and after each apple is eaten, a new apple is placed at a random empty cell (one that is not occupied by any part of the snake or by the wall boundary). If there are no empty cells (unlikely, but handle it gracefully), do not place a new apple.

6. **Death conditions**: The snake dies if its head:
   - Collides with the wall (border cells of the grid, as described above), or
   - Collides with its own body.

7. **Initial state**: The snake starts with a length of 3 segments (head + 2 body segments) placed horizontally in the center of the grid, moving to the right.

---

## Game Scenes / Screens

The game must have three distinct scenes, each rendering their own content:

### 1. Title Screen
- Displayed when the application first launches.
- Should display the game title ("**Snake**") prominently.
- Should display instructions such as "**Press any key or ENTER to start**" (or similar).
- Since `TryRenderDebugText` renders 8×8 pixel font characters, use `Renderer.Scale` (or `Renderer.LogicalSize`) if you want to scale the text up for visibility. Alternatively, render it at a larger implied scale by setting the renderer's logical presentation size. You may also draw colored rectangles as a background panel for the text.
- The title screen background should be a dark solid color (e.g., near-black).
- Pressing **any key** (including Enter, Space, or any direction key) on the title screen must transition to the Game Scene and start a fresh game.
- The window title should be set to **"Snake"** on this screen.

### 2. Game Scene
- The active gameplay screen.
- Renders the game grid, snake (with proper sprites), apple, wall indicators, and score overlay.
- **Wall rendering**: Draw the border cells in a visually distinct way (e.g., a filled rectangle in a dark green or gray color) to clearly delimit the death zone.
- **Score overlay**: In the top-left area of the screen (or wherever it is clearly visible), render the current score using `TryRenderDebugText`. Set the draw color to white (or another high-contrast color) before rendering the text. The score text should say something like `"Score: 5"`.
- The window title during gameplay should be updated to show the score, e.g.: `"Snake — Score: 5"`. Update it every time the score changes.
- The game pauses on the game-over condition and transitions to the Game Over screen.

### 3. Game Over Screen
- Displayed after the snake dies.
- Should render the final score prominently, e.g.: `"Game Over! Score: 12"`.
- Should display a prompt such as `"Press ENTER to play again or ESC to quit"`.
- The window title should be set to **"Snake — Game Over"**.
- Pressing **Enter** restarts the game (transition to Game Scene with a fresh game state).
- Pressing **Escape** exits the application (return `Success` from `OnEvent`).

---

## Score Display Details

Use `Renderer.TryRenderDebugText(float x, float y, string text)` or the extension method variant `TryRenderDebugText(in Point<float> point, string text)` from `RendererExtensions`. The debug font renders each character as 8×8 pixels at the default scale. You may set `Renderer.Scale` to `(2f, 2f)` or adjust `Renderer.LogicalSize` to make text more readable — just be consistent and make sure the text doesn't overlap the snake tiles in a way that is illegible. **Restore** any scale or transform changes after rendering text if they would affect sprite rendering.

Set `Renderer.DrawColorFloat` (or `Renderer.DrawColor`) to an appropriate color before calling `TryRenderDebugText`, since the function uses the current draw color for text.

---

## Rendering Pipeline Per Frame (OnIterate)

Each call to `OnIterate` must do the following in order:

1. Compute delta time using `Timer.MillisecondTicks` (store the last tick count as a `ulong` field, compute `delta = currentTicks - lastTicks`, update `lastTicks = currentTicks`).
2. Update game logic (advance movement timer, trigger game tick if enough time has elapsed, handle apple eating, check collision, etc.) — only when in the Game Scene and the game is running.
3. Clear the render target: `mRenderer.DrawColorFloat = (0.08f, 0.08f, 0.08f, 1f); mRenderer.TryClear();` (dark background).
4. Render the current scene (title, game, or game over).
5. Call `mRenderer.TryRenderPresent()` to display the frame.
6. Return `Continue`.

---

## Sprite Rendering Details

To render a sprite from the atlas, use:

```csharp
mRenderer.TryRenderTexture(
    destinationRect: new Rect<float>(destX, destY, cellSize, cellSize),
    texture:         mSpriteAtlas,
    sourceRect:      new Rect<float>(spriteCol * 64, spriteRow * 64, 64, 64)
);
```

Where `destX` and `destY` are the pixel position of the cell (i.e., `cellX * cellSize` and `cellY * cellSize`), and `spriteCol`/`spriteRow` identify the correct sprite.

Make sure alpha blending is enabled on the texture/renderer so the transparent atlas background does not overwrite the cell background. Use `Texture.BlendMode = BlendMode.Blend` (or equivalent) after loading the texture. Consult `Sdl3Sharp.xml` and the source for the exact property/method name.

---

## Input Handling in OnEvent

Inside `OnEvent`, dispatch on `@event.Type`:

```csharp
switch (@event.Type)
{
    case EventType.Quit:
    case EventType.WindowCloseRequested:
        return Success; // exit

    case EventType.KeyDown:
        HandleKeyDown(@event.Key.Scancode);
        break;
}
return Continue;
```

`@event.Key` accesses the `KeyboardEvent` union field. `@event.Key.Scancode` gives you the physical `Scancode`.

Do **not** process `EventType.KeyUp` for movement — only `KeyDown`.

---

## Resource Cleanup in OnQuit

In `OnQuit`, dispose all managed resources:

```csharp
protected override void OnQuit(Sdl sdl, AppResult result)
{
    mSpriteAtlas?.Dispose();
    mSpriteAtlas = null!;

    mRenderer?.Dispose();
    mRenderer = null!;

    mWindow?.Dispose();
    mWindow = null!;
}
```

---

## Code Quality Requirements

- The code must compile without **any errors** and without **any warnings**. Resolve all warnings before considering the implementation done.
- Use `nullable` reference types correctly (they may already be enabled in the project). Use `null!` only where strictly appropriate (e.g., temporary null assignment before `OnInitialize` sets up the field).
- Use C# 14 and .NET 10 features freely where they improve clarity (e.g., collection expressions, pattern matching, `is` patterns, primary constructors if appropriate, etc.).
- All game state must be encapsulated as private fields or local constants on the `AppBase` subclass (or in nested types defined within the same file).
- Define your sprite index constants (column/row pairs) as named readonly fields or a static helper so the rendering code is readable.
- Use a `Direction` enum with values `Up`, `Down`, `Left`, `Right` for the snake's movement direction.
- Use a `GameScene` (or `GameState`) enum with values `Title`, `Playing`, `GameOver` to track the current screen.
- The snake body should be stored as a `Queue<(int x, int y)>` or `LinkedList<(int x, int y)>` — whichever makes grow/advance logic cleanest.
- Use `System.Random.Shared` for random number generation (apple placement).

---

## Self-Review and Error Resolution

Before finishing, carefully review your implementation for:

1. **Compilation errors** — fix all of them.
2. **Nullable warnings** — ensure all nullable paths are handled.
3. **Logic correctness** — trace through the snake movement, growth, collision detection, and sprite selection logic mentally to verify correctness.
4. **Sprite correctness** — verify your mapping of direction pairs to turn sprites is accurate according to the atlas layout described above.
5. **Scene transitions** — verify that pressing Enter on the title screen, game over screen, and pressing Escape on the game over screen all work correctly.
6. **Resource leaks** — every resource obtained in `OnInitialize` must be disposed in `OnQuit`.
7. **Window title updates** — the title must be updated as specified for each scene.

If you encounter any issues you cannot resolve (API ambiguity, missing method, etc.), consult `Sdl3Sharp.xml` first. If truly stuck, ask a targeted, specific question.

---

## Summary Checklist

- [ ] Single-file C# 14 / .NET 10 app using the `AppBase` lifetime model (skeleton structure preserved)
- [ ] SDL initialized with `SubSystems.Video` only
- [ ] Window created (640×640 or similar, non-resizable)
- [ ] Renderer created, sprite atlas loaded via `TryLoadEmbeddedTexture("snake.png", out ...)`
- [ ] Alpha blending enabled on the atlas texture
- [ ] 3 game scenes: Title, Playing, GameOver
- [ ] Snake controlled with WASD and Arrow keys
- [ ] Direction change validation (no 180° reversal, input queuing)
- [ ] Snake growth on apple eat
- [ ] Score increments on apple eat
- [ ] Score rendered on screen during gameplay via `TryRenderDebugText`
- [ ] Window title updated per scene and reflects current score during gameplay
- [ ] Correct sprite rendering for head (4 directions), body straight (2 directions), body turn (4 variants), tail (4 directions), apple
- [ ] Wall death zone (border cells)
- [ ] Self-collision death
- [ ] Apple randomly placed on empty interior cell
- [ ] Restart on Enter from GameOver
- [ ] Quit on Escape from GameOver or on window close
- [ ] All resources disposed in `OnQuit`
- [ ] No compiler errors, no compiler warnings
- [ ] No audio, no gamepad/joystick code