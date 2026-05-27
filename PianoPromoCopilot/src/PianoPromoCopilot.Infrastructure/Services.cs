using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Application;
using PianoPromoCopilot.Domain;

namespace PianoPromoCopilot.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<YouTubeVideo> YouTubeVideos => Set<YouTubeVideo>();
    public DbSet<VideoOptimizationSuggestion> VideoOptimizationSuggestions => Set<VideoOptimizationSuggestion>();
    public DbSet<PromotionDraft> PromotionDrafts => Set<PromotionDraft>();
    public DbSet<VideoAnalyticsSnapshot> VideoAnalyticsSnapshots => Set<VideoAnalyticsSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<YouTubeVideo>().HasIndex(v => v.YouTubeVideoId).IsUnique();
    }
}

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (db.YouTubeVideos.Any()) return;
        db.YouTubeVideos.AddRange(
            new YouTubeVideo { YouTubeVideoId = "vid001", Title = "Moonlit Arpeggios", Description = "Original solo piano.", ViewCount = 1240, LikeCount = 82, CommentCount = 19, CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new YouTubeVideo { YouTubeVideoId = "vid002", Title = "Rain Over Ivory", Description = "Contemporary piano texture.", ViewCount = 860, LikeCount = 54, CommentCount = 10, CreatedAt = DateTime.UtcNow.AddDays(-8) },
            new YouTubeVideo { YouTubeVideoId = "vid003", Title = "Dawn Prelude", Description = "Gentle morning composition.", ViewCount = 430, LikeCount = 33, CommentCount = 4, CreatedAt = DateTime.UtcNow.AddDays(-4) }
        );
        db.SaveChanges();
    }
}

public class MockLlmService : ILlmService
{
    public Task<string> GenerateAsync(string s, string u, CancellationToken ct = default) => Task.FromResult("{\"titles\":[\"Original Piano Nocturne | Quiet Evening\"],\"descriptions\":[\"A calm original piano composition performed live.\"],\"tags\":[\"original piano\",\"neoclassical piano\"],\"hashtags\":[\"#piano\",\"#originalmusic\"],\"thumbnailIdeas\":[\"Close-up of hands on keys with warm light\"],\"shortsIdeas\":[{\"title\":\"Opening motif\",\"hook\":\"Hear the first phrase\",\"suggestedTimestamp\":\"00:00\",\"description\":\"A short preview of the main motif\",\"caption\":\"Original piano composition preview\"}],\"socialPosts\":{\"instagram\":\"New original piano piece is live 🎹\",\"tiktok\":\"A short moment from my new piano composition\",\"facebook\":\"I published a new original piano composition\",\"reddit\":\"I wrote an original piano composition and would appreciate constructive feedback\",\"x\":\"New original piano composition out now\",\"linkedin\":\"Released a new original piano composition\",\"emailNewsletter\":\"A new piano composition just went live\"}}");
}

public class OpenAiLlmService(HttpClient httpClient, IConfiguration config) : ILlmService
{
    public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var key = config["OpenAI:ApiKey"] ?? config["OpenAI__ApiKey"];
        var model = config["OpenAI:Model"] ?? "gpt-4o-mini";
        var baseUrl = (config["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        req.Headers.Authorization = new("Bearer", key);
        req.Content = JsonContent.Create(new { model, messages = new[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userPrompt } }, temperature = 0.2, response_format = new { type = "json_object" } });
        var resp = await httpClient.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";
    }
}

public class MockYouTubeService : IYouTubeService
{
    public Task<YouTubeChannelDto> GetChannelAsync(CancellationToken c = default) => Task.FromResult(new YouTubeChannelDto("mock-channel", "PianoPromoCopilot Channel", "Mock channel for dev", "", true));
    public Task<IReadOnlyList<YouTubeVideoDto>> GetVideosAsync(CancellationToken c = default) => Task.FromResult((IReadOnlyList<YouTubeVideoDto>)new List<YouTubeVideoDto> { new("vid001", "Moonlit Arpeggios", null, 1240, 82, 19, DateTime.UtcNow.AddDays(-10), null) });
    public Task<YouTubeVideoDto?> GetVideoAsync(string id, CancellationToken c = default) => Task.FromResult<YouTubeVideoDto?>(new(id, "Sample", null, 100, 5, 1, DateTime.UtcNow.AddDays(-1), null));
    public Task UpdateVideoMetadataAsync(UpdateYouTubeVideoMetadataRequest req, CancellationToken c = default) => Task.CompletedTask;
}

public class GoogleYouTubeService : IYouTubeService
{
    public Task<YouTubeChannelDto> GetChannelAsync(CancellationToken c = default) => throw new NotImplementedException("TODO: implement OAuth token retrieval and channels.list call.");
    public Task<IReadOnlyList<YouTubeVideoDto>> GetVideosAsync(CancellationToken c = default) => throw new NotImplementedException("TODO: implement videos.list and mapping.");
    public Task<YouTubeVideoDto?> GetVideoAsync(string id, CancellationToken c = default) => throw new NotImplementedException("TODO: implement single video retrieval.");
    public Task UpdateVideoMetadataAsync(UpdateYouTubeVideoMetadataRequest req, CancellationToken c = default) => throw new NotImplementedException("TODO: implement videos.update/thumbnails.set with token refresh.");
}
