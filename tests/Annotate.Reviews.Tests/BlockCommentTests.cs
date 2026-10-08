using Annotate.Reviews.Application;

using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Reviews.Tests;

public sealed class BlockCommentTests
{
    [Fact]
    public async Task AgentNoteTakesRepliesOnOneLevelAndApproveWaitsUntilItIsGone()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();
        ReviewId id = await Opened(reviews);

        Assert.Equal(
            "Review was not found.",
            Refused(await reviews.AddBlockCommentAsync(new ReviewId("missing"), Agent("block-1", "No"), CancellationToken.None)));
        Assert.Equal(
            "Comment was rejected.",
            Refused(await reviews.AddBlockCommentAsync(id, Agent("block-1", "  "), CancellationToken.None)));
        Assert.Equal(
            "Block was not found.",
            Refused(await reviews.AddBlockCommentAsync(id, Agent("  ", "No"), CancellationToken.None)));
        Assert.Empty((await Required(reviews, id)).Annotations);

        Assert.IsType<SaveAnnotationsOutcome.Done>(
            await reviews.AddBlockCommentAsync(id, Agent("block-1", "Use a file."), CancellationToken.None));
        Annotation note = Assert.Single((await Required(reviews, id)).Annotations);
        Assert.Equal(AnnotationKind.Comment, note.Kind);
        Assert.Equal("block-1", note.BlockKey);
        Assert.Equal("Use a file.", note.Comment);
        Assert.Equal(AnnotationAuthor.Agent, note.Author);
        Assert.False(note.Accepted);
        Assert.Null(note.Replies);

