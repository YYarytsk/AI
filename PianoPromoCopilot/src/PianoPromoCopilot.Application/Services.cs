using System.Text.Json;
namespace PianoPromoCopilot.Application;
public class ComplianceReviewService {
  private static readonly string[] blocked=["buy views","guaranteed viral","sub for sub","auto comment","bot","fake subscribers","mass dm","spam"];
  public ComplianceSummaryDto Review(IEnumerable<string> texts, string? sourceContext=null){
    var corpus=string.Join(" ",texts).ToLowerInvariant(); var issues=new List<string>();
    issues.AddRange(blocked.Where(corpus.Contains));
    if(corpus.Contains("world's best pianist") && !(sourceContext??"").Contains("world's best pianist",StringComparison.OrdinalIgnoreCase)) issues.Add("unsupported claim: world's best pianist");
    var risk=issues.Count==0?"Low":issues.Any(i=>i.Contains("buy views")||i.Contains("fake subscribers")||i.Contains("bot"))?"Blocked":issues.Count>2?"High":"Medium";
    return new(risk,issues.Distinct().ToArray(),risk is "Low" or "Medium");
  }
}
public class VideoOptimizationService {
  private readonly ILlmService _llm; private readonly ComplianceReviewService _compliance;
  public VideoOptimizationService(ILlmService llm, ComplianceReviewService compliance){_llm=llm;_compliance=compliance;}
  public async Task<OptimizeVideoResponse> OptimizeAsync(OptimizeVideoRequest req, CancellationToken ct=default){
    var systemPrompt="You are a YouTube growth strategist for an independent pianist who publishes original piano compositions.";
    var userPrompt=$"Title: {req.Title}\nDescription:{req.Description}";
    var raw=await _llm.GenerateAsync(systemPrompt,userPrompt,ct);
    LlmOptimizationPayload? payload=null;
    try { payload=JsonSerializer.Deserialize<LlmOptimizationPayload>(raw,new JsonSerializerOptions{PropertyNameCaseInsensitive=true}); } catch {}
    payload ??= new LlmOptimizationPayload{Titles=[req.Title],Descriptions=[req.Description??""],Tags=req.CurrentTags?.ToList()??[],Hashtags=["#piano"],ThumbnailIdeas=["Hands at keyboard"],ShortsIdeas=[new("Opening motif","Listen to the first phrase","00:00","Show opening bars","New original piano")],SocialPosts=new("New piano piece live","Original piano short","New composition","Sharing an original composition","New piano release","Original piano work published","A new piece is live")};
    var review=_compliance.Review(payload.Titles.Concat(payload.Descriptions).Concat(payload.Tags).Concat(payload.Hashtags).Concat(payload.ThumbnailIdeas).Append(payload.SocialPosts.Instagram));
    return new(payload.Titles.ToArray(),payload.Descriptions.ToArray(),payload.Tags.ToArray(),payload.Hashtags.ToArray(),payload.ThumbnailIdeas.ToArray(),payload.ShortsIdeas.ToArray(),payload.SocialPosts,review);
  }
}
public class AnalyticsRecommendationService {
  public IReadOnlyList<AnalyticsRecommendationDto> Analyze(IReadOnlyList<(long? views,long? likes,long? comments,int? avd,long? impressions,decimal? ctr,string? source,string? country)> snaps){
    var list=new List<AnalyticsRecommendationDto>(); if(snaps.Count==0){list.Add(new("Data","Collect baseline","No analytics snapshots available","Wait 48-72 hours then create initial promotion drafts",1)); return list;}
    var last=snaps.Last(); if((last.views??0)>0 && ((last.likes??0)+(last.comments??0)) < (last.views??0)/50) list.Add(new("Engagement","Improve call-to-action","Views are not converting to engagement","Ask for a comment prompt related to composition mood",2));
    if((last.avd??0)>120 && ((last.impressions??0)<500 || (last.ctr??0)<0.03m)) list.Add(new("Packaging","Improve title/thumbnail","Retention is decent but discovery is weak","Test two new compliant title variants and thumbnail concepts",1));
    if((last.ctr??0)>0.06m && (last.avd??999)<60) list.Add(new("Intro","Strengthen first 15 seconds","Click-through is high but early retention is low","Start with strongest motif and immediate value framing",1));
    if(!string.IsNullOrWhiteSpace(last.country)) list.Add(new("Localization","Localize one promo line","Top country signal detected","Add one localized caption line for top country",3));
    if(string.Equals(last.source,"search",StringComparison.OrdinalIgnoreCase)) list.Add(new("SEO","Expand related keyword set","Search is working","Generate related SEO title/tag variants",2));
    if(string.Equals(last.source,"external",StringComparison.OrdinalIgnoreCase)) list.Add(new("Distribution","Double down on external sharing","External source performs well","Plan additional manual shares in relevant music communities",2));
    return list;
  }
}
