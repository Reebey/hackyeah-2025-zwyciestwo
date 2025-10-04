namespace gtfs_manager.Gtfs.Realtime.Dtos
{
    public sealed class RtAlertDto
    {
        public IEnumerable<string> InformedEntity { get; set; } = [];
        public string? Header { get; set; }
        public string? Description { get; set; }
        public ulong? StartEpoch { get; set; }
        public ulong? EndEpoch { get; set; }
    }
}
