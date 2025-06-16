using Microsoft.AspNetCore.Components;
using Shouldly;

namespace Mdazor.Tests;

public class ComponentRegistryTests
{
    [Fact]
    public void RegisterComponent_ShouldRegisterComponentByName()
    {
        var registry = new ComponentRegistry();
        
        registry.RegisterComponent<TestComponent>();
        
        registry.IsRegistered("TestComponent").ShouldBeTrue();
        registry.GetComponentType("TestComponent").ShouldBe(typeof(TestComponent));
    }

    [Fact]
    public void RegisterComponent_WithCustomName_ShouldRegisterComponentByCustomName()
    {
        var registry = new ComponentRegistry();
        
        registry.RegisterComponent<TestComponent>("CustomName");
        
        registry.IsRegistered("CustomName").ShouldBeTrue();
        registry.GetComponentType("CustomName").ShouldBe(typeof(TestComponent));
    }

    [Fact]
    public void GetComponentType_WithUnknownName_ShouldReturnNull()
    {
        var registry = new ComponentRegistry();
        
        registry.GetComponentType("Unknown").ShouldBeNull();
    }

    [Fact]
    public void IsRegistered_WithUnknownName_ShouldReturnFalse()
    {
        var registry = new ComponentRegistry();
        
        registry.IsRegistered("Unknown").ShouldBeFalse();
    }

    private class TestComponent : ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Test Component");
        }
    }
}