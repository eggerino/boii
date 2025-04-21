namespace Boii.Abstractions;

public interface IWritable
{
    void Read(ushort address, byte value);
}
