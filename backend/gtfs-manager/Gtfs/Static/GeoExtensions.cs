namespace gtfs_manager.Gtfs.Static
{
    public static class GeoExtensions
    {
        private const double R = 6371008.8; // metrów

        public static double ToRad(this double deg) => deg * Math.PI / 180.0;
        public static double ToDeg(this double rad) => rad * 180.0 / Math.PI;

        public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = (lat2 - lat1).ToRad();
            var dLon = (lon2 - lon1).ToRad();
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1.ToRad()) * Math.Cos(lat2.ToRad()) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        public static double BearingDeg(double lat1, double lon1, double lat2, double lon2)
        {
            var φ1 = lat1.ToRad();
            var φ2 = lat2.ToRad();
            var λ = (lon2 - lon1).ToRad();

            var y = Math.Sin(λ) * Math.Cos(φ2);
            var x = Math.Cos(φ1) * Math.Sin(φ2) - Math.Sin(φ1) * Math.Cos(φ2) * Math.Cos(λ);
            var brng = Math.Atan2(y, x).ToDeg();
            return (brng + 360.0) % 360.0;
        }

        // Odległość punkt–odcinek geodezyjnie przybliżamy w układzie lokalnym (wystarczające dla małych promieni).
        // Zwraca: (minDistMeters, bearingSegmentDeg)
        public static (double distM, double segBearingDeg) PointToPolylineMeters(
            double lat, double lon,
            IReadOnlyList<StaticGtfsIndex.ShapePoint> poly)
        {
            double bestDist = double.MaxValue;
            double bestBearing = 0;

            for (int i = 0; i < poly.Count - 1; i++)
            {
                var a = poly[i]; var b = poly[i + 1];

                // bearing odcinka (A->B)
                var bearing = BearingDeg(a.Lat, a.Lon, b.Lat, b.Lon);

                // rzutowanie w lokalnym układzie metrów (equirectangular)
                // (dla niewielkich odcinków jest to wystarczające i szybkie)
                var dist = DistancePointToSegmentMeters(lat, lon, a.Lat, a.Lon, b.Lat, b.Lon);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestBearing = bearing;
                }
            }

            return (bestDist, bestBearing);
        }

        private static double DistancePointToSegmentMeters(
            double plat, double plon,
            double alat, double alon,
            double blat, double blon)
        {
            // equirectangular projection
            double φ = ((alat + blat + plat) / 3.0).ToRad();
            double x(double lon) => (lon - alon).ToRad() * Math.Cos(φ) * R;
            double y(double lat) => (lat - alat).ToRad() * R;

            var ax = 0.0; var ay = 0.0;
            var bx = x(blon); var by = y(blat);
            var px = x(plon); var py = y(plat);

            var abx = bx - ax; var aby = by - ay;
            var apx = px - ax; var apy = py - ay;

            var ab2 = abx * abx + aby * aby;
            if (ab2 <= 1e-9) return Math.Sqrt(apx * apx + apy * apy);

            var t = (apx * abx + apy * aby) / ab2;
            t = Math.Max(0, Math.Min(1, t));

            var cx = ax + t * abx;
            var cy = ay + t * aby;

            var dx = px - cx; var dy = py - cy;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
