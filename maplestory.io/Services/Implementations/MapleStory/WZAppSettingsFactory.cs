using maplestory.io.Models;
using maplestory.io.Services.Interfaces.MapleStory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PKG1;
using SixLabors.ImageSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using maplestory.io.Entities.Models;

namespace maplestory.io.Services.Implementations.MapleStory
{
    public class WZAppSettingsFactory : IWZFactory
    {
        // Inheritable.
        private WZOptions _config;
        public static ILogger Logger;

        // Dictionaries
        static ConcurrentDictionary<string, EventWaitHandle> wzLoading = new ConcurrentDictionary<string, EventWaitHandle>();
        static ConcurrentDictionary<Region, ConcurrentDictionary<string, MSPackageCollection>> cache = new ConcurrentDictionary<Region, ConcurrentDictionary<string, MSPackageCollection>>();

        public IEnumerable<MapleVersion> Versions => _config.versions;

        // Settings Factories
        public WZAppSettingsFactory(IOptions<WZOptions> config) : this(config.Value) { }
        public WZAppSettingsFactory(WZOptions config) => this._config = config;

        public MSPackageCollection GetWZ(Region region, string version)
        {
            // Make sure that we have a version.
            if (version == null) version = "latest";

            // Trim the versions tring.
            version = version.TrimStart('0');

            EventWaitHandle wait = new EventWaitHandle(false, EventResetMode.ManualReset);
            string versionHash = $"{region.ToString()}-{version}";

            if (!cache.ContainsKey(region)) cache.TryAdd(region, new ConcurrentDictionary<string, MSPackageCollection>());
            else if (cache[region].ContainsKey(version))
                return cache[region][version];

            if (!wzLoading.TryAdd(versionHash, wait))
            {
                Logger.LogInformation($"Waiting for other thread to finish loading {region} - {version}");
                wzLoading[versionHash].WaitOne();
                Logger.LogInformation($"Finished waiting for {region} - {version}");
                if (cache.ContainsKey(region) && cache[region].ContainsKey(version))
                    return GetWZ(region, version);
                else throw new KeyNotFoundException("That version or region could not be found");
            }

            // Placeholder variable.
            MapleVersion mapleVersion = null;

            if (version == "latest")
            {
                mapleVersion = _config.versions.LastOrDefault(c => c.Region == region);
            }
            else
            {
                mapleVersion = _config.versions.FirstOrDefault(c => c.Region == region && c.MapleVersionId == version);
            }

            if (mapleVersion == null)
            {
                wait.Set();
                throw new KeyNotFoundException("That version or region could not be found");
            }

            MSPackageCollection collection = new MSPackageCollection(mapleVersion, null, region);
            Logger.LogInformation($"Finished loading {region} - {version}");
            if (cache[region].TryAdd(version, collection) && cache[region].ContainsKey("latest"))
            {
                wait.Set();
                if (mapleVersion.Id > cache[region]["latest"].MapleVersion.Id)
                {
                    // Update the latest pointer if this is newer than the old latest
                    cache[region]["latest"] = collection;
                }

                // Return the new collection.
                return collection;
            }
            else
            {
                // Already exists, return the cached value.
                return cache[region][version];
            }
        }
    }
}
