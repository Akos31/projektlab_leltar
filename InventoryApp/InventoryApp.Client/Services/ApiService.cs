using InventoryApp.Client.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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

    // POST api/assets/import  (multipart: "file" + "zoneName")
    public async Task<ImportResult> ImportExcelAsync(Stream fileStream, string fileName, string zoneName)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(zoneName), "zoneName");

        using var response = await _http.PostAsync("api/assets/import", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"A szerver hibát adott ({(int)response.StatusCode}): {error}");
        }

        return await response.Content.ReadFromJsonAsync<ImportResult>() ?? new ImportResult();
    }
    public async Task<List<ZoneItem>> GetZonesAsync()
    => await _http.GetFromJsonAsync<List<ZoneItem>>("api/zones") ?? new();

    public async Task<PeriodItem?> OpenPeriodAsync(int zoneId)
    {
        var resp = await _http.PostAsync($"api/zones/{zoneId}/open-period", null);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<PeriodItem>();
    }

    public async Task<List<RoomItem>> GetRoomsAsync(int zoneId)
        => await _http.GetFromJsonAsync<List<RoomItem>>($"api/zones/{zoneId}/rooms") ?? new();

    public async Task<RoomItem?> AddRoomAsync(int zoneId, string roomNumber)
    {
        var resp = await _http.PostAsJsonAsync($"api/zones/{zoneId}/rooms", new { roomNumber });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<RoomItem>();
    }

    public async Task<ScanResponse> ScanAsync(string code, int zoneId, int roomId, int quantity = 1)
    {
        var resp = await _http.PostAsJsonAsync("api/scans",
            new { code, inventoryZoneId = zoneId, roomId, quantity });
        try
        {
            var body = await resp.Content.ReadFromJsonAsync<ScanResponse>();
            if (body?.Result != null) return body;
        }
        catch (JsonException) { }
        return new ScanResponse { Result = "Error", Message = $"Szerverhiba ({(int)resp.StatusCode})" };
    }

    public async Task ClosePeriodAsync(int zoneId)
    {
        var resp = await _http.PostAsync($"api/zones/{zoneId}/close-period", null);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<ScanItem>> GetScansAsync(int? zoneId = null, bool? otherZone = null)
    {
        var qs = new List<string>();
        if (zoneId != null) qs.Add($"zoneId={zoneId}");
        if (otherZone != null) qs.Add($"otherZone={(otherZone.Value ? "true" : "false")}");
        var url = "api/scans" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return await _http.GetFromJsonAsync<List<ScanItem>>(url) ?? new();
    }
}