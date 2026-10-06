using Annotate.Reviews.Application;
using Annotate.Web.Components.Ui;

namespace Annotate.Web.Tests;

public sealed class StatusToneTests
{
    [Theory]
    [InlineData(ReviewStatus.Pending, "Pending", StatusTone.Pending)]
    [InlineData(ReviewStatus.Approved, "Approved", StatusTone.Approved)]
    [InlineData(ReviewStatus.ChangesRequested, "Changes requested", StatusTone.Changes)]
    public void MapsEachStatusToItsLabelAndTone(ReviewStatus status, string label, string tone)
    {
        Assert.Equal(label, StatusTone.Label(status));
        Assert.Equal(tone, StatusTone.Of(status));
    }

    [Fact]
    public void MissingReviewReadsAsNoReviewWithNeutralTone()
    {
        Assert.Equal("No review", StatusTone.Label(null));
        Assert.Equal(StatusTone.None, StatusTone.Of(null));
    }

    [Theory]
    [InlineData("Pending", StatusTone.Pending)]
    [InlineData("Approved", StatusTone.Approved)]
    [InlineData("Changes requested", StatusTone.Changes)]
    [InlineData("No review", StatusTone.None)]
    [InlineData("anything else", StatusTone.None)]
    public void LabelsMapBackToTones(string label, string tone) =>
        Assert.Equal(tone, StatusTone.OfLabel(label));
}