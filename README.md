# Fergo Pixel Engine

This is a very basic implementation of a 32-bit pixel rendering plataform in C#. Currently supporting basic functions like DrawPixel, DrawLine, DrawRectangle, DrawCircle. I tried to make its usage as simple as possible, as described below.

The engine is not supposed to be fast. My main intentions with it is to mimic olcConsoleGameEngine from [javidx9](https://github.com/OneLoneCoder) but using a Windows Form instead of the console for output. I also want to keep it independent from external rendering libraries such as OpenGL and DirectX, so I'm sticking to the regular .NET drawing classes.

# Usage

1. Add `FergoPixelEngine.cs` to your project
2. Create a derived class from `FergoPixelEngine` and override the `Update()` method. All game logic and rendering should be done inside this method, which is called every frame in the game loop.

```C#
class MyEngine : FergoPixelEngine {
	protected override void Update() {
		Clear(0xff000000);

		DrawRectangle(100, 100, 10, 10, 0xffff0000);
		DrawCircle(150, 150, 25, 0xffff00ff);
		DrawLine(150, 10, 100, 50, 0xffffff00);
	}
}
```
  
3. Create an instance of your new class, set the window parameters and call the `Run()` method. Currenly, this should be done inside a form, as the class expects a Windows Form as the render target. The main loop runs in a separate thread so it won't lock the window UI.

```C#
private void frmMain_Load(object sender, EventArgs e) {
	MyEngine my = new MyEngine();

	my.Create(this, 320, 240, 2);
	my.Run();
}
```

# Documentation

```C#
Create(Form RenderTarget, int Width, int Height, int PixelSize)
```

This method is responsible for setting the rendering target, screen dimensions and pixel size. 

Currenty the engine only supports a Windows Form as its rendering target. The pixel size is used to emulate larger pixel sizes in the screen for a more old-school look if desired. At the moment I'm using some DPI tricks to get this done, but I plan on improving the drawing routines to get better performance and this will probably change in the future.

```C#
Run()
```

This simply starts the main game loop. It runs in a separate thread.

```C#
Update()
```

This is an abstract method that must overriden by the inherited class. This method is called by the engine at every frame, so this is where you are supposed to add all the game logic and rendering routines. 

# Drawing methods

```C#
Clear(uint Color)
```

Clears the screen with the specified color. Color must be a 32bit integer in ARGB format.

```C#
DrawPixel(int x, int y, uint Color)
```

Plots a single pixel to the screen at coordinates (x, y) with the specified colors. The (0,0) corrdinate is the top left corner of the window.

```C#
DrawLine(int X0, int Y0, int X1, int Y1, uint Color)
```

Draws a line from (x0,y0) to (x1, y1) using Bresenham's line algorithm.

```C#
DrawRectangle(int TopLeftX, int TopLeftY, int BotRightX, int BotRightY, uint Color)
```

Draws a rectangle outline from a top left to a bottom right coordinate.

```C#
DrawCircle(int X0, int Y0, int Radius, uint Color)
```

Draws a circle outline using mid-point circle algorithm.

```C#
FillRectangle(int TopLeftX, int TopLeftY, int BotRightX, int BotRightY, uint Color)
```

Fills a rectangle with the specified color.






