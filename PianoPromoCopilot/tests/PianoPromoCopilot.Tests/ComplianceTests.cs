using PianoPromoCopilot.Application;
using Xunit;
public class ComplianceTests {
 [Fact] public void FlagsBlockedTerms(){ var s=new ComplianceReviewService(); var r=s.Review(new[]{"buy views now"}); Assert.Equal("Medium",r.RiskLevel); Assert.Contains("buy views",r.Issues); }
}
