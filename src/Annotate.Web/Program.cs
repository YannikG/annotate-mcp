using Annotate.Plans;
using Annotate.Reviews;
using Annotate.Web;
using Annotate.Web.Components;

using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 1024 * 1024;
});

string databasePath = AnnotateDatabase.Path(builder.Configuration);
string connectionString = "Data Source=" + databasePath;
string dataDirectory = Path.GetDirectoryName(databasePath)
    ?? throw new InvalidOperationException("Annotate data directory was not set.");
string[] trustedStoryDomains = builder.Configuration.GetSection("Annotate:TrustedStoryDomains").Get<string[]>() ?? [];

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<PlanTools>();
builder.Services.AddPlans(new PlansSettings(connectionString, trustedStoryDomains));
builder.Services.AddReviews(new ReviewsSettings(connectionString));
builder.Services.AddSingleton<IProjectPlanPages>(new SqliteProjectPlanPages(connectionString));
builder.Services.AddSingleton<IBrowserProcess, ShellBrowserProcess>();
builder.Services.AddSingleton<IReviewBrowser>(provider => new ReviewBrowser(
    ReviewBrowser.CurrentPlatform(),
    provider.GetRequiredService<IBrowserProcess>()));
builder.Services.AddSingleton<IPlanHost, PlanHost>();
builder.Services.AddScoped<ISelectionReader, JsSelectionReader>();
builder.Services.AddScoped<IReportScript, JsReportScript>();
builder.Services.AddSingleton<IAutoClosePreference>(new FileAutoClosePreference(dataDirectory));
builder.Services.AddHostedService<DatabaseStartup>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers.Remove("Server");
        return Task.CompletedTask;
    });

    if (!LoopbackHost.Accepts(context.Request.Host.Value))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    await next(context);
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapHealthChecks("/health");
app.MapMcp("/mcp");
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;