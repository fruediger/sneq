Could you please help me write a prompt for an agentic LLM in a code editor (VS Code)?

I have a developed custom bindings to SDL3 for C# and that's essentially a test run for them.

The thing the AI shall generate is just a simple clone of the well known game Snake.

Here are some requirements it must fulfill:
* It must generate the whole game from start to finish in one go. The expected result is a working clone of snake.
* It must use my API and my API only for developing the game. Of course it can use the .NET 10 SDK, but everything game-related should be driven by my API.
* It must respect the template/skeleton it was given (more on that below), but other than that is free to implement whatever it thinks is needed. It shall come up on its own with whatever is needed to make the game work.
* It shall implement the "standard" Snake rules. There are some rules that are some rules that are the bare minimum: Snake gets longer if it eats fruit. The pointer counter increases if a fruit is eaten. There are "death" walls surrounding the playable area.

Now there's a skeleton file which the AI should use as a basis to work off. I will point the AI to file. The AI shall not change how the game's overall lifetime works (which is called 'AppBase' lifetime model), it should rather use that as a framework to implement the game. The skeleton will be a single file-based C# app (which is a C#14/.NET10 feature). It is the single file the AI is allowed to modify and should modify in order to implement the game.

I will also provide a sprite atlas, called 'snake.png', that the AI should use in its game. It doesn't need to come up with how to load the file from disk. I'll embed the file into the resulting executable and I'll provide the AI with a method to load the sprite atlas resource as a texture. This method will be an extension method called "TryLoadEmbeddedTexture", ready to be used in the aforementioned skeleton.

Now let me describe the sprite atlas, so you can pass that on in the prompt you're generating:
The whole sprite atlas is 320 by 256 pixels with 5 columns and 4 rows of sprites. So each sprite is exactly 64 by 64 pixel. There is no additional padding to the sprites. The sprite atlas and the sprites use an alpha transparent background.
The sprite contents of the first row are: snake body turn right <-> down, snake body straight horizontal, snake body turn left <-> down, snake head looking upwards, snake head looking right
The sprite contents of the second row are: snake body turn right <-> up, blank, snake body straight vertical, snake head looking left, snake head looking down
The sprite contents of the third row are: blank, blank, snake body turn left <-> up, snake tail tip facing down, snake tail tip facing left
The sprite contents of the fourth row are: apple, blank, blank, snake tail tip facing right, snake tail tip facing up
When I say "snake tail tip facing somewhere", I mean that the tip is facing in that direction and the connecting edge to the body is the opposite direction.

Now there are some more requirements I have regarding gameplay and functionality:
* The snake shall be controllable by either using WASD or arrow keys.
* The score should be rendered visible to the user. For that the AI is allowed to use the "TryRenderDebugText" methods.
* The game should have a title screen, a game scene, and a game over screen displaying the results
* The window title should be appropriately updated with the game scene and potentially with the current score

The AI does not need to and shouldn't try implement sound effects (or any audio) or gamepad/joystick input handling as that's not yet implemented in the API!

To help the AI understand the API, I'll provide the API with the following:
* The GitHub repo that's containing the source code of the bindings - but that could be a little a hard to look through
	-> the url is https://github.com/Sdl3Sharp/Sdl3Sharp
* A link to the API documentation host on GitHub pages - but because of shortcomings of docfx, it's kinda broken and incomplete at the moment
	-> the url is https://sdl3sharp.github.io/Sdl3Sharp/api/Sdl3Sharp.html
* The xml file containing the xml API doc that was generated when I last build the bindings project - that might be the best place to look through, at least for an AI
	-> The file is named 'Sdl3Sharp.xml' and is within the same folder as the skeleton file, but I'll point the AI to that file as well

Other than all of that, the AI is free to do what it wants in order to implement the game.

The AI is free to add anything it likes in order to produce a well polished and well thought out application. The result should be as complete as possible.

The AI should take its time thinking about all of that and take its time coming up with a solution and implementing it. It's not about a quick and easy solution, it's about a flawlessly working and polished solution. It also should solve errors and warnings on its own. The result should be issue-free.

The AI should do its job without additional user input and tries its best to finish the work on its own. If the AI gets seriously stuck, it is okay for it to ask question to would help it getting further.

The prompt I ask you to generate should be very in-depth and very detailed. It should be elaborate enough to not be up to interpretation or too vaguely in some places aside from the explicitly mentioned freedoms I'll grant to the AI. Don't be afraid to generate a very long prompt, in fact that's what I'd actually prefer.

I just want to able to past your generated prompt into the AI chat window in VS Code and be good to go. Don't worry about the model used, I'm on GitHub Copilot Pro and will choose the best model myself (or maybe even just stick with 'auto').
