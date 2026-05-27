using Microsoft.EntityFrameworkCore;
using PianoPromoCopilot.Application;
using PianoPromoCopilot.Domain;
using PianoPromoCopilot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddPolicy("dev", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? builder.Configuration["ConnectionStrings__DefaultConnection"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("PianoPromoCopilot"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
}

builder.Services.AddScoped<ComplianceReviewService>();
builder.Services.AddScoped<VideoOptimizationService>();
builder.Services.AddScoped<AnalyticsRecommendationService>();

if (string.IsNullOrWhiteSpace(builder.Configuration["OpenAI:ApiKey"] ?? builder.Configuration["OpenAI__ApiKey"]))
    builder.Services.AddScoped<ILlmService, MockLlmService>();
else
    builder.Services.AddScoped<ILlmService, OpenAiLlmService>();

if (bool.TryParse(builder.Configuration["Features:UseMockYouTube"] ?? builder.Configuration["Features__UseMockYouTube"], out var useMock) && useMock)
    builder.Services.AddScoped<IYouTubeService, MockYouTubeService>();
else
    builder.Services.AddScoped<IYouTubeService, GoogleYouTubeService>();

var app = builder.Build();
app.UseCors("dev");
app.MapOpenApi();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.Initialize(db);
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", mockMode = true }));

app.MapGet("/api/videos", async (AppDbContext db, CancellationToken ct) =>
    Results.Ok(await db.YouTubeVideos.OrderByDescending(v => v.CreatedAt).ToListAsync(ct)));

app.MapGet("/api/videos/{youtubeVideoId}", async (string youtubeVideoId, AppDbContext db, CancellationToken ct) =>
{
    var v = await db.YouTubeVideos.FirstOrDefaultAsync(x => x.YouTubeVideoId == youtubeVideoId, ct);
    return v is null ? Results.NotFound() : Results.Ok(v);
});

app.MapPost("/api/videos", async (YouTubeVideo video, AppDbContext db, CancellationToken ct) =>
{
    db.YouTubeVideos.Add(video);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/videos/{video.YouTubeVideoId}", video);
});

app.MapPut("/api/videos/{youtubeVideoId}", async (string youtubeVideoId, YouTubeVideo input, AppDbContext db, CancellationToken ct) =>
{
    var v = await db.YouTubeVideos.FirstOrDefaultAsync(x => x.YouTubeVideoId == youtubeVideoId, ct);
    if (v is null) return Results.NotFound();
    v.Title = input.Title;
    v.Description = input.Description;
    v.Mood = input.Mood;
    v.Style = input.Style;
    v.TargetAudience = input.TargetAudience;
    v.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.Ok(v);
});

app.MapPost("/api/videos/optimize", async (OptimizeVideoRequest req, VideoOptimizationService svc, AppDbContext db, CancellationToken ct) =>
{
    var result = await svc.OptimizeAsync(req, ct);

    if (!string.IsNullOrWhiteSpace(req.YouTubeVideoId))
    {
        var suggestions = new List<VideoOptimizationSuggestion>();
        suggestions.AddRange(result.Titles.Select(t => new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "Title", Platform = "YouTube", SuggestionText = t }));
        suggestions.AddRange(result.Descriptions.Select(d => new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "Description", Platform = "YouTube", SuggestionText = d }));
        suggestions.AddRange(result.Tags.Select(t => new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "Tags", Platform = "YouTube", SuggestionText = t }));
        suggestions.AddRange(result.Hashtags.Select(h => new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "Hashtags", Platform = "YouTube", SuggestionText = h }));
        suggestions.AddRange(result.ThumbnailIdeas.Select(t => new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "ThumbnailIdea", Platform = "YouTube", SuggestionText = t }));
        suggestions.AddRange(result.ShortsIdeas.Select(s => new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "ShortsIdea", Platform = "YouTube", SuggestionText = $"{s.Title} | {s.Hook} | {s.Caption}" }));
        suggestions.AddRange(new[]
        {
            new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "SocialPost", Platform = "Instagram", SuggestionText = result.SocialPosts.Instagram },
            new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "SocialPost", Platform = "TikTok", SuggestionText = result.SocialPosts.TikTok },
            new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "SocialPost", Platform = "Facebook", SuggestionText = result.SocialPosts.Facebook },
            new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "SocialPost", Platform = "Reddit", SuggestionText = result.SocialPosts.Reddit },
            new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "SocialPost", Platform = "X", SuggestionText = result.SocialPosts.X },
            new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "SocialPost", Platform = "LinkedIn", SuggestionText = result.SocialPosts.LinkedIn },
            new VideoOptimizationSuggestion { YouTubeVideoId = req.YouTubeVideoId!, SuggestionType = "SocialPost", Platform = "Email", SuggestionText = result.SocialPosts.EmailNewsletter }
        });

        db.VideoOptimizationSuggestions.AddRange(suggestions);
        await db.SaveChangesAsync(ct);
    }

    return Results.Ok(result);
});

