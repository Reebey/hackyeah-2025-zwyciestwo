using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Components.Routing;
using System.Globalization;
using System.IO.Compression;

namespace gtfs_manager.Gtfs.Static
{
    public sealed class StaticGtfsIndex
    {
        public sealed record TripMeta(string TripId, string RouteId, string? Headsign);
        public sealed record StopMeta(string StopId, string? Name, double? Lat, double? Lon);
        public sealed record TripStops(string TripId, IReadOnlyList<string> StopIds, IReadOnlyDictionary<string, int> SeqByStop);
        public sealed record RouteMeta(string RouteId, string? ShortName, string? LongName);
        public sealed record ShapePoint(double Lat, double Lon, int Seq);
        public Dictionary<string, List<ShapePoint>> Shapes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> TripShapeIds { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, RouteMeta> Routes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, TripMeta> Trips { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, StopMeta> Stops { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, TripStops> TripStopSeq { get; } = new(StringComparer.OrdinalIgnoreCase);

        public static StaticGtfsIndex FromZip(string zipPath)
        {
            using var zip = ZipFile.OpenRead(zipPath);
            var idx = new StaticGtfsIndex();

            // --- trips.txt ---
            foreach (var t in ReadCsvFromZip<TripRow>(zip, "trips.txt", new TripRowMap()))
            {
                idx.Trips[t.TripId] = new TripMeta(t.TripId, t.RouteId, t.TripHeadsign);
                if (!string.IsNullOrWhiteSpace(t.ShapeId))
                    idx.TripShapeIds[t.TripId] = t.ShapeId!;
            }

            // --- stops.txt ---
            foreach (var s in ReadCsvFromZip<StopRow>(zip, "stops.txt", new StopRowMap()))
            {
                idx.Stops[s.StopId] = new StopMeta(s.StopId, s.StopName, s.StopLat, s.StopLon);
            }

            // --- routes.txt ---
            foreach (var r in ReadCsvFromZip<RouteRow>(zip, "routes.txt", new RouteRowMap()))
            {
                idx.Routes[r.RouteId] = new RouteMeta(r.RouteId, r.RouteShortName, r.RouteLongName);
            }

            // --- stop_times.txt --- (zbierz sekwencje przystanków per trip)
            var seqByTrip = new Dictionary<string, List<(int seq, string stopId)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var st in ReadCsvFromZip<StopTimeRow>(zip, "stop_times.txt", new StopTimeRowMap()))
            {
                if (!seqByTrip.TryGetValue(st.TripId, out var list))
                {
                    list = new();
                    seqByTrip[st.TripId] = list;
                }
                list.Add((st.StopSequence, st.StopId));
            }

            foreach (var (tripId, list) in seqByTrip)
            {
                list.Sort((a, b) => a.seq.CompareTo(b.seq));
                var ordered = list.Select(x => x.stopId).ToArray();
                var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < ordered.Length; i++)
                    dict[ordered[i]] = i + 1; // 1-based sequence
                idx.TripStopSeq[tripId] = new TripStops(tripId, ordered, dict);
            }

            // --- shapes.txt ---
            try
            {
                var bag = new Dictionary<string, List<ShapePoint>>(StringComparer.OrdinalIgnoreCase);
                foreach (var sh in ReadCsvFromZip<ShapeRow>(zip, "shapes.txt", new ShapeRowMap()))
                {
                    if (!bag.TryGetValue(sh.ShapeId, out var list))
                    {
                        list = new();
                        bag[sh.ShapeId] = list;
                    }
                    list.Add(new ShapePoint(sh.ShapePtLat, sh.ShapePtLon, sh.ShapePtSequence));
                }

                // posortuj po sequence
                foreach (var (shapeId, list) in bag)
                {
                    list.Sort((a, b) => a.Seq.CompareTo(b.Seq));
                    idx.Shapes[shapeId] = list;
                }
            }
            catch (FileNotFoundException)
            {
                // część feedów nie ma shapes.txt — wtedy fallback na przystanki
            }

            return idx;
        }

        private static IEnumerable<T> ReadCsvFromZip<T>(ZipArchive zip, string entryName, ClassMap<T> map)
        {
            var entry = zip.GetEntry(entryName) ?? throw new FileNotFoundException(entryName);
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                DetectDelimiter = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null // ignoruj brakujące pola
            };
            using var csv = new CsvReader(reader, cfg);
            csv.Context.RegisterClassMap(map);
            foreach (var record in csv.GetRecords<T>())
                yield return record;
        }