        Assert.Equal(
            "Annotation was not found.",
            Refused(await reviews.SetAcceptedAsync(id, "phrase", false, CancellationToken.None)));
        Annotation phrase = new("phrase", AnnotationKind.Comment, "quote", "why", null, 0, 1, 2, "t");
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(id, [phrase], CancellationToken.None));
        Assert.Equal(
            "Accept applies to an agent note.",
            Refused(await reviews.SetAcceptedAsync(id, phrase.Id, true, CancellationToken.None)));
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SetAcceptedAsync(id, note.Id, true, CancellationToken.None));
        Assert.True((await Required(reviews, id)).Annotations.Single(item => item.Id == note.Id).Accepted);
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SetAcceptedAsync(id, note.Id, false, CancellationToken.None));

        Assert.Equal(
            "Reply was rejected.",
            Refused(await reviews.AddReplyAsync(id, note.Id, " ", CancellationToken.None)));
        Assert.Equal(
            "Annotation was not found.",
            Refused(await reviews.AddReplyAsync(id, "missing", "No", CancellationToken.None)));
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.AddReplyAsync(id, note.Id, "Because.", CancellationToken.None));
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.AddReplyAsync(id, phrase.Id, "On the quote.", CancellationToken.None));
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.AddReplyAsync(id, note.Id, "And the path.", CancellationToken.None));
        Annotation replied = (await Required(reviews, id)).Annotations.Single(item => item.Id == note.Id);
        Assert.Equal(["Because.", "And the path."], replied.Replies!.Select(reply => reply.Text).ToArray());
        string firstReply = replied.Replies![0].Id;
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.DeleteReplyAsync(id, note.Id, firstReply, CancellationToken.None));
        Assert.Equal(["And the path."], (await Required(reviews, id)).Annotations.Single(item => item.Id == note.Id).Replies!.Select(reply => reply.Text).ToArray());
        Assert.Equal(
            "Reply was not found.",
            Refused(await reviews.DeleteReplyAsync(id, note.Id, firstReply, CancellationToken.None)));

        Assert.Equal(
            "Annotations must be removed before approval.",
            DecideRefused(await reviews.ApproveAsync(id, CancellationToken.None)));
        Assert.Equal(2, (await Required(reviews, id)).Annotations.Count);

        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.DeleteAnnotationAsync(id, note.Id, CancellationToken.None));
        ReviewDetail afterParent = await Required(reviews, id);
        Assert.Equal(phrase.Id, Assert.Single(afterParent.Annotations).Id);
        Assert.Equal(["On the quote."], afterParent.Annotations[0].Replies!.Select(reply => reply.Text).ToArray());
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.DeleteAnnotationAsync(id, phrase.Id, CancellationToken.None));
        Assert.Empty((await Required(reviews, id)).Annotations);
        Assert.IsType<DecideOutcome.Done>(await reviews.ApproveAsync(id, CancellationToken.None));
        Assert.Equal(
            "Review already decided.",
            Refused(await reviews.AddBlockCommentAsync(id, Agent("block-1", "Later"), CancellationToken.None)));
        Assert.Equal(
            "Review already decided.",
            Refused(await reviews.AddReplyAsync(id, phrase.Id, "Later", CancellationToken.None)));
    }

    [Fact]
    public async Task RequestChangesDropsUnacceptedAgentNotesAndKeepsTheAuthorLine()
    {
        await using OpenReviews open = await OpenReviews.Open();
        await using ServiceProvider host = open.Reviews();
        IReviews reviews = host.GetRequiredService<IReviews>();
        ReviewId id = await Opened(reviews);
        Annotation phrase = new("phrase", AnnotationKind.Comment, "quote", "why", null, 0, 1, 2, "t");
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SaveAnnotationsAsync(id, [phrase], CancellationToken.None));
        Assert.IsType<SaveAnnotationsOutcome.Done>(
            await reviews.AddBlockCommentAsync(id, Agent("block-a", "Use a file."), CancellationToken.None));
        Assert.Equal(
            "A block comment comes from an agent.",
            Refused(await reviews.AddBlockCommentAsync(id, new BlockComment("block-a", "I agree.", AnnotationAuthor.Operator), CancellationToken.None)));
        Assert.IsType<SaveAnnotationsOutcome.Done>(
            await reviews.AddBlockCommentAsync(id, Agent("block-b", "Drop me."), CancellationToken.None));
        string acceptedId = (await Required(reviews, id)).Annotations.Single(item => item.Comment == "Use a file.").Id;
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.SetAcceptedAsync(id, acceptedId, true, CancellationToken.None));
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.AddReplyAsync(id, acceptedId, "Because.", CancellationToken.None));
        Assert.IsType<SaveAnnotationsOutcome.Done>(await reviews.AddReplyAsync(id, phrase.Id, "On the quote.", CancellationToken.None));

        Annotation[] full = new Annotation[100];
        for (int index = 0; index < full.Length; index++)
        {
            full[index] = new("p" + index, AnnotationKind.Deletion, "x", null, null, 0, 1, 2, "t");
        }

        Assert.Equal(
            "Too many annotations.",
            Refused(await reviews.SaveAnnotationsAsync(id, full, CancellationToken.None)));
        Assert.Equal(3, (await Required(reviews, id)).Annotations.Count);

        Assert.IsType<DecideOutcome.Done>(await reviews.RequestChangesAsync(id, [phrase], CancellationToken.None));
        ReviewDetail changed = await Required(reviews, id);
        Assert.Equal(Feedback, changed.Feedback);
        Assert.DoesNotContain("Drop me.", changed.Feedback, StringComparison.Ordinal);
        Assert.Equal(2, changed.Annotations.Count);
        Assert.DoesNotContain(changed.Annotations, item => item.Comment == "Drop me.");

        ReviewId onlyAgent = await Opened(reviews, "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Assert.IsType<SaveAnnotationsOutcome.Done>(
            await reviews.AddBlockCommentAsync(onlyAgent, Agent("block-b", "Drop me."), CancellationToken.None));
        Assert.IsType<DecideOutcome.Done>(await reviews.RequestChangesAsync(onlyAgent, [], CancellationToken.None));
        ReviewDetail dropped = await Required(reviews, onlyAgent);
        Assert.Equal(ReviewStatus.ChangesRequested, dropped.Status);
        Assert.Equal("Plan changes requested.", dropped.Feedback);
        Assert.Empty(dropped.Annotations);
    }

    private const string Feedback =
        """
        ## Plan Review Feedback

        Apply the following anchored review comments before proceeding.

        ### Suggested Changes

        1. {==quote==}}{{>>why<<}}{id="phrase" by="user" at="t"}
        from: operator
        - On the quote.
        2. blockId: block-a
        Use a file.
        from: agent
        - Because.

        Please revise the plan to address this feedback and submit the revised draft again.
        """;

    private static BlockComment Agent(string blockKey, string comment) =>
        new(blockKey, comment, AnnotationAuthor.Agent);

    private static string Refused(SaveAnnotationsOutcome outcome) =>
        Assert.IsType<SaveAnnotationsOutcome.Refused>(outcome).Error;

    private static string DecideRefused(DecideOutcome outcome) =>
        Assert.IsType<DecideOutcome.Refused>(outcome).Error;

    private static async Task<ReviewId> Opened(IReviews reviews, string revision = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
    {
        OpenOutcome.Opened opened = Assert.IsType<OpenOutcome.Opened>(
            await reviews.OpenAsync(new RevisionId(revision), "# Plan\n", CancellationToken.None));
        return opened.Id;
    }

    private static async Task<ReviewDetail> Required(IReviews reviews, ReviewId id)
    {
        ReviewDetail? detail = await reviews.FindAsync(id, CancellationToken.None);
        Assert.NotNull(detail);
        return detail;
    }
}