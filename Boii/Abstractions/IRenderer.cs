namespace Boii.Abstractions;

public interface IRenderer
{
    void SetPixel(int x, int y, Color color);

    void Update();
}