        // ====== CSV modele + mapy ======

        private sealed class TripRow
        {
            public string RouteId { get; set; } = "";
            public string ServiceId { get; set; } = "";
            public string TripId { get; set; } = "";
            public string? TripHeadsign { get; set; }
            public string? ShapeId { get; set; }

        }

        private sealed class TripRowMap : ClassMap<TripRow>
        {
            public TripRowMap()
            {
                Map(m => m.RouteId).Name("route_id");
                Map(m => m.ServiceId).Name("service_id");
                Map(m => m.TripId).Name("trip_id");
                Map(m => m.TripHeadsign).Name("trip_headsign").Optional();
                Map(m => m.ShapeId).Name("shape_id").Optional();

            }
        }

        private sealed class StopRow
        {
            public string StopId { get; set; } = "";
            public string? StopName { get; set; }
            public double? StopLat { get; set; }
            public double? StopLon { get; set; }
        }

        private sealed class StopRowMap : ClassMap<StopRow>
        {
            public StopRowMap()
            {
                Map(m => m.StopId).Name("stop_id");
                Map(m => m.StopName).Name("stop_name").Optional();
                Map(m => m.StopLat).Name("stop_lat").Optional();
                Map(m => m.StopLon).Name("stop_lon").Optional();
            }
        }

        private sealed class StopTimeRow
        {
            public string TripId { get; set; } = "";
            public string StopId { get; set; } = "";
            public int StopSequence { get; set; }
            // Możesz dodać Arrival/Departure w przyszłości, tu nie są wymagane do indeksu
            public string? ArrivalTime { get; set; }
            public string? DepartureTime { get; set; }
        }

        private sealed class StopTimeRowMap : ClassMap<StopTimeRow>
        {
            public StopTimeRowMap()
            {
                Map(m => m.TripId).Name("trip_id");
                Map(m => m.StopId).Name("stop_id");
                Map(m => m.StopSequence).Name("stop_sequence");
                Map(m => m.ArrivalTime).Name("arrival_time").Optional();
                Map(m => m.DepartureTime).Name("departure_time").Optional();
            }
        }

        private sealed class RouteRow
        {
            public string RouteId { get; set; } = "";
            public string? RouteShortName { get; set; }
            public string? RouteLongName { get; set; }
        }

        private sealed class RouteRowMap : ClassMap<RouteRow>
        {
            public RouteRowMap()
            {
                Map(m => m.RouteId).Name("route_id");
                Map(m => m.RouteShortName).Name("route_short_name").Optional();
                Map(m => m.RouteLongName).Name("route_long_name").Optional();
            }
        }

        private sealed class ShapeRow
        {
            public string ShapeId { get; set; } = "";
            public double ShapePtLat { get; set; }
            public double ShapePtLon { get; set; }
            public int ShapePtSequence { get; set; }
        }

        private sealed class ShapeRowMap : ClassMap<ShapeRow>
        {
            public ShapeRowMap()
            {
                Map(m => m.ShapeId).Name("shape_id");
                Map(m => m.ShapePtLat).Name("shape_pt_lat");
                Map(m => m.ShapePtLon).Name("shape_pt_lon");
                Map(m => m.ShapePtSequence).Name("shape_pt_sequence");
            }
        }
    }
}
