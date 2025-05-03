namespace Boii.Timing;

public class GenericTimer
{
    private bool _enabled = false;
    private long ticksLeft = 0;

    private GenericTimer() { }

    public static GenericTimer Create() => new();

    public bool Done => _enabled && ticksLeft <= 0;

    public void Start(ulong duration)
    {
        _enabled = true;
        ticksLeft = (long)duration;
    }

    public void Advance(ulong duration)
    {
        if (!_enabled)
            return;

        if (ticksLeft > 0)
            ticksLeft -= (int)duration;
    }

    public void Disable()
    {
        _enabled = false;
    }
}
