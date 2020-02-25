namespace CometChar.Structs
{
    /// <summary>
    /// Information about the Geo Layout's length.
    /// Length is its length in bytes, StartMargin is where the information has been found the earliest from jumps.
    /// </summary>
    public struct GeoLayoutInformation
    {
        public long Length;
        public long StartMargin;
    }
}
