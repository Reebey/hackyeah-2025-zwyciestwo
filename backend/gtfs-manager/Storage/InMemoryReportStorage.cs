using gtfs_manager.Gtfs.Static.Models;
using System.Collections.Concurrent;

namespace gtfs_manager.Storage
{
    public interface IInMemoryReportStore
    {
        User UpsertUser(string id);
        bool UserExists(string id);

        ReportDto CreateReport(CreateReportDto dto);
        bool TryGetReport(string id, out ReportDto report);
    }

    public sealed class InMemoryReportStore : IInMemoryReportStore
    {
        private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ReportDto> _reports = new(StringComparer.OrdinalIgnoreCase);

        public User UpsertUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("User id is required");
            var user = new User { Id = id.Trim() };
            _users.AddOrUpdate(user.Id, user, (_, _) => user);
            return user;
        }

        public bool UserExists(string id) => !string.IsNullOrWhiteSpace(id) && _users.ContainsKey(id);

        public ReportDto CreateReport(CreateReportDto dto)
        {
            // prosta walidacja
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.UserId)) throw new ArgumentException("userId is required");
            if (double.IsNaN(dto.Lat) || double.IsNaN(dto.Lon)) throw new ArgumentException("lat/lon are required");

            // użytkownika tworzymy „on the fly” jeśli nie istnieje (wygodne na hackathonie)
            UpsertUser(dto.UserId);

            var id = Guid.NewGuid().ToString("N");
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var rep = new ReportDto
            {
                Id = id,
                CreatedAt = now,
                Status = "pending",
                Score = 0,

                UserId = dto.UserId.Trim(),
                Title = dto.Title?.Trim() ?? "",
                Description = dto.Description?.Trim(),
                Lat = dto.Lat,
                Lon = dto.Lon,
                RouteId = dto.RouteId?.Trim(),
                TripId = dto.TripId?.Trim(),
                DelayMinutes = dto.DelayMinutes
            };

            if (!_reports.TryAdd(id, rep))
                throw new InvalidOperationException("Could not persist report");

            return rep;
        }

        public bool TryGetReport(string id, out ReportDto report)
            => _reports.TryGetValue(id, out report!);
    }
}