app.MapGet("/api/videos/{youtubeVideoId}/suggestions", async (string youtubeVideoId, AppDbContext db, CancellationToken ct) =>
    Results.Ok(await db.VideoOptimizationSuggestions.Where(s => s.YouTubeVideoId == youtubeVideoId).OrderByDescending(s => s.CreatedAt).ToListAsync(ct)));

app.MapPost("/api/videos/{youtubeVideoId}/suggestions/{suggestionId:int}/approve", async (string youtubeVideoId, int suggestionId, AppDbContext db, CancellationToken ct) =>
{
    var suggestion = await db.VideoOptimizationSuggestions.FirstOrDefaultAsync(s => s.Id == suggestionId && s.YouTubeVideoId == youtubeVideoId, ct);
    if (suggestion is null) return Results.NotFound();
    suggestion.IsApproved = true;
    suggestion.IsRejected = false;
    suggestion.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.Ok(suggestion);
});

app.MapPost("/api/videos/{youtubeVideoId}/suggestions/{suggestionId:int}/reject", async (string youtubeVideoId, int suggestionId, AppDbContext db, CancellationToken ct) =>
{
    var suggestion = await db.VideoOptimizationSuggestions.FirstOrDefaultAsync(s => s.Id == suggestionId && s.YouTubeVideoId == youtubeVideoId, ct);
    if (suggestion is null) return Results.NotFound();
    suggestion.IsApproved = false;
    suggestion.IsRejected = true;
    suggestion.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.Ok(suggestion);
});

app.MapGet("/api/videos/{youtubeVideoId}/promotion-drafts", async (string youtubeVideoId, AppDbContext db, CancellationToken ct) =>
    Results.Ok(await db.PromotionDrafts.Where(d => d.YouTubeVideoId == youtubeVideoId).OrderByDescending(d => d.CreatedAt).ToListAsync(ct)));

app.MapPost("/api/videos/{youtubeVideoId}/promotion-drafts", async (string youtubeVideoId, AppDbContext db, CancellationToken ct) =>
{
    var sourceSuggestions = await db.VideoOptimizationSuggestions
        .Where(s => s.YouTubeVideoId == youtubeVideoId && s.SuggestionType == "SocialPost")
        .ToListAsync(ct);

    if (sourceSuggestions.Count == 0)
        return Results.BadRequest(new { message = "No social post suggestions found. Run optimize first." });

    var drafts = sourceSuggestions.Select(s => new PromotionDraft
    {
        YouTubeVideoId = youtubeVideoId,
        Platform = s.Platform ?? "Unknown",
        DraftText = s.SuggestionText,
        Status = "Draft",
        CreatedAt = DateTime.UtcNow
    }).ToList();

    db.PromotionDrafts.AddRange(drafts);
    await db.SaveChangesAsync(ct);
    return Results.Ok(drafts);
});

app.MapGet("/api/videos/{youtubeVideoId}/analytics", async (string youtubeVideoId, AppDbContext db, CancellationToken ct) =>
    Results.Ok(await db.VideoAnalyticsSnapshots.Where(x => x.YouTubeVideoId == youtubeVideoId).ToListAsync(ct)));

app.MapPost("/api/videos/{youtubeVideoId}/recommendations", async (string youtubeVideoId, AppDbContext db, AnalyticsRecommendationService svc, CancellationToken ct) =>
{
    var snaps = await db.VideoAnalyticsSnapshots.Where(x => x.YouTubeVideoId == youtubeVideoId).OrderBy(x => x.SnapshotDate).ToListAsync(ct);
    var data = snaps.Select(s => (s.Views, s.Likes, s.Comments, s.AverageViewDurationSeconds, s.Impressions, s.ImpressionClickThroughRate, s.TrafficSource, s.Country)).ToList();
    return Results.Ok(svc.Analyze(data));
});

app.MapGet("/api/youtube/channel", async (IYouTubeService yt, CancellationToken ct) =>
    Results.Ok(await yt.GetChannelAsync(ct)));

app.MapGet("/api/youtube/videos", async (IYouTubeService yt, CancellationToken ct) =>
    Results.Ok(await yt.GetVideosAsync(ct)));

app.Run();

public partial class Program;
