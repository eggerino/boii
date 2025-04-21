namespace Boii.Abstractions;

public interface IWritable
{
    void Write(ushort address, byte value);
}
