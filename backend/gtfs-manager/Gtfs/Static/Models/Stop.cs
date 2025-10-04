namespace gtfs_manager.Gtfs.Static.Models
{
    public sealed class Stop
    {
        public string StopId { get; set; } = "";
        public string? StopName { get; set; }
        public double? StopLat { get; set; }
        public double? StopLon { get; set; }
    }
}
