using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryApp.Client.Models;

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
    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        using var response = await _http.PostAsJsonAsync("api/auth/login", new { username, password });

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Hibás felhasználónév vagy jelszó.");

        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>()
                    ?? throw new InvalidOperationException("Üres válasz érkezett a szervertől.");

        _session.SignIn(login);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        return login;
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
}