using System.Security.Cryptography;
using System.Text;

using Annotate.Markdown;
using Annotate.Plans.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Plans.Tests;

public sealed class RevisionBlockTests
{
    [Fact]
    public async Task RevisionStoresParsedTopLevelBlocks()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string longHeading = new string('H', 2005);
        string markdown =
            "# Title\r\n" +
            "\r\n" +
            "Intro\r\n" +
            "\r\n" +
            "> Quote line\r\n" +
            ">\r\n" +
            "> - child item\r\n" +
            "\r\n" +
            "Tail\r\n" +
            "\r\n" +
            "## " + longHeading + "\r\n" +
            "\r\n" +
            "Under\r\n";

        SubmitOutcome.Created created = await Submit(plans, markdown);
        RevisionDetail detail = Assert.IsType<RevisionDetail>(
            await plans.RevisionAsync(created.Revision, CancellationToken.None));
        PlanDocument document = Assert.IsType<ParseOutcome.Ok>(PlanMarkdown.Parse(markdown)).Document;
        QuoteBlock quote = Assert.Single(document.Blocks.OfType<QuoteBlock>());

        Assert.NotEmpty(quote.Children);
        Assert.Contains(document.Blocks, block => block.SectionPath.Length > 2000);
        Assert.Equal(document.Blocks.Count, detail.Blocks.Count);
        Assert.Equal(detail.Blocks.Count, detail.Blocks.Select(block => block.Key).Distinct().Count());
        Assert.DoesNotContain(
            detail.Blocks,
            block => quote.Children.Any(child => child.Start == block.Start));

        for (int index = 0; index < document.Blocks.Count; index++)
        {
            Block expected = document.Blocks[index];
            RevisionBlock actual = detail.Blocks[index];
            string path = expected.SectionPath.Length <= 2000
                ? expected.SectionPath
                : expected.SectionPath[..2000];

            Assert.Equal(36, actual.Key.Length);
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(path, actual.SectionPath);
            Assert.True(actual.SectionPath.Length <= 2000);
            Assert.Equal(expected.Start, actual.Start);
            Assert.Equal(expected.End, actual.End);
            Assert.Equal(Hash(markdown, expected.Start, expected.End), actual.ContentHash);
            Assert.Equal(BlockChange.Added, actual.Change);
        }

        RevisionBlock storedQuote = Assert.Single(detail.Blocks, block => block.Kind == BlockKind.Quote);
        Assert.True(storedQuote.Start <= quote.Children.Min(child => child.Start));
        Assert.True(storedQuote.End >= quote.Children.Max(child => child.End));
    }

    [Fact]
    public async Task BlockKeysFollowContentThenSectionPosition()
    {
        await using OpenPlans open = await OpenPlans.Open();
        await using ServiceProvider host = open.Plans();
        IPlans plans = host.GetRequiredService<IPlans>();
        string folder = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));
        string first = "edit\n\nmove\n\nstay\n";
        string second = "edited\n\ninserted\n\nmove\n\nstay\n";

        SubmitOutcome.Created created = await Submit(plans, first, folder, null);
        SubmitOutcome.Created continued = await Submit(plans, second, folder, created.Revision.Value);
        RevisionDetail parent = Assert.IsType<RevisionDetail>(
            await plans.RevisionAsync(created.Revision, CancellationToken.None));
        RevisionDetail child = Assert.IsType<RevisionDetail>(
            await plans.RevisionAsync(continued.Revision, CancellationToken.None));

        RevisionBlock edited = child.Blocks[0];
        RevisionBlock inserted = child.Blocks[1];
        RevisionBlock moved = child.Blocks[2];
        RevisionBlock stayed = child.Blocks[3];

        Assert.Equal(parent.Blocks[0].Key, edited.Key);
        Assert.Equal(BlockChange.Changed, edited.Change);
        Assert.Equal(parent.Blocks[2].Key, stayed.Key);
        Assert.Equal(BlockChange.Unchanged, stayed.Change);
        Assert.Equal(parent.Blocks[1].Key, moved.Key);
        Assert.Equal(BlockChange.Unchanged, moved.Change);
        Assert.Equal(BlockChange.Added, inserted.Change);
        Assert.DoesNotContain(parent.Blocks, block => block.Key == inserted.Key);
        Assert.NotEqual(moved.Key, inserted.Key);
        Assert.Equal(4, child.Blocks.Select(block => block.Key).Distinct().Count());
    }

    private static Task<SubmitOutcome.Created> Submit(IPlans plans, string markdown) =>
        Submit(plans, markdown, null, null);

    private static async Task<SubmitOutcome.Created> Submit(
        IPlans plans,
        string markdown,
        string? folder,
        string? parentRevisionId)
    {
        SubmitOutcome outcome = await plans.SubmitAsync(
            new SubmitRevision(markdown, null, folder, null, parentRevisionId, null, null, "Cursor", "claude-opus-4"),
            CancellationToken.None);
        return Assert.IsType<SubmitOutcome.Created>(outcome);
    }

    private static string Hash(string source, int start, int end)
    {
        int hashEnd = end;
        while (hashEnd > start && source[hashEnd - 1] is '\r' or '\n')
        {
            hashEnd--;
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source[start..hashEnd]));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}