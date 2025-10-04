using gtfs_manager.Gtfs.Realtime;
using gtfs_manager.Gtfs.Static;
using gtfs_manager.Gtfs.Static.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Services.AddSingleton<RealtimeReader>();
builder.Services.AddSingleton<StaticGtfsReader>();
builder.Services.AddSingleton<IStaticIndexCache, StaticIndexCache>();
builder.Services.AddSingleton<VehicleEnricher>();

// OpenAPI (wbudowany generator .NET 9)
builder.Services.AddOpenApi();

// ProblemDetails domyślnie w ASP.NET Core
builder.Services.AddRouting(opts => opts.LowercaseUrls = true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .AllowAnyOrigin()   // do NOT combine with .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader()
    );
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = "Unexpected error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = app.Environment.IsDevelopment() ? ex?.ToString() : ex?.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    });
});

app.UseCors("AllowAll");

app.MapOpenApi(); // /openapi

// ========= Helpers =========
string DataDir() => app.Configuration["Gtfs:DataDir"] ?? "Data";
string PathInData(string file) => Path.Combine(app.Environment.ContentRootPath, DataDir(), file);

string ContentPath(string relative) => Path.Combine(app.Environment.ContentRootPath, relative);

app.MapGet("/v1/rt/vehicles/enriched", (string file, string zip, string? tripUpdates, VehicleEnricher enricher) =>
{
    var vp = PathInData(file);
    var z = PathInData(zip);
    string? tu = string.IsNullOrWhiteSpace(tripUpdates) ? null : PathInData(tripUpdates);

    if (!System.IO.File.Exists(vp)) return Results.NotFound(new { file = vp, error = "file_not_found" });
    if (!System.IO.File.Exists(z)) return Results.NotFound(new { file = z, error = "file_not_found" });
    if (tu != null && !System.IO.File.Exists(tu)) tu = null; // ignoruj jeśli brak

    var data = enricher.Enrich(vp, z, tu);
    return Results.Ok(data);
});

// ========= Root ping =========
app.MapGet("/", () => Results.Ok(new { service = "gtfs-manager", ok = true }));

// ========= META =========
app.MapGet("/meta", () =>
{
    var asm = Assembly.GetExecutingAssembly();
    var version = asm.GetName().Version?.ToString() ?? "unknown";
    var file = asm.Location;
    var buildUtc = System.IO.File.GetLastWriteTimeUtc(file);

    return Results.Ok(new
    {
        name = "gtfs-manager",
        version,
        buildUtc = buildUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        dataDir = DataDir()
    });
});

// ========= HEALTH =========
// Sprawdza: istnienie Data/, opcjonalnie przykładowe pliki, prawa odczytu
app.MapGet("/health", () =>
{
    var checks = new List<object>();
    var ok = true;

    // 1) folder Data
    var dataFolder = ContentPath(DataDir());
    var dataExists = Directory.Exists(dataFolder);
    ok &= dataExists;
    checks.Add(new { check = "data_dir_exists", value = dataFolder, ok = dataExists });

    // 2) przykładowe pliki (jeśli masz inne nazwy — spoko, to tylko hint)
    string[] expected = [
        "VehiclePositions.pb",
        "TripUpdates.pb",
        "ServiceAlerts.pb",
        "GTFS_KRK_T.zip"
    ];
    foreach (var name in expected)
    {
        var path = Path.Combine(dataFolder, name);
        var exists = System.IO.File.Exists(path);
        checks.Add(new { check = "file_exists", file = name, ok = exists });
        // Nie wymagamy tych plików do „green” → to tylko informacja
    }

    var status = ok ? "healthy" : "degraded";
    return Results.Ok(new { status, checks, nowUtc = DateTime.UtcNow.ToString("O") });
});

// ========= v1 group (pod przygotowanie na przyszłe fazy) =========
var v1 = app.MapGroup("/v1");

// Helper do ścieżek plików w ./Data (konfigurowalne w appsettings.json przez "Gtfs:DataDir")
string GetDataPath(string file)
{
    var baseDir = app.Configuration["Gtfs:DataDir"] ?? "Data";
    return Path.Combine(app.Environment.ContentRootPath, baseDir, file);
}

// === REALTIME (protobuf .pb) ===

app.MapGet("/api/rt/vehicles", (string file, RealtimeReader reader) =>
{
    var path = GetDataPath(file);
    if (!System.IO.File.Exists(path)) return Results.NotFound(new { path, error = "file_not_found" });
    var data = reader.ReadVehicles(path);
    return Results.Ok(data);
});

app.MapGet("/api/rt/trip-updates", (string file, RealtimeReader reader) =>
{
    var path = GetDataPath(file);
    if (!System.IO.File.Exists(path)) return Results.NotFound(new { path, error = "file_not_found" });
    var data = reader.ReadTripUpdates(path);
    return Results.Ok(data);
});

