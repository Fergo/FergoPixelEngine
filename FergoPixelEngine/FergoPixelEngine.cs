using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FergoPixelEngine {
	abstract class FergoPixelEngine {
		private Graphics g;
		private Stopwatch sw = new Stopwatch();
		private SynchronizationContext sContext;
		private Form renderTarget;

		protected Bitmap screenBuffer32bpp;
		protected uint[] frameBuffer32bpp;

		public uint WindowWidth { get; private set; }
		public uint WindowHeight { get; private set; }
		public uint PixelSize { get; private set; }

		protected double FPS;
		protected double elapsedTime;

		//Setup the enviroment
		public void Create(Form RenderTarget, uint Width, uint Height, uint PixelSize) {
			float DPI;

			this.WindowWidth = Width;
			this.WindowHeight = Height;
			this.PixelSize = PixelSize;
			this.renderTarget = RenderTarget;

			//Sets the sync context for thread safe form update
			this.sContext = SynchronizationContext.Current;

			//This is our frame buffer, where everything is drawn into. It's a 32pp pixel array
			frameBuffer32bpp = new uint[WindowWidth * WindowHeight];

			//Sets the form as the render target
			g = renderTarget.CreateGraphics();
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

			//Create our bitmap that will be displayed in the form, mirroring the frame buffer
			screenBuffer32bpp = new Bitmap((int)WindowWidth, (int)WindowHeight, PixelFormat.Format32bppRgb);
			DPI = screenBuffer32bpp.HorizontalResolution;
			screenBuffer32bpp.SetResolution(DPI / this.PixelSize, DPI / this.PixelSize);

			//Resizes the form to the screen dimensions
			renderTarget.Size = new Size((int)(WindowWidth * this.PixelSize), (int)(WindowHeight * this.PixelSize));
		}

		//This method must be overriden by the derived class and contais all the game logic and rendering. It's called every frame.
		protected abstract void Update();

		//Run the main loop in a separate thread so it won't block UI
		public void Run() {
			Task.Run(() => MainLoop());
		}

		//Main game loop
		private void MainLoop() {
			while (true) {
				sw.Restart();

				//Call the overriden method containing all game logic
				Update();

				//"Swap Buffer"... copies the content from the screen array to the bitmap
				byte[] result = new byte[frameBuffer32bpp.Length * sizeof(int)];
				Buffer.BlockCopy(frameBuffer32bpp, 0, result, 0, result.Length);

				BitmapData bmpData = screenBuffer32bpp.LockBits(new Rectangle(0, 0, screenBuffer32bpp.Width, screenBuffer32bpp.Height), ImageLockMode.WriteOnly, screenBuffer32bpp.PixelFormat);
				Marshal.Copy(result, 0, bmpData.Scan0, result.Length);
				screenBuffer32bpp.UnlockBits(bmpData);

				//Update the FPS on the form in a thread safe way (todo: check performance impact)
				sContext.Send(new SendOrPostCallback((o) => {
					g.DrawImage(screenBuffer32bpp, 0, 0);
					renderTarget.Text = ((int)FPS).ToString();
				}), null);

				sw.Stop();

				//Calculate FPS
				elapsedTime = sw.Elapsed.TotalMilliseconds;
				FPS = 1000 / elapsedTime;
			}
		}

		//Clears the screen using multithreaded for loop
		protected void Clear(uint Color) {
			Parallel.For(0, WindowHeight, y => {
				for (int x = 0; x < WindowWidth; x++) {
					frameBuffer32bpp[y * WindowWidth + x] = Color;
				}
			});
		}

		//Draw a single pixel in the frame buffer. Inline this function if possible to reduce the call stack
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void DrawPixel(int x, int y, uint Color) {
			frameBuffer32bpp[y * WindowWidth + x] = Color;
		}

		//Draws a line from (x0,y0) to (x1, y1) using Bresenham's line algorithm
		protected void DrawLine(int X0, int Y0, int X1, int Y1, uint Color) {
			int dx = Math.Abs(X1 - X0);
			int dy = Math.Abs(Y1 - Y0);
			int sx = X0 < X1 ? 1 : -1;
			int sy = Y0 < Y1 ? 1 : -1;
			int err = (dx > dy ? dx : -dy) / 2;
			int e2;

			while (true) {
				DrawPixel(X0, Y0, Color);

				if (X0 == X1 && Y0 == Y1)
					break;

				e2 = err;

				if (e2 > -dx) {
					err -= dy;
					X0 += sx;
				}

				if (e2 < dy) {
					err += dx;
					Y0 += sy;
				}
			}
		}

		//Draws a rectangle outline
		protected void DrawRectangle(int TopLeftX, int TopLeftY, int BotRightX, int BotRightY, uint Color) {
			DrawLine(TopLeftX, TopLeftY, BotRightX, TopLeftY, Color);
			DrawLine(TopLeftX, BotRightY, BotRightX, BotRightY, Color);
			DrawLine(TopLeftX, TopLeftY, TopLeftX, BotRightY, Color);
			DrawLine(BotRightX, TopLeftY, BotRightX, BotRightY, Color);
		}

		//Draws a circle outline using mid-point circle algorithm
		protected void DrawCircle(int X0, int Y0, int Radius, uint Color) {
			int f = 1 - Radius;
			int ddFx = 0;
			int ddFy = -2 * Radius;
			int x = 0;
			int y = Radius;

			DrawPixel(X0, Y0 + Radius, Color);
			DrawPixel(X0, Y0 - Radius, Color);
			DrawPixel(X0 + Radius, Y0, Color);
			DrawPixel(X0 - Radius, Y0, Color);

			while (x < y) {
				if (f >= 0) {
					y--;
					ddFy += 2;
					f += ddFy;
				}

				x++;
				ddFx += 2;
				f += ddFx + 1;

				DrawPixel(X0 + x, Y0 + y, Color);
				DrawPixel(X0 - x, Y0 + y, Color);
				DrawPixel(X0 + x, Y0 - y, Color);
				DrawPixel(X0 - x, Y0 - y, Color);
				DrawPixel(X0 + y, Y0 + x, Color);
				DrawPixel(X0 - y, Y0 + x, Color);
				DrawPixel(X0 + y, Y0 - x, Color);
				DrawPixel(X0 - y, Y0 - x, Color);
			}
		}

		//Fills a rectangle
		protected void FillRectangle(int TopLeftX, int TopLeftY, int BotRightX, int BotRightY, uint Color) {
			for (int i = TopLeftY + 1; i < BotRightY; i++) {
				DrawLine(TopLeftX + 1, i, BotRightX - 1, i, Color);
			}
		}
	}
}
