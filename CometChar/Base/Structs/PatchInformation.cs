namespace CometChar.Structs
{
    /// <summary>
    /// Information about the patch.
    /// </summary>
    public struct PatchInformation
    {
        public int versionMajor;
        public int versionMinor;
        public ushort Features;
        public uint compGeoLayoutLength;
        public uint compSegment04Length;
        public uint GeoLayoutStartMargin;
        public uint Segment04Length;
        public uint GeoLayoutLength;
        public uint GeoLayoutSegOffset;
    }
}
