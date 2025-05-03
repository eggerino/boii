namespace Boii.Abstractions;

public interface IRenderer
{
    void ShowWindow();

    void SetPixel(int x, int y, Color color);

    void Update();

    void CloseWindow();
}
