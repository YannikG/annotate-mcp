using Annotate.Web;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Annotate.Web.Tests;

public sealed class AnnotateApp : WebApplicationFactory<global::Program>
{
    public string DataDirectory { get; } = Path.Combine(Path.GetTempPath(), "annotate-" + Guid.NewGuid().ToString("N"));

    public string DatabasePath => Path.Combine(DataDirectory, "annotate.db");

    public bool? OpenBrowser { get; init; }

    public IReviewBrowser? Browser { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Annotate:DataDir", DataDirectory);
        if (OpenBrowser is bool open)
        {
            builder.UseSetting("Annotate:OpenBrowser", open ? "true" : "false");
        }

        if (Browser is not null)
        {
            IReviewBrowser browser = Browser;
            builder.ConfigureTestServices(services =>
            {
                ServiceDescriptor? registered = services.SingleOrDefault(
                    service => service.ServiceType == typeof(IReviewBrowser));
                if (registered is not null)
                {
                    services.Remove(registered);
                }

                services.AddSingleton(browser);
            });
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (Directory.Exists(DataDirectory))
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
    }
}