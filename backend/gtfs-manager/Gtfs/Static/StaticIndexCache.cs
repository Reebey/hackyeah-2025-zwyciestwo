using System.Collections.Concurrent;

namespace gtfs_manager.Gtfs.Static
{
    public interface IStaticIndexCache
    {
        StaticGtfsIndex GetOrLoad(string zipPath);
    }

    public sealed class StaticIndexCache : IStaticIndexCache
    {
        private readonly ConcurrentDictionary<string, StaticGtfsIndex> _cache = new(StringComparer.OrdinalIgnoreCase);

        public StaticGtfsIndex GetOrLoad(string zipPath)
        {
            return _cache.GetOrAdd(zipPath, StaticGtfsIndex.FromZip);
        }
    }
}
