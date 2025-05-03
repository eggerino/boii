namespace Boii.Processing;

internal class InterruptMasterDispatcher
{
    private bool _value;

    private bool _target;
    private int _counter = -1;

    private InterruptMasterDispatcher(bool value) => (_value, _target) = (value, value);

    public static InterruptMasterDispatcher CreateWith(bool value) => new(value);

    public bool Value => _value;

    public void Enque(bool value, int numberInvokations)
    {
        _target = value;
        _counter = numberInvokations;
    }

    public void Force(bool value)
    {
        _value = value;
        _target = value;
        _counter = -1;
    }

    public void Update()
    {
        if (_counter < 0)
            return;

        if (_counter == 0)
            _value = _target;

        _counter--;
    }
}
