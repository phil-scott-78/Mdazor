using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mdazor.Tests;

public class ThreadSafetyTests
{
    [Fact]
    public void MultipleServiceCollections_ShouldNotInterfereWithEachOther()
    {
        // Create two separate service collections with different components
        var services1 = new ServiceCollection();
        services1.AddLogging();
        services1.AddMdazor()
            .AddMdazorComponent<TestComponentA>();

        var services2 = new ServiceCollection();
        services2.AddLogging();
        services2.AddMdazor()
            .AddMdazorComponent<TestComponentB>();

        // Build service providers
        var serviceProvider1 = services1.BuildServiceProvider();
        var serviceProvider2 = services2.BuildServiceProvider();

        // Get component registries
        var registry1 = serviceProvider1.GetRequiredService<IComponentRegistry>();
        var registry2 = serviceProvider2.GetRequiredService<IComponentRegistry>();

        // Verify each registry only has its own component
        registry1.IsRegistered("TestComponentA").ShouldBeTrue();
        registry1.IsRegistered("TestComponentB").ShouldBeFalse();

        registry2.IsRegistered("TestComponentB").ShouldBeTrue();
        registry2.IsRegistered("TestComponentA").ShouldBeFalse();
    }

    [Fact]
    public void ConcurrentServiceCollectionCreation_ShouldNotCauseRaceConditions()
    {
        const int threadCount = 10;
        const int iterationsPerThread = 5;
        var tasks = new List<Task>();
        var results = new List<(bool hasA, bool hasB)>[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            results[threadIndex] = new List<(bool, bool)>();
            
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    // Create alternating service collections
                    var services = new ServiceCollection();
                    services.AddLogging();
                    services.AddMdazor();

                    if (threadIndex % 2 == 0)
                    {
                        services.AddMdazorComponent<TestComponentA>();
                    }
                    else
                    {
                        services.AddMdazorComponent<TestComponentB>();
                    }

                    var serviceProvider = services.BuildServiceProvider();
                    var registry = serviceProvider.GetRequiredService<IComponentRegistry>();

                    var hasA = registry.IsRegistered("TestComponentA");
                    var hasB = registry.IsRegistered("TestComponentB");
                    
                    results[threadIndex].Add((hasA, hasB));
                }
            }));
        }

#pragma warning disable xUnit1031
        Task.WaitAll(tasks.ToArray());
#pragma warning restore xUnit1031

        // Verify results - even-indexed threads should only have A, odd should only have B
        for (int i = 0; i < threadCount; i++)
        {
            foreach (var (hasA, hasB) in results[i])
            {
                if (i % 2 == 0)
                {
                    // Even threads should only have TestComponentA
                    hasA.ShouldBeTrue($"Thread {i} should have TestComponentA");
                    hasB.ShouldBeFalse($"Thread {i} should not have TestComponentB");
                }
                else
                {
                    // Odd threads should only have TestComponentB
                    hasA.ShouldBeFalse($"Thread {i} should not have TestComponentA");
                    hasB.ShouldBeTrue($"Thread {i} should have TestComponentB");
                }
            }
        }
    }

    // Test components for thread safety testing
    private class TestComponentA : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Component A");
        }
    }

    private class TestComponentB : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Component B");
        }
    }
}