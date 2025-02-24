﻿using maplestory.io.Entities;
using maplestory.io.Models;
using maplestory.io.Services.Implementations.MapleStory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using PKG1;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace maplestory.io
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Console Arguments: {string.Join(",", args)}");

            Stopwatch watch = Stopwatch.StartNew();

            ILoggerFactory logging = LoggerFactory.Create(builder => builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));
            ILogger<Package> packageLogger = logging.CreateLogger<Package>();
            ILogger<PackageCollection> packageCollectionLogger = logging.CreateLogger<PackageCollection>();
            ILogger<VersionGuesser> versionGuesserLogger = logging.CreateLogger<VersionGuesser>();
            ILogger<WZReader> readerLogging = logging.CreateLogger<WZReader>();

            MSPackageCollection.Logger = logging.CreateLogger<MSPackageCollection>();
            WZFactory.Logger = logging.CreateLogger<WZFactory>();
            WZAppSettingsFactory.Logger = logging.CreateLogger<WZAppSettingsFactory>();
            PackageCollection.Logging = (s) => packageCollectionLogger.LogInformation(s);
            VersionGuesser.Logging = (s) => versionGuesserLogger.LogInformation(s);
            Package.Logging = (s) => packageLogger.LogInformation(s);

            WZProperty.SpecialUOL.Add("version", (uol, version) =>
            {
                string path = uol.Path;
                MSPackageCollection wz = WZFactory.GetWZFromCache(uol.FileContainer.Collection.WZRegion, version);
                if (wz != null) return wz.Resolve(path);

                using (ApplicationDbContext dbCtx = new ApplicationDbContext())
                {
                    WZFactory wzFactory = new WZFactory(dbCtx);
                    return wzFactory.GetWZ(uol.FileContainer.Collection.WZRegion, version).Resolve(path);
                }
            });

            //WZProperty.ChildrenMutate = (i) =>
            //{
            //    return i.Select(c =>
            //    {
            //        if (c.Type != PropertyType.Lua) return c;

            //        string luaVal = c.ResolveForOrNull<string>();
            //        if (!luaVal.StartsWith("!")) return c;

            //        string specialType = luaVal.Substring(1, luaVal.IndexOf(':') - 1);
            //        if (WZProperty.SpecialUOL.ContainsKey(specialType))
            //            return WZProperty.SpecialUOL[specialType](c, luaVal.Substring(specialType.Length + 2));
            //        else throw new InvalidOperationException("Unable to follow Special UOL, as there is no defined route");
            //    });
            //};

            readerLogging.LogDebug("Initializing Keys");
            WZReader.InitializeKeys();
            readerLogging.LogDebug("Done");

            //WZFactory.LoadAllWZ();

            ILogger prog = logging.CreateLogger<Program>();
            watch.Stop();
            prog.LogInformation($"Starting aspnet kestrel, took {watch.ElapsedMilliseconds}ms to initialize");

            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Limits.MaxConcurrentConnections = ushort.MaxValue;
                    options.Limits.MaxConcurrentUpgradedConnections = ushort.MaxValue;
                    options.Limits.MaxRequestLineSize = ushort.MaxValue;
                    options.Limits.MaxRequestBufferSize = int.MaxValue;
                    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
                    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(120);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls("http://*:5000")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
