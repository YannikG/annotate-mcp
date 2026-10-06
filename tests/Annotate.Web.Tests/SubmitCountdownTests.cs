using Annotate.Web;

namespace Annotate.Web.Tests;

public sealed class SubmitCountdownTests
{
    [Fact]
    public void CountdownStartsTicksCancelsAndCompletes()
    {
        SubmitCountdown countdown = new();
        Assert.Equal(5, countdown.Count);
        Assert.False(countdown.Running);
        Assert.False(countdown.Complete);

        countdown.Tick();
        Assert.Equal(4, countdown.Count);
        Assert.False(countdown.Complete);

        countdown.Start();
        Assert.Equal(5, countdown.Count);
        Assert.True(countdown.Running);
        Assert.False(countdown.Complete);

        countdown.Tick();
        Assert.Equal(4, countdown.Count);
        Assert.True(countdown.Running);
        Assert.False(countdown.Complete);

        countdown.Tick();
        countdown.Tick();
        countdown.Tick();
        Assert.Equal(1, countdown.Count);
        Assert.False(countdown.Complete);

        countdown.Tick();
        Assert.Equal(0, countdown.Count);
        Assert.True(countdown.Complete);

        countdown.Tick();
        Assert.Equal(0, countdown.Count);
        Assert.True(countdown.Complete);

        countdown.Cancel();
        Assert.Equal(5, countdown.Count);
        Assert.False(countdown.Running);
        Assert.False(countdown.Complete);
    }
}