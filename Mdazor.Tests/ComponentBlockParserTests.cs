using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class ComponentBlockParserTests
{
    private readonly MarkdownPipeline _pipeline;

    public ComponentBlockParserTests()
    {
        var services = new ServiceCollection();
        services.AddMdazor();

        var serviceProvider = services.BuildServiceProvider();
        var componentRegistry = serviceProvider.GetRequiredService<IComponentRegistry>();
        _pipeline = new MarkdownPipelineBuilder()
            .UseMdazor(serviceProvider)
            .Build();
    }

    [Fact]
    public void ParseSelfClosingComponent_ShouldCreateComponentBlock()
    {
        var markdown = """
                       <Card title="Test" />
                       """;
        
        var document = Markdown.Parse(markdown, _pipeline);
        
        document.Count.ShouldBe(1);
        var block = document[0].ShouldBeOfType<ComponentBlock>();
        block.ComponentName.ShouldBe("Card");
        block.IsSelfClosing.ShouldBeTrue();
        block.Attributes.ShouldContainKeyAndValue("title", "Test");
    }

    [Fact]
    public void ParseOpeningAndClosingTags_ShouldCreateComponentBlock()
    {
        var markdown = """
                       <Card title="Test">
                       Content here
                       </Card>
                       """;
        
        var document = Markdown.Parse(markdown, _pipeline);
        
        document.Count.ShouldBe(1);
        var block = document[0].ShouldBeOfType<ComponentBlock>();
        block.ComponentName.ShouldBe("Card");
        block.IsSelfClosing.ShouldBeFalse();
        block.Attributes.ShouldContainKeyAndValue("title", "Test");
    }

    [Fact]
    public void ParseMultipleAttributes_ShouldParseAllAttributes()
    {
        var markdown = """
                       <Alert type="warning" dismissible="true" />
                       """;
        
        var document = Markdown.Parse(markdown, _pipeline);
        
        var block = document[0].ShouldBeOfType<ComponentBlock>();
        block.Attributes.ShouldContainKeyAndValue("type", "warning");
        block.Attributes.ShouldContainKeyAndValue("dismissible", "true");
    }

    [Fact]
    public void ParseInvalidTag_ShouldNotCreateComponentBlock()
    {
        var markdown = """
                       <div>Not a component</div>
                       """;
        
        var document = Markdown.Parse(markdown, _pipeline);
        
        document[0].ShouldNotBeOfType<ComponentBlock>();
    }

    [Fact]
    public void ParseUnknownComponent_ShouldCreateComponentBlock()
    {
        var markdown = """
                       <UnknownComponent attr="value" />
                       """;
        
        var document = Markdown.Parse(markdown, _pipeline);
        
        document.Count.ShouldBe(1);
        var block = document[0].ShouldBeOfType<ComponentBlock>();
        block.ComponentName.ShouldBe("UnknownComponent");
        block.IsSelfClosing.ShouldBeTrue();
        block.Attributes.ShouldContainKeyAndValue("attr", "value");
    }
}