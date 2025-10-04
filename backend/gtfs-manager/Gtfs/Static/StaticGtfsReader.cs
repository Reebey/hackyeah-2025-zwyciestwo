using CsvHelper;
using CsvHelper.Configuration;
using gtfs_manager.Gtfs.Static.Models;
using System.Globalization;
using System.IO.Compression;
using Route = gtfs_manager.Gtfs.Static.Models.Route;

namespace gtfs_manager.Gtfs.Static
{
    public sealed class StaticGtfsReader
    {
        public IEnumerable<Stop> ReadStopsFromZip(string zipPath)
            => ReadCsvFromZip<Stop>(zipPath, "stops.txt", map =>
            {
                map.Map(m => m.StopId).Name("stop_id");
                map.Map(m => m.StopName).Name("stop_name");
                map.Map(m => m.StopLat).Name("stop_lat").Optional();
                map.Map(m => m.StopLon).Name("stop_lon").Optional();
            });

        public IEnumerable<Route> ReadRoutesFromZip(string zipPath)
            => ReadCsvFromZip<Route>(zipPath, "routes.txt", map =>
            {
                map.Map(m => m.RouteId).Name("route_id");
                map.Map(m => m.RouteShortName).Name("route_short_name").Optional();
                map.Map(m => m.RouteLongName).Name("route_long_name").Optional();
            });

        public IEnumerable<Trip> ReadTripsFromZip(string zipPath)
            => ReadCsvFromZip<Trip>(zipPath, "trips.txt", map =>
            {
                map.Map(m => m.RouteId).Name("route_id");
                map.Map(m => m.ServiceId).Name("service_id");
                map.Map(m => m.TripId).Name("trip_id");
                map.Map(m => m.TripHeadsign).Name("trip_headsign").Optional();
            });

        private static IEnumerable<T> ReadCsvFromZip<T>(
            string zipPath,
            string entryName,
            Action<ClassMap<T>> configureMap)
        {
            using var zip = ZipFile.OpenRead(zipPath);
            var entry = zip.GetEntry(entryName) ?? throw new FileNotFoundException(entryName);

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                DetectDelimiter = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim
            };
            using var csv = new CsvReader(reader, cfg);

            var map = new DefaultClassMap<T>();
            configureMap(map);
            csv.Context.RegisterClassMap(map);

            foreach (var record in csv.GetRecords<T>())
                yield return record;
        }
    }

}
