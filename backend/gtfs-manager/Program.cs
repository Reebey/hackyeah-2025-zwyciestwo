using gtfs_manager.Gtfs.Realtime;
using gtfs_manager.Gtfs.Static;

var builder = WebApplication.CreateBuilder(args);

// Config
builder.Services.AddSingleton<RealtimeReader>();
builder.Services.AddSingleton<StaticGtfsReader>();

var app = builder.Build();

// Helper do œcie¿ek plików w ./Data (konfigurowalne w appsettings.json przez "Gtfs:DataDir")
string GetDataPath(string file)
{
    var baseDir = app.Configuration["Gtfs:DataDir"] ?? "Data";
    return Path.Combine(app.Environment.ContentRootPath, baseDir, file);
}

app.MapGet("/", () => Results.Ok(new { service = "gtfs-manager", ok = true }));

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

app.Run();
