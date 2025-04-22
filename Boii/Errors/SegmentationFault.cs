using System;

namespace Boii.Errors;

public class SegmentationFault : Exception
{
    public string Location { get; }
    public ushort Address { get; }

    private SegmentationFault(string location, ushort address) : base($"Segmentation Fault in {location} at 0x{address:X}") =>
        (Location, Address) = (location, address);

    public static SegmentationFault Create(string location, ushort address) => new(location, address);
}
