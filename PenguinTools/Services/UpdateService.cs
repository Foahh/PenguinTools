using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PenguinTools.Services;

public interface IUpdateService
{
    Task<(Version Version, string Url)> CheckForUpdatesAsync();
}

public class GitHubUpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;

    public GitHubUpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(App.Name, App.Version.ToString()));
    }

    public async Task<(Version Version, string Url)> CheckForUpdatesAsync()
    {
        var response = await _httpClient.GetAsync("https://api.github.com/repos/Foahh/PenguinTools/releases/latest");
        if (!response.IsSuccessStatusCode) throw new OperationCanceledException("Could not retrieve release information from GitHub.");

        var jsonContent = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        var tagName = root.GetProperty("tag_name").GetString();
        var htmlUrl = root.GetProperty("html_url").GetString();

        if (!string.IsNullOrWhiteSpace(tagName) && tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase)) tagName = tagName[1..];
        if (!Version.TryParse(tagName, out var version)) throw new OperationCanceledException("The release version string is not in a valid format.");
        if (string.IsNullOrWhiteSpace(htmlUrl)) throw new OperationCanceledException("The release URL is not in a valid format.");

        return (version, htmlUrl);
    }
}