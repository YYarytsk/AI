using FluentAssertions;
using PianoPromoCopilot.Application;
using Xunit;
public class ComplianceTests {
 [Fact] public void FlagsBlockedTerms(){ var s=new ComplianceReviewService(); var r=s.Review(["buy views now"]); r.RiskLevel.Should().Be("Blocked"); r.Issues.Should().Contain(i=>i.Contains("buy views")); }
 [Fact] public void LowRiskForNeutralText(){ var s=new ComplianceReviewService(); var r=s.Review(["original piano composition in A minor"]); r.RiskLevel.Should().Be("Low"); }
}
