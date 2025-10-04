namespace gtfs_manager.Gtfs.Static.Models
{
    public sealed class Trip
    {
        public string RouteId { get; set; } = "";
        public string ServiceId { get; set; } = "";
        public string TripId { get; set; } = "";
        public string? TripHeadsign { get; set; }
    }
}
