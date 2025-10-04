using gtfs_manager.Gtfs.Realtime.Dtos;
using ProtoBuf;
using TransitRealtime;

namespace gtfs_manager.Gtfs.Realtime
{
    public sealed class RealtimeReader
    {
        private static FeedMessage Load(string path)
        {
            using var fs = File.OpenRead(path);
            return Serializer.Deserialize<FeedMessage>(fs); // protobuf-net deserialize
        }

        public IEnumerable<RtVehicleDto> ReadVehicles(string pbPath)
        {
            var feed = Load(pbPath);
            foreach (var e in feed.Entities)                 // <— Entities (plural)
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
            foreach (var e in feed.Entities)
            {
                if (e.TripUpdate == null) continue;
                var tu = e.TripUpdate;

                yield return new RtTripUpdateDto
                {
                    TripId = tu.Trip?.TripId,
                    RouteId = tu.Trip?.RouteId,
                    StopTimeUpdates = tu.StopTimeUpdates.Select(stu => new RtTripUpdateDto.StopTimeUpdateItem
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
            foreach (var e in feed.Entities)
            {
                if (e.Alert == null) continue;
                var a = e.Alert;

                yield return new RtAlertDto
                {
                    InformedEntity = a.InformedEntities
                        .Select(ie => ie.StopId ?? ie.RouteId ?? ie.AgencyId ?? string.Empty)
                        .Where(s => !string.IsNullOrWhiteSpace(s)),
                    Header = a.HeaderText?.Translations.FirstOrDefault()?.Text,
                    Description = a.DescriptionText?.Translations.FirstOrDefault()?.Text,
                    StartEpoch = a.ActivePeriods.FirstOrDefault()?.Start,
                    EndEpoch = a.ActivePeriods.FirstOrDefault()?.End
                };
            }
        }
    }
}
