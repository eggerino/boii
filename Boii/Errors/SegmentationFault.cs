using System;

namespace Boii.Errors;

public class SegmentationFault : Exception
{
    public string Location { get; }
    public ushort Address { get; }

    public SegmentationFault(string location, ushort address) : base($"Segmentation Fault in {location} at {address}") =>
        (Location, Address) = (location, address);
}
