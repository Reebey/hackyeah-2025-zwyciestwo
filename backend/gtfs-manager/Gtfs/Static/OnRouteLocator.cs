namespace gtfs_manager.Gtfs.Static
{
    public sealed class OnRouteLocator
    {
        private readonly IMultiStaticIndexCache _multi;

        public OnRouteLocator(IMultiStaticIndexCache multi) => _multi = multi;

        public object RoutesOnRoute(
            double lat,
            double lon,
            double radiusMeters,
            IEnumerable<string> zipPaths,
            double? headingDeg // opcjonalnie: kierunek poruszania się użytkownika
        )
        {
            var indices = _multi.GetOrLoadMany(zipPaths);

            var results = new List<dynamic>();

            foreach (var (feedId, idx) in indices)
            {
                // Bez shapes — fallback na przystanki (mniej precyzyjne)
                bool hasShapes = idx.Shapes.Count > 0 && idx.TripShapeIds.Count > 0;

                if (hasShapes)
                {
                    // iteruj po shape_id używanych przez tripy; wyprowadź routeId przez trip
                    // optymalizacja: shapeId -> przykładowy trip -> routeId
                    var shapeToRoute = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var (tripId, shapeId) in idx.TripShapeIds)
                    {
                        if (!shapeToRoute.ContainsKey(shapeId) && idx.Trips.TryGetValue(tripId, out var tmeta))
                            shapeToRoute[shapeId] = tmeta.RouteId;
                    }

                    foreach (var (shapeId, poly) in idx.Shapes)
                    {
                        if (!shapeToRoute.TryGetValue(shapeId, out var routeId))
                            continue;

                        if (poly.Count < 2) continue;

                        var (distM, segBearing) = GeoExtensions.PointToPolylineMeters(lat, lon, poly);
                        if (distM <= radiusMeters)
                        {
                            double? bearingPenalty = null;
                            if (headingDeg is double h)
                            {
                                var diff = Math.Abs(NormAngle(h - segBearing));
                                // kara za różnicę kierunku (0..180) → mniejsza = lepsza
                                bearingPenalty = diff;
                            }

                            // prosty score: im bliżej i im mniejsza różnica bearing, tym lepiej
                            var score = distM + (bearingPenalty ?? 0) * 2.0; // 2 m per 1 deg (empirycznie)

                            idx.Routes.TryGetValue(routeId, out var rmeta);

                            results.Add(new
                            {
                                feedId,
                                routeId,
                                routeShortName = rmeta?.ShortName,
                                routeLongName = rmeta?.LongName,
                                distanceMeters = Math.Round(distM, 1),
                                segmentBearingDeg = Math.Round(segBearing, 1),
                                headingDeg = headingDeg,
                                score = Math.Round(score, 2),
                                method = "shape"
                            });
                        }
                    }
                }
                else
                {
                    // Fallback: przystanki w promieniu, potem routeIds z TripStopSeq
                    var nearStops = idx.Stops.Values
                        .Where(s => s.Lat is double slat && s.Lon is double slon)
                        .Select(s => new { s.StopId, s.Name, DistM = GeoExtensions.HaversineMeters(lat, lon, s.Lat!.Value, s.Lon!.Value) })
                        .Where(x => x.DistM <= radiusMeters)
                        .OrderBy(x => x.DistM)
                        .ToList();

                    if (nearStops.Count == 0) continue;

                    var nearStopIds = new HashSet<string>(nearStops.Select(s => s.StopId), StringComparer.OrdinalIgnoreCase);
                    var routeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var ts in idx.TripStopSeq.Values)
                    {
                        if (ts.StopIds.Any(nearStopIds.Contains) && idx.Trips.TryGetValue(ts.TripId, out var tmeta))
                            routeIds.Add(tmeta.RouteId);
                    }

                    foreach (var rid in routeIds)
                    {
                        idx.Routes.TryGetValue(rid, out var rmeta);
                        results.Add(new
                        {
                            feedId,
                            routeId = rid,
                            routeShortName = rmeta?.ShortName,
                            routeLongName = rmeta?.LongName,
                            distanceMeters = Math.Round(nearStops.First().DistM, 1),
                            segmentBearingDeg = (double?)null,
                            headingDeg = headingDeg,
                            score = Math.Round(nearStops.First().DistM, 1),
                            method = "stops"
                        });
                    }
                }
            }

            // Unikalność po (feedId, routeId), wybierz rekord z najlepszym (najmniejszym) score
            var unique = results
                .GroupBy(r => new { r.feedId, r.routeId })
                .Select(g => g.OrderBy(x => (double)x.score).First())
                .OrderBy(x => (double)x.score)
                .ThenBy(x => (string?)x.routeShortName ?? (string)x.routeId)
                .ToList();

            return new
            {
                query = new { lat, lon, radiusMeters, headingDeg },
                candidates = unique
            };

            static double NormAngle(double a)
            {
                a = (a + 360.0) % 360.0;
                if (a > 180.0) a -= 180.0; // interesuje nas 0..180 (różnica kierunku)
                return Math.Abs(a);
            }
        }
    }
}
