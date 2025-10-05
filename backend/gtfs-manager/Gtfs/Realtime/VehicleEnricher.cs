using gtfs_manager.Gtfs.Realtime.Dtos;
using gtfs_manager.Gtfs.Static;
using TransitRealtime;

namespace gtfs_manager.Gtfs.Realtime
{
    public sealed class VehicleEnricher
    {
        private readonly IStaticIndexCache _cache;
        private readonly RealtimeReader _reader;

        public VehicleEnricher(IStaticIndexCache cache, RealtimeReader reader)
        {
            _cache = cache;
            _reader = reader;
        }

        public IEnumerable<RtVehicleEnrichedDto> Enrich(string vehiclePositionsPb, string staticZip, string? tripUpdatesPb)
        {
            var idx = _cache.GetOrLoad(staticZip);

            // Optional: trip updates map (TripId -> next stop ETA/delay)
            Dictionary<string, (int? delayMin, long? eta, string? nextStopId, uint? nextSeq)>? tu = null;
            if (!string.IsNullOrWhiteSpace(tripUpdatesPb) && File.Exists(tripUpdatesPb))
            {
                tu = BuildTripUpdateMap(tripUpdatesPb!);
            }

            foreach (var v in _reader.ReadVehicles(vehiclePositionsPb))
            {
                var dto = new RtVehicleEnrichedDto
                {
                    VehicleId = v.VehicleId,
                    TripId = v.TripId,
                    Lat = v.Lat,
                    Lon = v.Lon,
                    Bearing = v.Bearing,
                    Speed = v.Speed,
                    TimestampEpoch = v.TimestampEpoch
                };

                if (v.TripId != null && idx.Trips.TryGetValue(v.TripId, out var meta))
                {
                    dto.RouteId = meta.RouteId;
                    dto.Headsign = meta.Headsign;

                    // next stop: prefer TripUpdates (ma next stop & seq), fallback: geolokacja do najbliższego kolejnego w TripStopSeq
                    if (idx.TripStopSeq.TryGetValue(v.TripId, out var seq))
                    {
                        // 1) TripUpdates first
                        if (tu != null && tu.TryGetValue(v.TripId, out var tuInfo))
                        {
                            dto.DelayMinutes = tuInfo.delayMin;
                            dto.EtaEpoch = tuInfo.eta;
                            dto.NextStopId = tuInfo.nextStopId;
                            dto.NextStopSequence = tuInfo.nextSeq;

                            if (dto.NextStopId != null && v.Lat is double lat && v.Lon is double lon &&
                                idx.Stops.TryGetValue(dto.NextStopId, out var s) && s.Lat is double slat && s.Lon is double slon)
                            {
                                dto.NextStopName = s.Name;
                                dto.DistanceToNextStopKm = Geo.HaversineKm(lat, lon, slat, slon);
                            }
                        }

                        // 2) Fallback: geodystans do najbliższego „jeszcze-nieodwiedzonego” przystanku
                        if (dto.NextStopId == null && v.Lat is double plat && v.Lon is double plon)
                        {
                            // Jeżeli VehiclePositions ma stop_id/sequence — wykorzystaj
                            // (w bindingach często jest CurrentStopSequence i StopId)
                            // Tu fallback: bierz najbliższy z całej sekwencji
                            string? bestStop = null;
                            double bestDist = double.MaxValue;
                            foreach (var stopId in seq.StopIds)
                            {
                                if (!idx.Stops.TryGetValue(stopId, out var sm) || sm.Lat is not double slat || sm.Lon is not double slon)
                                    continue;

                                var d = Geo.HaversineKm(plat, plon, slat, slon);
                                if (d < bestDist)
                                {
                                    bestDist = d;
                                    bestStop = stopId;
                                }
                            }

                            if (bestStop != null)
                            {
                                dto.NextStopId = bestStop;
                                dto.NextStopSequence = seq.SeqByStop.TryGetValue(bestStop, out var seqNo) ? (uint)seqNo : null;
                                if (idx.Stops.TryGetValue(bestStop, out var sm))
                                {
                                    dto.NextStopName = sm.Name;
                                    dto.DistanceToNextStopKm = bestDist;
                                }
                            }
                        }
                    }
                }

                yield return dto;
            }
        }

        private static Dictionary<string, (int? delayMin, long? eta, string? nextStopId, uint? nextSeq)> BuildTripUpdateMap(string tripUpdatesPbPath)
        {
            using var fs = File.OpenRead(tripUpdatesPbPath);
            var feed = FeedMessage.Parser.ParseFrom(fs);
            var map = new Dictionary<string, (int?, long?, string?, uint?)>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in feed.Entity)
            {
                var tu = e.TripUpdate;
                if (tu == null || string.IsNullOrWhiteSpace(tu.Trip?.TripId)) continue;

                // bierz pierwszy „przyszły” stop (StopTimeUpdate z Arrival/Departure w przyszłości) — proste MVP
                var next = tu.StopTimeUpdate.FirstOrDefault();
                int? delayMin = next?.Arrival?.Delay ?? next?.Departure?.Delay;
                if (delayMin.HasValue) delayMin = (int)Math.Round(delayMin.Value / 60.0);

                long? eta = next?.Arrival?.Time ?? next?.Departure?.Time;
                string? stopId = next?.StopId;

                map[tu.Trip.TripId] = (delayMin, eta, stopId, next?.StopSequence);
            }

            return map;
        }
    }
}
