using System.Net;
using InventoryApp.Client.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InventoryApp.Client.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly UserSession _session;

    public ApiService(HttpClient http, UserSession session)
    {
        _http = http;
        _session = session;
    }

    // POST api/auth/login
    // Ha a válaszban MustChangePassword igaz, NEM jelentkezteti be a felhasználót:
    // előbb a ChangePasswordAsync-kal új jelszót kell beállítani.
    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/login", new { username, password });

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException(
                await ReadErrorAsync(response) ?? "Hibás felhasználónév vagy jelszó.");

        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>()
                    ?? throw new InvalidOperationException("Üres válasz érkezett a szervertől.");

        if (!login.MustChangePassword)
            SignIn(login);

        return login;
    }

    // POST api/auth/change-password
    // Ideiglenes és saját jelszó cseréjére is. Siker esetén a szerver új tokent ad
    // (a régi érvénytelen lesz), ezzel be is jelentkeztetjük a felhasználót.
    public async Task<(bool Ok, string? Error)> ChangePasswordAsync(
        string username, string currentPassword, string newPassword)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/change-password",
            new { username, currentPassword, newPassword });

        if (!response.IsSuccessStatusCode)
            return (false, await ReadErrorAsync(response) ?? $"Szerverhiba ({(int)response.StatusCode})");

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (login is null || string.IsNullOrEmpty(login.Token))
            return (false, "Üres válasz érkezett a szervertől.");

        SignIn(login);
        return (true, null);
    }

    private void SignIn(LoginResponse login)
    {
        _session.SignIn(login);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
    }

    // --- Felhasználókezelés (admin) ---

    // GET api/users
    public async Task<List<UserItem>> GetUsersAsync()
        => await _http.GetFromJsonAsync<List<UserItem>>("api/users") ?? new();

    // GET api/users/roles
    public async Task<List<string>> GetRolesAsync()
        => await _http.GetFromJsonAsync<List<string>>("api/users/roles") ?? new();

    // POST api/users
    public async Task<(TempPasswordResult? Result, string? Error)> CreateUserAsync(
        string username, string displayName, string role)
    {
        using var resp = await _http.PostAsJsonAsync("api/users", new { username, displayName, role });
        if (!resp.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(resp) ?? $"Szerverhiba ({(int)resp.StatusCode})");
        return (await resp.Content.ReadFromJsonAsync<TempPasswordResult>(), null);
    }

    // PUT api/users/{id}
    public async Task<(bool Ok, string? Error)> UpdateUserAsync(
        int id, string username, string displayName, string role, bool isActive)
    {
        using var resp = await _http.PutAsJsonAsync($"api/users/{id}",
            new { username, displayName, role, isActive });
        return await ReadResultAsync(resp);
    }

    // POST api/users/{id}/reset-password
    public async Task<(TempPasswordResult? Result, string? Error)> ResetPasswordAsync(int id)
    {
        using var resp = await _http.PostAsync($"api/users/{id}/reset-password", null);
        if (!resp.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(resp) ?? $"Szerverhiba ({(int)resp.StatusCode})");
        return (await resp.Content.ReadFromJsonAsync<TempPasswordResult>(), null);
    }

    // A szerver hibaüzenete: { "message": "..." } JSON vagy sima szöveg
    private static async Task<string?> ReadErrorAsync(HttpResponseMessage resp)
    {
        if (resp.StatusCode == HttpStatusCode.Forbidden)
            return "Nincs jogosultságod ehhez a művelethez.";

        var text = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            var err = JsonSerializer.Deserialize<ErrorResponse>(text,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (!string.IsNullOrWhiteSpace(err?.Message)) return err.Message;
        }
        catch (JsonException) { }
        return text.Trim().Trim('"');
    }

    public void Logout()
    {
        _session.SignOut();
        _http.DefaultRequestHeaders.Authorization = null;
    }

    // GET api/assets?skip=..&take=..&search=..&status=..&type=..&zone=..
    public async Task<PagedResult<AssetSummary>> GetAssetsPageAsync(
        int skip, int take, AssetQuery filter, CancellationToken cancellationToken = default)
    {
        var parameters = new List<string> { $"skip={skip}", $"take={take}" };

        void Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parameters.Add($"{name}={Uri.EscapeDataString(value)}");
        }
        void AddInt(string name, int? value)
        {
            if (value is not null)
                parameters.Add($"{name}={value}");
        }

        AddInt("roomId", filter.RoomId);
        AddInt("responsiblePersonId", filter.ResponsiblePersonId);

        Add("search", filter.Search);
        Add("status", filter.Status);
        Add("type", filter.Type);
        Add("zone", filter.Zone);

        var url = "api/assets?" + string.Join("&", parameters);
        var result = await _http.GetFromJsonAsync<PagedResult<AssetSummary>>(url, cancellationToken);
        return result ?? new PagedResult<AssetSummary>();
    }

    // GET api/assets/filters
    public async Task<AssetFilterOptions> GetAssetFiltersAsync()
    {
        var result = await _http.GetFromJsonAsync<AssetFilterOptions>("api/assets/filters");
        return result ?? new AssetFilterOptions();
    }


    // PUT api/assets/{id}/status
    public async Task<AssetStatusResult> UpdateAssetStatusAsync(int assetId, string newStatus)
    {
        using var response = await _http.PutAsJsonAsync($"api/assets/{assetId}/status", new { newStatus });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AssetStatusResult>()
               ?? throw new InvalidOperationException("Üres válasz érkezett a szervertől.");
    }

    // GET api/assets/{id}/status-history
    public async Task<List<AssetStatusHistoryEntry>> GetAssetStatusHistoryAsync(int assetId)
    {
        var result = await _http.GetFromJsonAsync<List<AssetStatusHistoryEntry>>($"api/assets/{assetId}/status-history");
        return result ?? new List<AssetStatusHistoryEntry>();
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

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new HttpRequestException("Nincs jogosultságod az importáláshoz.");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"A szerver hibát adott ({(int)response.StatusCode}): {error}");
        }

        return await response.Content.ReadFromJsonAsync<ImportResult>() ?? new ImportResult();
    }
    public async Task<List<ZoneItem>> GetZonesAsync(bool includeInactive = false)
    => await _http.GetFromJsonAsync<List<ZoneItem>>(
           $"api/zones?includeInactive={(includeInactive ? "true" : "false")}") ?? new();

    public async Task<(bool Ok, string? Error)> SaveZoneAsync(int? id, string code, string name)
    {
        var resp = id == null
            ? await _http.PostAsJsonAsync("api/zones", new { code, name })
            : await _http.PutAsJsonAsync($"api/zones/{id}", new { code, name });
        return await ReadResultAsync(resp);
    }

    public async Task<(bool Ok, string? Error)> SetZoneActiveAsync(int id, bool active)
    {
        var resp = await _http.PostAsync($"api/zones/{id}/{(active ? "activate" : "deactivate")}", null);
        return await ReadResultAsync(resp);
    }

    private static async Task<(bool Ok, string? Error)> ReadResultAsync(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return (true, null);
        try
        {
            var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
            if (err?.Message != null) return (false, err.Message);
        }
        catch (JsonException) { }
        return (false, $"Szerverhiba ({(int)resp.StatusCode})");
    }
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

    public async Task<List<PeriodListItem>> GetPeriodsAsync(int zoneId)
    => await _http.GetFromJsonAsync<List<PeriodListItem>>($"api/zones/{zoneId}/periods") ?? new();

    public async Task<List<ScanItem>> GetScansAsync(int? zoneId = null, int? periodId = null, bool? otherZone = null)
    {
        var qs = new List<string>();
        if (zoneId != null) qs.Add($"zoneId={zoneId}");
        if (periodId != null) qs.Add($"periodId={periodId}");
        if (otherZone != null) qs.Add($"otherZone={(otherZone.Value ? "true" : "false")}");
        var url = "api/scans" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return await _http.GetFromJsonAsync<List<ScanItem>>(url) ?? new();
    }

    // --- Kiértékelés: lezárt leltárak összehasonlítása ---

    // GET api/reports/closed-periods
    public async Task<List<ClosedPeriodItem>> GetClosedPeriodsAsync()
        => await _http.GetFromJsonAsync<List<ClosedPeriodItem>>("api/reports/closed-periods") ?? new();

    // GET api/reports/compare?periodA=..&periodB=..
    public async Task<(CompareResult? Result, string? Error)> ComparePeriodsAsync(int periodA, int periodB)
    {
        using var resp = await _http.GetAsync($"api/reports/compare?periodA={periodA}&periodB={periodB}");
        if (!resp.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(resp) ?? $"Szerverhiba ({(int)resp.StatusCode})");
        return (await resp.Content.ReadFromJsonAsync<CompareResult>(), null);
    // --- Felelős személyek 
    public async Task<List<ResponsiblePersonItem>> GetResponsiblePersonsAsync()
        => await _http.GetFromJsonAsync<List<ResponsiblePersonItem>>("api/responsiblepersons") ?? new();

    public async Task<(ResponsiblePersonItem? Result, string? Error)> CreateResponsiblePersonAsync(string name)
    {
        using var resp = await _http.PostAsJsonAsync("api/responsiblepersons", new { name });
        if (!resp.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(resp) ?? $"Szerverhiba ({(int)resp.StatusCode})");
        return (await resp.Content.ReadFromJsonAsync<ResponsiblePersonItem>(), null);
    }

    public async Task<(bool Ok, string? Error)> AssignResponsiblePersonAsync(int assetId, int? responsiblePersonId)
    {
        using var resp = await _http.PutAsJsonAsync($"api/assets/{assetId}/responsible-person",
            new { responsiblePersonId });
        return await ReadResultAsync(resp);
    }

    // --- Eszköztípusok 
    public async Task<List<AssetTypeItem>> GetAssetTypesAsync()
        => await _http.GetFromJsonAsync<List<AssetTypeItem>>("api/assettypes") ?? new();

    public async Task<(bool Ok, string? Error)> CreateAssetTypeAsync(string name)
    {
        using var resp = await _http.PostAsJsonAsync("api/assettypes", new { name });
        return await ReadResultAsync(resp);
    }
}