using System.Net.Http.Json;
using InventoryApp.Client.Models;

namespace InventoryApp.Client.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<AssetSummary>> GetAssetsAsync()
    {
        var result = await _http.GetFromJsonAsync<List<AssetSummary>>("api/assets");
        return result ?? new List<AssetSummary>();
    }
}