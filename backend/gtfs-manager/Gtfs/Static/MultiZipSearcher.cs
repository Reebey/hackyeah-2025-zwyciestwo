namespace gtfs_manager.Gtfs.Static
{
    public sealed class MultiZipSearcher
    {
        private readonly IMultiStaticIndexCache _multi;

        public MultiZipSearcher(IMultiStaticIndexCache multi) => _multi = multi;

        public object RoutesByPoint(double lat, double lon, double radiusMeters, IEnumerable<string> zipPaths)
        {
            var indices = _multi.GetOrLoadMany(zipPaths);

            var aggregatedRoutes = new List<object>();
            var nearbyStopsAgg = new List<object>();

            foreach (var (feedId, idx) in indices)
            {
                var nearStops = idx.Stops.Values
                    .Where(s => s.Lat is double slat && s.Lon is double slon)
                    .Select(s =>
                    {
                        var d = Geo.HaversineKm(lat, lon, s.Lat!.Value, s.Lon!.Value) * 1000.0;
                        return new { s.StopId, s.Name, DistanceM = d };
                    })
                    .Where(x => x.DistanceM <= radiusMeters)
                    .OrderBy(x => x.DistanceM)
                    .ToList();

                if (nearStops.Count == 0) continue;

                nearbyStopsAgg.AddRange(nearStops.Select(s => new {
                    feedId,
                    s.StopId,
                    s.Name,
                    distanceMeters = Math.Round(s.DistanceM, 1)
                }));

                var routeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var ts in idx.TripStopSeq.Values)
                {
                    foreach (var stopId in ts.StopIds)
                    {
                        // jeśli którykolwiek stop w sekwencji jest „near”
                        if (nearStops.Any(n => string.Equals(n.StopId, stopId, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (idx.Trips.TryGetValue(ts.TripId, out var meta))
                                routeIds.Add(meta.RouteId);
                            break;
                        }
                    }
                }

                aggregatedRoutes.AddRange(routeIds.Select(rid =>
                {
                    idx.Routes.TryGetValue(rid, out var rmeta);
                    return new
                    {
                        feedId,
                        routeId = rid,
                        routeShortName = rmeta?.ShortName,
                        routeLongName = rmeta?.LongName
                    };
                }));
            }

            // unikalność po (feedId, routeId)
            var uniqueRoutes = aggregatedRoutes
                .Cast<dynamic>()
                .GroupBy(r => new { r.feedId, r.routeId }, r => r)
                .Select(g => g.First())
                .OrderBy(r => (string?)r.routeShortName ?? (string)r.routeId)
                .ToList();

            return new
            {
                query = new { lat, lon, radiusMeters },
                routes = uniqueRoutes,
                nearbyStops = nearbyStopsAgg
            };
        }
    }
}
