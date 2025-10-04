namespace gtfs_manager.Gtfs.Realtime.Dtos
{
    public sealed class RtTripUpdateDto
    {
        public string? TripId { get; set; }
        public string? RouteId { get; set; }
        public IEnumerable<StopTimeUpdateItem> StopTimeUpdates { get; set; } = [];
        public sealed class StopTimeUpdateItem
        {
            public string? StopId { get; set; }
            public int? ArrivalDelaySec { get; set; }
            public int? DepartureDelaySec { get; set; }
            public long? ArrivalEpoch { get; set; }
            public long? DepartureEpoch { get; set; }
        }
    }
}
