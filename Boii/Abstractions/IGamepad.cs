namespace Boii.Abstractions;

public interface IGamepad
{
    bool Left { get; }
    bool Right { get; }
    bool Up { get; }
    bool Down { get; }
    bool A { get; }
    bool B { get; }
    bool Start { get; }
    bool Select { get; }
}
