namespace gtfs_manager.Gtfs.Realtime.Dtos
{
    public sealed class RtVehicleEnrichedDto
    {
        public string? VehicleId { get; set; }
        public string? TripId { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
        public double? Bearing { get; set; }
        public double? Speed { get; set; }
        public long TimestampEpoch { get; set; }

        // Enrichment
        public string? RouteId { get; set; }
        public string? Headsign { get; set; }
        public string? NextStopId { get; set; }
        public string? NextStopName { get; set; }
        public uint? NextStopSequence { get; set; }
        public int? DelayMinutes { get; set; }      // jeśli podasz TripUpdates
        public long? EtaEpoch { get; set; }         // jeśli podasz TripUpdates z czasami
        public double? DistanceToNextStopKm { get; set; }
    }
}
