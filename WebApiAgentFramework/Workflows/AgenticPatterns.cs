using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace WebApiAgentFramework.Workflows;

public class AgenticPatterns(IChatClient chatClient)
{
    public async Task Run()
    {
        Console.Write(
            "Choose workflow type ('sequential', 'concurrent', 'handoffs', 'groupchat'): "
        );
        switch (Console.ReadLine())
        {
            // Sequential workflow: agents execute one after another, each with the full context of the conversation so far.
            // Good for when you want to have a series of steps that build on each other.

            case "sequential":
                await RunWorkflowAsync(
                    AgentWorkflowBuilder.BuildSequential(
                        from lang in (string[])["French", "Spanish", "English"]
                        select GetTranslationAgent(lang, chatClient)
                    ),
                    [new(ChatRole.User, "Hello, world!")]
                );
                break;

            // Concurrent workflow: agents execute in parallel, each with the same initial context of the conversation.
            // Responses are returned as they come in.

            case "concurrent":
                await RunWorkflowAsync(
                    AgentWorkflowBuilder.BuildConcurrent(
                        from lang in (string[])["French", "Spanish", "English"]
                        select GetTranslationAgent(lang, chatClient)
                    ),
                    [new(ChatRole.User, "Hello, world!")]
                );
                break;

            // Handoff workflow: a triage agent routes messages to specialist agents based on the content of the user's message.
            // The triage agent can also receive messages back from the specialists.

            case "handoffs":
                ChatClientAgent historyTutor = new(
                    chatClient,
                    "You provide assistance with historical queries. Explain important events and context clearly. Only respond about history.",
                    "history_tutor",
                    "Specialist agent for historical questions"
                );
                ChatClientAgent mathTutor = new(
                    chatClient,
                    "You provide help with math problems. Explain your reasoning at each step and include examples. Only respond about math.",
                    "math_tutor",
                    "Specialist agent for math questions"
                );
                ChatClientAgent triageAgent = new(
                    chatClient,
                    "You determine which agent to use based on the user's homework question. ALWAYS handoff to another agent.",
                    "triage_agent",
                    "Routes messages to the appropriate specialist agent"
                );
                var workflow = AgentWorkflowBuilder
                    .CreateHandoffBuilderWith(triageAgent)
                    .WithHandoffs(triageAgent, [mathTutor, historyTutor])
                    .WithHandoffs([mathTutor, historyTutor], triageAgent)
                    .Build();

                List<ChatMessage> messages = [];
                while (true)
                {
                    Console.Write("Q: ");
                    messages.Add(new(ChatRole.User, Console.ReadLine()));
                    messages.AddRange(await RunWorkflowAsync(workflow, messages));
                }

            // Group chat workflow: all agents participate in a shared conversation, responding to each other and the user in a round-robin fashion.

            case "groupchat":
                await RunWorkflowAsync(
                    AgentWorkflowBuilder
                        .CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents)
                        {
                            MaximumIterationCount = 5,
                        })
                        .AddParticipants(
                            from lang in (string[])["French", "Spanish", "English"]
                            select GetTranslationAgent(lang, chatClient)
                        )
                        .WithName("Translation Round Robin Workflow")
                        .WithDescription(
                            "A workflow where three translation agents take turns responding in a round-robin fashion."
                        )
                        .Build(),
                    [new(ChatRole.User, "Hello, world!")]
                );
                break;

            default:
                throw new InvalidOperationException("Invalid workflow type.");
        }
    }

    private ChatClientAgent GetTranslationAgent(string targetLanguage, IChatClient chatClient) =>
        new(
            chatClient,
            $"You are a translation assistant who only responds in {targetLanguage}. Respond to any "
                + $"input by outputting the name of the input language and then translating the input to {targetLanguage}."
        );

    private async Task<List<ChatMessage>> RunWorkflowAsync(
        Workflow workflow,
        List<ChatMessage> messages
    )
    {
        string? lastExecutorId = null;

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
            workflow,
            messages
        );
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentResponseUpdateEvent e)
            {
                if (e.ExecutorId != lastExecutorId)
                {
                    lastExecutorId = e.ExecutorId;
                    Console.WriteLine();
                    Console.WriteLine(e.ExecutorId);
                }

                Console.Write(e.Update.Text);
                if (
                    e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault()
                    is FunctionCallContent call
                )
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        $"  [Calling function '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]"
                    );
                }
            }
            else if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine();
                return output.As<List<ChatMessage>>()!;
            }
        }

        return [];
    }
}
