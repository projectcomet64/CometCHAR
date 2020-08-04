namespace CometChar.Structs
{
    /// <summary>
    /// Information about the patch.
    /// </summary>
    //TODO: CMTP v0.2: make SegOffset become StartMarginOffset
    //Would allow to put the geolayout in absolutely any segment with no problems
    //as the initial node's offset would be recorded as well
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
