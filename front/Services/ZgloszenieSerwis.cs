using front.Components.Pages;
using front.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace front.Services;

public class ZgloszenieSerwis
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<PowiadomieniaSerwis> _logger;

    public ZgloszenieSerwis(IHttpClientFactory clientFactory, ILogger<PowiadomieniaSerwis> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<List<Candidate>> GetNearbyRoutesAsync(Models.Location? location)
    {
        try
        {
            Models.Location locationMock = new Models.Location { Latitude = 50.065, Longitude = 19.945 };

            var client = _clientFactory.CreateClient("ZgloszeniaClient");
            var candidates = client.GetFromJsonAsync<OnRoute>($"https://localhost:7042/v1/map/routes/on-route?lon={locationMock.Longitude.ToString().Replace(",",".")}&lat={locationMock.Latitude.ToString().Replace(",", ".")}").Result.candidates;
            return candidates ?? new List<Candidate>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania pobliskich tras");
            return new List<Candidate>();
        }
    }

    public async Task AddReport(CreateReportDto report)
    {
        try
        {
            var client = _clientFactory.CreateClient("ZgloszeniaClient");
            var response = await client.PostAsJsonAsync<CreateReportDto>($"https://localhost:7042/v1/reports", report);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania pobliskich tras");
        }
    }
}
