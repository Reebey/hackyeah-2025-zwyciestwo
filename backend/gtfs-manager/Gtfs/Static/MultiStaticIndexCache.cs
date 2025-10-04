using System.Collections.Concurrent;

namespace gtfs_manager.Gtfs.Static
{
    public interface IMultiStaticIndexCache
    {
        /// Zwraca: (feedId -> StaticGtfsIndex)
        IReadOnlyDictionary<string, StaticGtfsIndex> GetOrLoadMany(IEnumerable<string> zipPaths);
        string GetFeedId(string zipPath);
    }

    public sealed class MultiStaticIndexCache : IMultiStaticIndexCache
    {
        private readonly IStaticIndexCache _single;
        private readonly ConcurrentDictionary<string, string> _feedIds = new(StringComparer.OrdinalIgnoreCase);

        public MultiStaticIndexCache(IStaticIndexCache single) => _single = single;

        public IReadOnlyDictionary<string, StaticGtfsIndex> GetOrLoadMany(IEnumerable<string> zipPaths)
        {
            var dict = new Dictionary<string, StaticGtfsIndex>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in zipPaths)
            {
                var feedId = GetFeedId(path);
                dict[feedId] = _single.GetOrLoad(path);
            }
            return dict;
        }

        public string GetFeedId(string zipPath)
        {
            // feedId = nazwa pliku bez rozszerzenia, np. "GTFS_KRK_T"
            return _feedIds.GetOrAdd(zipPath, p => Path.GetFileNameWithoutExtension(p) ?? p);
        }
    }
}
