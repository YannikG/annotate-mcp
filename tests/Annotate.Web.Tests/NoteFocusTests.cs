using Annotate.Plans.Application;
using Annotate.Reviews.Application;
using Annotate.Web;
using Annotate.Web.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class NoteFocusTests
{
    [Fact]
    public async Task OpeningANoteFocusesTheTextarea()
    {
        FakePlans plans = new();
        plans.Revisions["revision"] = new(new("revision"), new PlanId("plan"), 1, "# Keep me", null, null, null, [], null);
        FakeReviews reviews = new()
        {
            Review = new(new("review"), new("revision"), ReviewStatus.Pending, null, [], [], [], DateTimeOffset.UnixEpoch, null),
        };
        using BunitContext context = BrowseHost.Open(plans, reviews);
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton<ISelectionReader>(new FakeSelection { Next = new(0, 2, 6, "Keep") });
        IRenderedComponent<ReviewPage> page = context.Render<ReviewPage>(parameters =>
            parameters.Add(component => component.Id, "review"));
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[data-review]")));

        await page.Find("[data-review]").KeyDownAsync("c");

        page.Find("[data-note-dialog]");
        Assert.Contains(
            context.JSInterop.Invocations,
            invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }
}