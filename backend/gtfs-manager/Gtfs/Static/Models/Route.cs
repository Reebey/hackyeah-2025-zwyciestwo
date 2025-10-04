namespace gtfs_manager.Gtfs.Static.Models
{
    public sealed class Route
    {
        public string RouteId { get; set; } = "";
        public string? RouteShortName { get; set; }
        public string? RouteLongName { get; set; }
    }
}
