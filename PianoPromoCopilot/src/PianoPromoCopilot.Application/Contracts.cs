using System.Text.Json.Serialization;
namespace PianoPromoCopilot.Application;
public record OptimizeVideoRequest(string? YouTubeVideoId,string Title,string? Description,string? CompositionName,string? Mood,string? Style,string? Tempo,string? KeySignature,string? TargetAudience,string? StoryBehindComposition,string[]? CurrentTags,string? VideoUrl);
public record ShortsIdeaDto(string Title,string Hook,string SuggestedTimestamp,string Description,string Caption);
public record SocialPostsDto(string Instagram,string TikTok,string Facebook,string Reddit,string X,string LinkedIn,string EmailNewsletter);
public record ComplianceSummaryDto(string RiskLevel,string[] Issues,bool IsSafeToUse);
public record OptimizeVideoResponse(string[] Titles,string[] Descriptions,string[] Tags,string[] Hashtags,string[] ThumbnailIdeas,ShortsIdeaDto[] ShortsIdeas,SocialPostsDto SocialPosts,ComplianceSummaryDto Compliance);
public record AnalyticsRecommendationDto(string RecommendationType,string Message,string Reason,string SuggestedAction,int Priority);
public interface ILlmService { Task<string> GenerateAsync(string systemPrompt,string userPrompt,CancellationToken cancellationToken=default); }
public interface IYouTubeService { Task<YouTubeChannelDto> GetChannelAsync(CancellationToken cancellationToken=default); Task<IReadOnlyList<YouTubeVideoDto>> GetVideosAsync(CancellationToken cancellationToken=default); Task<YouTubeVideoDto?> GetVideoAsync(string videoId,CancellationToken cancellationToken=default); Task UpdateVideoMetadataAsync(UpdateYouTubeVideoMetadataRequest request,CancellationToken cancellationToken=default); }
public record YouTubeChannelDto(string ChannelId,string ChannelTitle,string? Description,string? ThumbnailUrl,bool IsConnected);
public record YouTubeVideoDto(string YouTubeVideoId,string Title,string? Description,long? ViewCount,long? LikeCount,long? CommentCount,DateTime? PublishedAt,string? ThumbnailUrl);
public record UpdateYouTubeVideoMetadataRequest(string YouTubeVideoId,string? Title,string? Description,string[]? Tags,string? ThumbnailPath);
public sealed class LlmOptimizationPayload { [JsonPropertyName("titles")] public List<string> Titles {get;set;}=[]; [JsonPropertyName("descriptions")] public List<string> Descriptions{get;set;}=[]; [JsonPropertyName("tags")] public List<string> Tags{get;set;}=[]; [JsonPropertyName("hashtags")] public List<string> Hashtags{get;set;}=[]; [JsonPropertyName("thumbnailIdeas")] public List<string> ThumbnailIdeas{get;set;}=[]; [JsonPropertyName("shortsIdeas")] public List<ShortsIdeaDto> ShortsIdeas{get;set;}=[]; [JsonPropertyName("socialPosts")] public SocialPostsDto SocialPosts {get;set;} = new("","","","","","",""); }
