using front.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace front.Services;

public class TrasySerwis
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<PowiadomieniaSerwis> _logger;

    public TrasySerwis(IHttpClientFactory clientFactory, ILogger<PowiadomieniaSerwis> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<List<Stop>> GetStacje()
    {
        try
        {
            var client = _clientFactory.CreateClient("ZgloszeniaClient");
            var stops = await client.GetFromJsonAsync<List<Stop>>($"https://localhost:7042/api/static/stops/all");
            return stops ?? new List<Stop>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania pobliskich tras");
            return new List<Stop>();
        }
    }
}
