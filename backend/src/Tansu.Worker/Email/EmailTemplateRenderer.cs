using RazorLight;

namespace Tansu.Worker.Email;

public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync<TModel>(string templateName, TModel model);
}

public sealed class RazorLightEmailRenderer : IEmailTemplateRenderer
{
    private readonly IRazorLightEngine _engine;

    public RazorLightEmailRenderer()
    {
        var templatesRoot = Path.Combine(AppContext.BaseDirectory, "Templates");
        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(templatesRoot)
            .UseMemoryCachingProvider()
            .Build();
    }

    public Task<string> RenderAsync<TModel>(string templateName, TModel model) =>
        _engine.CompileRenderAsync(templateName, model);
}
