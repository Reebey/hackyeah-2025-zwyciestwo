using front.Components.Pages;
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

    public async Task<Powiadomienia[]> GetNotificationsAsync()
    {
        try
        {
            var client = _clientFactory.CreateClient("NotificationsClient");
            var notifications = await client.GetFromJsonAsync<Powiadomienia[]>("notifications");
            return notifications ?? Array.Empty<Powiadomienia>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania powiadomień");
            return Array.Empty<Powiadomienia>();
        }
    }
}
