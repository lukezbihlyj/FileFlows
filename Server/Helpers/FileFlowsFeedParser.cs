using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Helpers;

public class FileFlowsFeedParser
{
    class FeedItem
    {
        [JsonPropertyName("tags")] public List<string> Tags { get; set; }

        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("content_html")] public string ContentHtml { get; set; }
    }

    class Feed
    {
        [JsonPropertyName("items")] public List<FeedItem> Items { get; set; }
    }

    public static List<ReleaseNotes> Parse(string json)
    {
        var releaseNotesList = new List<ReleaseNotes>();

        var feed = JsonSerializer.Deserialize<Feed>(json);

        foreach (var item in feed.Items.Where(i => i.Tags.Contains("release", StringComparer.OrdinalIgnoreCase)))
        {
            var releaseNotes = new ReleaseNotes
            {
                Version = ExtractVersion(item.Title),
                New = ExtractSection(item.ContentHtml, "new"),
                Fixed = ExtractSection(item.ContentHtml, "fixed")
            };

            releaseNotesList.Add(releaseNotes);
        }

        return releaseNotesList;
    }

    private static string ExtractVersion(string title)
    {
        var versionMatch = Regex.Match(title, @"\d+\.\d+\.\d+");
        return versionMatch.Success ? versionMatch.Value : string.Empty;
    }

    private static List<string> ExtractSection(string contentHtml, string sectionId)
    {
        var sectionPattern = $@"<h[\d][^>]+id=\""{sectionId}\"">.*?</h[\d]>\s*<ul>(.*?)</ul>";
        var match = Regex.Match(contentHtml, sectionPattern, RegexOptions.Singleline);

        if (!match.Success)
            return new List<string>();

        var listItems = Regex.Matches(match.Groups[1].Value, @"<li>(.*?)</li>", RegexOptions.Singleline)
            .Select(m => Clean(m.Groups[1].Value))
            .ToList();

        return listItems;
    }

    private static string Clean(string input)
    {
        input = Regex.Replace(input, "<.*?>", string.Empty)
            .Replace("&nbsp;", " ")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .Trim();
        if (Regex.IsMatch(input, "^FF-\\d+:"))
        {
            // Remove the ticket number from the start of the line
            input = input[(input.IndexOf(':') + 1)..].Trim();
        }
        return input;
    }
}