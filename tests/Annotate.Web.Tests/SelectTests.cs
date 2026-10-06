using AngleSharp.Dom;

using Annotate.Web.Components.Ui;

using Bunit;

namespace Annotate.Web.Tests;

public sealed class SelectTests
{
    private static readonly SelectOption[] Options =
        [new("v1", "v1 · Approved"), new("v2", "v2 · Pending"), new("v3", "v3 · No review")];

    [Fact]
    public void ClosedSelectShowsTheCurrentValueAsALabelledButton()
    {
        using BunitContext context = new();
        var select = Render(context);

        IElement trigger = select.Find("[data-select-trigger]");
        Assert.Equal("v2 · Pending", trigger.TextContent.Trim());
        Assert.Equal("listbox", trigger.GetAttribute("aria-haspopup"));
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));

        IElement label = select.Find(".select-label");
        Assert.Equal("Revision", label.TextContent);
        Assert.Equal($"{label.Id} {trigger.Id}", trigger.GetAttribute("aria-labelledby"));
        Assert.Empty(select.FindAll("[role='listbox']"));
    }

    [Fact]
    public async Task OpeningListsAllOptions()
    {
        using BunitContext context = new();
        var select = Render(context);

        await select.Find("[data-select-trigger]").ClickAsync();

        IElement trigger = select.Find("[data-select-trigger]");
        Assert.Equal("true", trigger.GetAttribute("aria-expanded"));
        IElement list = select.Find("[role='listbox']");
        Assert.Equal(
            ["v1 · Approved", "v2 · Pending", "v3 · No review"],
            list.QuerySelectorAll("[role='option']").Select(option => option.TextContent.Trim()));
        IElement current = list.QuerySelector("[aria-selected='true']")!;
        Assert.Equal("v2 · Pending", current.TextContent.Trim());
        Assert.Equal(current.Id, trigger.GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public async Task ChoosingAnOptionRaisesTheValueAndCloses()
    {
        using BunitContext context = new();
        List<string> chosen = [];
        var select = Render(context, chosen.Add);

        await select.Find("[data-select-trigger]").ClickAsync();
        await select.FindAll("[role='option']")[2].ClickAsync();

        Assert.Equal(["v3"], chosen);
        Assert.Empty(select.FindAll("[role='listbox']"));
        Assert.Equal("false", select.Find("[data-select-trigger]").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task ArrowKeysMoveTheSelectionWhileOpen()
    {
        using BunitContext context = new();
        List<string> chosen = [];
        var select = Render(context, chosen.Add);

        await select.Find("[data-select-trigger]").ClickAsync();
        await select.Find("[data-select-trigger]").KeyDownAsync("ArrowDown");

        Assert.Equal(["v3"], chosen);
        IElement trigger = select.Find("[data-select-trigger]");
        Assert.NotEmpty(select.FindAll("[role='listbox']"));
        IElement active = select.FindAll("[role='option']")[2];
        Assert.Equal(active.Id, trigger.GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public async Task EscapeClosesWithoutChangingTheValue()
    {
        using BunitContext context = new();
        List<string> chosen = [];
        var select = Render(context, chosen.Add);

        await select.Find("[data-select-trigger]").ClickAsync();
        await select.Find("[data-select-trigger]").KeyDownAsync("Escape");

        Assert.Empty(chosen);
        Assert.Empty(select.FindAll("[role='listbox']"));
    }

    [Fact]
    public async Task BackdropClosesWithoutChangingTheValue()
    {
        using BunitContext context = new();
        List<string> chosen = [];
        var select = Render(context, chosen.Add);

        await select.Find("[data-select-trigger]").ClickAsync();
        await select.Find("[data-select-dismiss]").ClickAsync();

        Assert.Empty(chosen);
        Assert.Empty(select.FindAll("[role='listbox']"));
    }

    [Fact]
    public void ExtraAttributesSplatOntoTheWrapper()
    {
        using BunitContext context = new();
        var select = context.Render<Select>(parameters => parameters
            .Add(component => component.Label, "Revision")
            .Add(component => component.Value, "v2")
            .Add(component => component.Options, Options)
            .AddUnmatched("data-revision", ""));

        Assert.NotNull(select.Find(".select[data-revision]"));
    }

    private static IRenderedComponent<Select> Render(BunitContext context, Action<string>? onChange = null) =>
        context.Render<Select>(parameters => parameters
            .Add(component => component.Label, "Revision")
            .Add(component => component.Value, "v2")
            .Add(component => component.Options, Options)
            .Add(component => component.ValueChanged, value => onChange?.Invoke(value)));
}