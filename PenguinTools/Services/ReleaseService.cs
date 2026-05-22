using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using PenguinTools.i18n;

namespace PenguinTools.Services;

public interface IReleaseService
{
    Task<(Version Version, string Url)> CheckForUpdatesAsync();
}

public class GitHubReleaseService : IReleaseService
{
    private readonly HttpClient _httpClient;

    public GitHubReleaseService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(App.Name, App.Version.ToString()));
    }

    public async Task<(Version Version, string Url)> CheckForUpdatesAsync()
    {
        var response = await _httpClient.GetAsync("https://api.github.com/repos/Foahh/PenguinTools/releases/latest");
        if (!response.IsSuccessStatusCode)
            throw new OperationCanceledException(Strings.Error_Release_github_failed);

        var jsonContent = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        var tagName = root.GetProperty("tag_name").GetString();
        var htmlUrl = root.GetProperty("html_url").GetString();

        if (!string.IsNullOrWhiteSpace(tagName) && tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            tagName = tagName[1..];
        if (!Version.TryParse(tagName, out var version))
            throw new OperationCanceledException(Strings.Error_Release_version_invalid);
        if (string.IsNullOrWhiteSpace(htmlUrl))
            throw new OperationCanceledException(Strings.Error_Release_url_invalid);

        return (version, htmlUrl);
    }
}