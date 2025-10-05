using TransitRealtime;  // <- tak ma być (namespace z option csharp_namespace)
using Google.Protobuf;
using gtfs_manager.Gtfs.Realtime.Dtos;

namespace gtfs_manager.Gtfs.Realtime
{
    public sealed class RealtimeReader
    {
        private static FeedMessage Load(string path)
        {
            using var fs = File.OpenRead(path);
            FeedMessage feed = FeedMessage.Parser.ParseFrom(fs);
            return feed;
        }

        public IEnumerable<RtVehicleDto> ReadVehicles(string pbPath)
        {
            var feed = Load(pbPath);
            foreach (var e in feed.Entity)                 // <— Entities (plural)
            {
                if (e.Vehicle == null) continue;             // <— brak HasVehicle
                var v = e.Vehicle;

                yield return new RtVehicleDto
                {
                    VehicleId = v.Vehicle?.Id,
                    TripId = v.Trip?.TripId,
                    Lat = v.Position?.Latitude,              // float -> double? OK
                    Lon = v.Position?.Longitude,
                    Bearing = v.Position?.Bearing,
                    Speed = v.Position?.Speed,
                    TimestampEpoch = (long)v.Timestamp       // ulong -> long
                };
            }
        }

        public IEnumerable<RtTripUpdateDto> ReadTripUpdates(string pbPath)
        {
            var feed = Load(pbPath);
            foreach (var e in feed.Entity)
            {
                if (e.TripUpdate == null) continue;
                var tu = e.TripUpdate;

                yield return new RtTripUpdateDto
                {
                    TripId = tu.Trip?.TripId,
                    RouteId = tu.Trip?.RouteId,
                    StopTimeUpdates = tu.StopTimeUpdate.Select(stu => new RtTripUpdateDto.StopTimeUpdateItem
                    {
                        StopId = stu.StopId,
                        ArrivalDelaySec = stu.Arrival?.Delay,      // Arrival może być null
                        DepartureDelaySec = stu.Departure?.Delay,
                        ArrivalEpoch = stu.Arrival?.Time,
                        DepartureEpoch = stu.Departure?.Time
                    })
                };
            }
        }

        public IEnumerable<RtAlertDto> ReadAlerts(string pbPath)
        {
            var feed = Load(pbPath);
            foreach (var e in feed.Entity)
            {
                if (e.Alert == null) continue;
                var a = e.Alert;

                yield return new RtAlertDto
                {
                    InformedEntity = a.InformedEntity
                        .Select(ie => ie.StopId ?? ie.RouteId ?? ie.AgencyId ?? string.Empty)
                        .Where(s => !string.IsNullOrWhiteSpace(s)),
                    Header = a.HeaderText?.Translation.FirstOrDefault()?.Text,
                    Description = a.DescriptionText?.Translation.FirstOrDefault()?.Text,
                    StartEpoch = a.ActivePeriod.FirstOrDefault()?.Start,
                    EndEpoch = a.ActivePeriod.FirstOrDefault()?.End
                };
            }
        }
    }
}
