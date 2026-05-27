namespace PianoPromoCopilot.Application;
public record OptimizeVideoRequest(string? YouTubeVideoId,string Title,string? Description,string? CompositionName,string? Mood,string? Style,string? Tempo,string? KeySignature,string? TargetAudience,string? StoryBehindComposition,string[]? CurrentTags,string? VideoUrl);
public record ShortsIdeaDto(string Title,string Hook,string SuggestedTimestamp,string Description,string Caption);
public record SocialPostsDto(string Instagram,string TikTok,string Facebook,string Reddit,string X,string LinkedIn,string EmailNewsletter);
public record ComplianceSummaryDto(string RiskLevel,string[] Issues,bool IsSafeToUse);
public record OptimizeVideoResponse(string[] Titles,string[] Descriptions,string[] Tags,string[] Hashtags,string[] ThumbnailIdeas,ShortsIdeaDto[] ShortsIdeas,SocialPostsDto SocialPosts,ComplianceSummaryDto Compliance);
public record AnalyticsRecommendationDto(string RecommendationType,string Message,string Reason,string SuggestedAction,int Priority);
public interface ILlmService { Task<string> GenerateAsync(string systemPrompt,string userPrompt,CancellationToken cancellationToken=default); }
public interface IYouTubeService { Task<object> GetChannelAsync(CancellationToken cancellationToken=default); Task<IReadOnlyList<object>> GetVideosAsync(CancellationToken cancellationToken=default); Task<object?> GetVideoAsync(string videoId,CancellationToken cancellationToken=default); Task UpdateVideoMetadataAsync(object request,CancellationToken cancellationToken=default); }
public class ComplianceReviewService {
  static readonly string[] blocked=["buy views","guaranteed viral","sub for sub","auto comment","bot","fake subscribers","mass dm","spam"];
  public ComplianceSummaryDto Review(IEnumerable<string> texts, string? userContext=null){ var issues=new List<string>(); var joined=string.Join(" ",texts).ToLowerInvariant(); issues.AddRange(blocked.Where(joined.Contains)); if(joined.Contains("world's best pianist") && !(userContext??"").Contains("world's best pianist",StringComparison.OrdinalIgnoreCase)) issues.Add("Unsupported superlative claim"); var risk=issues.Count switch {0=>"Low",<3=>"Medium",_=>"Blocked"}; return new(risk,issues.ToArray(),risk is "Low" or "Medium"); }
}
