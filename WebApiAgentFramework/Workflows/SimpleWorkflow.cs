using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace WebApiAgentFramework.Workflows;

public class SimpleWorkflow(IChatClient chatClient)
{
    public async Task Run()
    {
        Console.WriteLine("Running simple workflow...");

        // Create the executors
        Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
        var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

        ReverseTextExecutor reverse = new();

        // agent wrapper
        AIAgent frenchAgent = GetTranslationAgent("French", chatClient);
        AIAgent spanishAgent = GetTranslationAgent("Spanish", chatClient);

        // Build the workflow by connecting executors sequentially
        WorkflowBuilder builder = new(uppercase);
        builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
        builder.AddFanOutEdge(uppercase, [frenchAgent, spanishAgent]);

        var workflow = builder.Build();

        // Execute the workflow with input data
        await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");
        foreach (WorkflowEvent evt in run.NewEvents)
        {
            if (evt is ExecutorCompletedEvent executorComplete)
            {
                Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
            }
        }

        // Visualize the workflow
        // Mermaid
        string mermaid = workflow.ToMermaidString();

        // DOT format
        string dotString = workflow.ToDotString();
    }

    private static ChatClientAgent GetTranslationAgent(
        string targetLanguage,
        IChatClient chatClient
    ) =>
        new(
            chatClient,
            $"You are a translation assistant that translates the provided text to {targetLanguage}."
        );
}

internal sealed class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
{
    public override ValueTask<string> HandleAsync(
        string message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default
    )
    {
        // Because we do not suppress it, the returned result will be yielded as an output from this executor.
        return ValueTask.FromResult(string.Concat(message.Reverse()));
    }
}