app.MapGet("/api/rt/alerts", (string file, RealtimeReader reader) =>
{
    var path = GetDataPath(file);
    if (!System.IO.File.Exists(path)) return Results.NotFound(new { path, error = "file_not_found" });
    var data = reader.ReadAlerts(path);
    return Results.Ok(data);
});

// === STATIC (GTFS zip z CSV) ===

app.MapGet("/api/static/stops", (string zip, StaticGtfsReader reader) =>
{
    var path = GetDataPath(zip);
    if (!System.IO.File.Exists(path)) return Results.NotFound(new { path, error = "file_not_found" });
    var data = reader.ReadStopsFromZip(path);
    return Results.Ok(data);
});

app.MapGet("/api/static/stops/all", (StaticGtfsReader reader) =>
{
    IEnumerable<Stop> stops;
    var path = GetDataPath("GTFS_KRK_A.zip");
    if (!System.IO.File.Exists(path)) 
        return Results.NotFound(new { path, error = "file_not_found" });
    stops = reader.ReadStopsFromZip(path);

    path = GetDataPath("GTFS_KRK_M.zip");
    if (!System.IO.File.Exists(path)) 
        return Results.NotFound(new { path, error = "file_not_found" });
    stops = stops.Concat(reader.ReadStopsFromZip(path));

    path = GetDataPath("GTFS_KRK_T.zip");
    if (!System.IO.File.Exists(path)) 
        return Results.NotFound(new { path, error = "file_not_found" });
    stops = stops.Concat(reader.ReadStopsFromZip(path));

    return Results.Ok(stops);
});

app.MapGet("/api/static/routes", (string zip, StaticGtfsReader reader) =>
{
    var path = GetDataPath(zip);
    if (!System.IO.File.Exists(path)) return Results.NotFound(new { path, error = "file_not_found" });
    var data = reader.ReadRoutesFromZip(path);
    return Results.Ok(data);
});

app.MapGet("/api/static/trips", (string zip, StaticGtfsReader reader) =>
{
    var path = GetDataPath(zip);
    if (!System.IO.File.Exists(path)) return Results.NotFound(new { path, error = "file_not_found" });
    var data = reader.ReadTripsFromZip(path);
    return Results.Ok(data);
});

app.MapGet("/v1/map/routes/by-point", (
    double lat,
    double lon,
    double? radiusMeters,
    IStaticIndexCache cache) =>
{
    var dataZip = PathInData("GTFS_KRK_A.zip");
    if (!System.IO.File.Exists(dataZip))
        return Results.NotFound(new { file = dataZip, error = "file_not_found" });

    var idx = cache.GetOrLoad(dataZip);

    // 1) Znajdź przystanki w promieniu (domyślnie 150 m)
    double radius = radiusMeters is > 0 ? radiusMeters.Value : 150.0;
    var nearStops = idx.Stops.Values
        .Where(s => s.Lat is double slat && s.Lon is double slon)
        .Select(s =>
        {
            var dKm = Geo.HaversineKm(lat, lon, s.Lat!.Value, s.Lon!.Value);
            return new { s.StopId, s.Name, DistanceM = dKm * 1000.0 };
        })
        .Where(x => x.DistanceM <= radius)
        .OrderBy(x => x.DistanceM)
        .ToList();

    if (nearStops.Count == 0)
        return Results.Ok(Array.Empty<object>());

    var nearStopIds = new HashSet<string>(nearStops.Select(s => s.StopId), StringComparer.OrdinalIgnoreCase);

    // 2) Wyznacz tripy, które zatrzymują się na tych przystankach
    var routeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var ts in idx.TripStopSeq.Values)
    {
        // Jeżeli którykolwiek stop z tripa jest wśród "near"
        // (dla wydajności: przerwij jak znajdziesz pierwszy)
        foreach (var stopId in ts.StopIds)
        {
            if (nearStopIds.Contains(stopId))
            {
                if (idx.Trips.TryGetValue(ts.TripId, out var meta))
                    routeIds.Add(meta.RouteId);
                break;
            }
        }
    }

    // 3) Zbuduj wynik: routeId + nazwy + lista najbliższych przystanków dopasowanych (informacyjnie)
    var routes = routeIds
        .Select(rid =>
        {
            idx.Routes.TryGetValue(rid, out var rmeta);
            return new
            {
                routeId = rid,
                routeShortName = rmeta?.ShortName,
                routeLongName = rmeta?.LongName
            };
        })
        .OrderBy(r => r.routeShortName ?? r.routeId)
        .ToList();

    var response = new
    {
        query = new { lat, lon, radiusMeters = radius },
        nearbyStops = nearStops.Select(s => new { s.StopId, s.Name, distanceMeters = Math.Round(s.DistanceM, 1) }),
        routes
    };

    return Results.Ok(response);
});


app.Run();
