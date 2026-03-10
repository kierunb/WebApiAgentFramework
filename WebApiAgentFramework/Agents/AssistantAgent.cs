using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using WebApiAgentFramework.Services;

namespace WebApiAgentFramework.Agents;

public class AssistantAgent(IChatClient chatClient)
{
    public AIAgent Create() =>
        chatClient.AsAIAgent(name: "assistant-agent", instructions: "You are helpful assistant");

    public AIAgent CreateAdvanced()
    {
        // Invoke the agent with a multi-turn conversation, where the context is preserved in the session object.
        // AgentSession session = await agent.CreateSessionAsync();

        return chatClient.AsAIAgent(
            new ChatClientAgentOptions()
            {
                ChatOptions = new()
                {
                    Instructions =
                        "You are a friendly assistant. Always address the user by their name.",
                },
                AIContextProviders = [new UserInfoMemory(chatClient)],
            }
        );
    }
}
