using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.ClientModel;
using WebApiAgentFramework.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Chat Clients
IChatClient chatClient = new AzureOpenAIClient(
        new Uri(builder.Configuration["AZURE_OPENAI_ENDPOINT"]!),
        new ApiKeyCredential(builder.Configuration["AZURE_OPENAI_API_KEY"]!))
    .GetChatClient("gpt-4.1")
    .AsIChatClient();

builder.Services.AddChatClient(chatClient);

// Agents
builder.AddAIAgent(name: "assistant", instructions: "You are a helpful assistant.");

builder.AddAIAgent("poet", "You are a creative poet. Respond to all requests with beautiful poetry.");

builder.AddAIAgent("coder", "You are an expert programmer. Help users with coding questions and provide code examples.")
    .WithAITool(AIFunctionFactory.Create(Tools.Add, name: "add"));

builder.AddAIAgent("assistant-tools", "You are a helpful assistant. Answer questions concisely and accurately.")
    .WithAITools(
        AIFunctionFactory.Create(Tools.GetWeather, name: "get_weather"),
        AIFunctionFactory.Create(Tools.GetCurrentTime, name: "get_current_time")
    );

// Workflows
var assistantBuilder = builder.AddAIAgent("workflow-assistant", "You are a helpful assistant in a workflow.");
var reviewerBuilder = builder.AddAIAgent("workflow-reviewer", "You are a reviewer. Review and critique the previous response.");

builder.AddWorkflow("review-workflow", (sp, key) =>
{
    var agents = new List<IHostedAgentBuilder>()
    {
        assistantBuilder, reviewerBuilder
    }
    .Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agents);
}).AddAsAIAgent();

// DevUI
builder.AddOpenAIResponses();
builder.AddOpenAIConversations();

var app = builder.Build();

// DevUI
app.MapOpenAIResponses();
app.MapOpenAIConversations();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevUI();
}

app.UseHttpsRedirection();

app.Run();


