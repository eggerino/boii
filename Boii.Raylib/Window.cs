using Boii.Abstractions;
using Raylib_cs;
using RL = Raylib_cs.Raylib;
using RColor = Raylib_cs.Color;
using BColor = Boii.Abstractions.Color;
using SColor = System.Drawing.Color;
using System;

namespace Boii.Raylib;

public class Window : IRenderer, IGamepad
{
    private const int BaseWidth = 160;
    private const int BaseHeight = 144;

    private readonly string _title;
    private readonly int _scale;

    private readonly byte[] _image = new byte[BaseWidth * BaseHeight];
    private readonly RColor[] _colorMap = new RColor[4];

    private Window(string title, int scale) => (_title, _scale) = (title, scale);

    public static Window Create(string title, int scale, ColorTheme theme)
    {
        var window = new Window(title, scale);

        window._colorMap[(int)BColor.White] = ToRColor(theme.White);
        window._colorMap[(int)BColor.LightGray] = ToRColor(theme.LightGray);
        window._colorMap[(int)BColor.DarkGray] = ToRColor(theme.DarkGray);
        window._colorMap[(int)BColor.Black] = ToRColor(theme.Black);

        return window;
    }

    public void Open() => RL.InitWindow(_scale * BaseWidth, _scale * BaseHeight, _title);

    public bool ShouldClose() => RL.WindowShouldClose();

    public void Close() => RL.CloseWindow();

    public void SetPixel(int x, int y, BColor color) => _image[BaseWidth * y + x] = (byte)color;

    public void Update()
    {
        RL.BeginDrawing();
        for (var x = 0; x < BaseWidth; x++)
        {
            for (var y = 0; y < BaseHeight; y++)
            {
                var color = _colorMap[_image[BaseWidth * y + x]];
                RL.DrawRectangle(_scale * x, _scale * y, _scale, _scale, color);
            }
        }
        RL.DrawFPS(0, 0);
        RL.EndDrawing();
    }

    public bool Left => RL.IsKeyDown(KeyboardKey.A);
    public bool Right => RL.IsKeyDown(KeyboardKey.D);
    public bool Up => RL.IsKeyDown(KeyboardKey.W);
    public bool Down => RL.IsKeyDown(KeyboardKey.S);
    public bool A => RL.IsKeyDown(KeyboardKey.Enter);
    public bool B => RL.IsKeyDown(KeyboardKey.Space);
    public bool Start => RL.IsKeyDown(KeyboardKey.Backspace);
    public bool Select => RL.IsKeyDown(KeyboardKey.Delete);

    private static RColor ToRColor(SColor color) => new(color.R, color.G, color.B);
}
