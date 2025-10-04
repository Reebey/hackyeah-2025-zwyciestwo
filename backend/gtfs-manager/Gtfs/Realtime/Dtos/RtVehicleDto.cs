namespace gtfs_manager.Gtfs.Realtime.Dtos
{
    public sealed class RtVehicleDto
    {
        public string? VehicleId { get; set; }
        public string? TripId { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
        public double? Bearing { get; set; }
        public double? Speed { get; set; }
        public long TimestampEpoch { get; set; }
    }
}
