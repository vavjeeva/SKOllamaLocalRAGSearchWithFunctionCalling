#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0110
#pragma warning disable KMEXP00

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using System.Reflection;
using SKLocalRAGSearchWithFunctionCalling.Plugins;
using SKLocalRAGSearchWithFunctionCalling;


var config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
    .Build();

var modelId = config["modelId"];
var embeddingModelId = config["embeddingModelId"];
var baseUrl = config["baseUrl"];
var qdrantMemoryStoreUrl = config["qdrantMemoryStoreUrl"];
var baseEmbeddingUrl = config["baseUrl"];
var weatherApiKey = config["weatherApiKey"];
var domainName = config["domainName"];
var domainGuide = config["domainGuide"];

var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(10)
};

var memory = new MemoryBuilder()
           .WithOllamaTextEmbeddingGeneration(embeddingModelId, new Uri(baseEmbeddingUrl!))
           .WithQdrantMemoryStore(httpClient, 1024, qdrantMemoryStoreUrl)
           .WithHttpClient(httpClient)
           .Build();

var builder = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId: modelId, apiKey: null, endpoint: new Uri(baseUrl), httpClient: httpClient);


var kernel = builder.Build();

var collections = await memory.GetCollectionsAsync();
if (!collections.Contains(domainName))
    await EmbedData();

string HostName = $"{domainName} Assistant";
string HostInstructions = $@"You are an Assistant to search content from the {domainName} guide to help users to answer the question. 

You can answer general questions like greetings, good bye with your response without using any plugins. 
For all other questions, use the list of available plugin below to get the answer. 

List of Available Plugins:
    Local Time Plugin : Retrieve the current date and time
    Weather Plugin : Calculate the weather for the given location.
    Memory Plugin: Search answers from memory for questions related to {domainName}.

If any one of the plugin can not be used for the give query , 
even if you know the answer, you should not provide the answer outside of the {domainName} context. respond back with ""I dont have the answer for your question"" 
Be precise with the response. Do not add what plugin you have used to get the answers in the response.
";

OpenAIPromptExecutionSettings settings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

ChatCompletionAgent agent =
           new()
           {
               Instructions = HostInstructions,
               Name = HostName,
               Kernel = kernel,
               Arguments = new(settings),
           };

var memoryPlugin = new TextMemoryPlugin(memory);
agent.Kernel.ImportPluginFromObject(memoryPlugin);

KernelPlugin localDateTimePlugin = KernelPluginFactory.CreateFromType<LocalDateTimePlugin>();
agent.Kernel.Plugins.Add(localDateTimePlugin);

KernelPlugin weatherPlugin = KernelPluginFactory.CreateFromObject(new WeatherPlugin(weatherApiKey!));
agent.Kernel.Plugins.Add(weatherPlugin);

Console.WriteLine($"Assistant: Hello, I am your {domainName} Assistant. How may i help you?");
ChatHistory chat = [];
while (true)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User: ");
    string question = Console.ReadLine()!;
    await InvokeAgentAsync(question);
}

// Local function to invoke agent and display the conversation messages.
async Task InvokeAgentAsync(string input)
{
    chat.AddUserMessage(input);

    var arguments = new KernelArguments(settings)
    {
        { "input", input },
        { "collection", domainName }
    };

    Console.ForegroundColor = ConsoleColor.Green;
    await foreach (ChatMessageContent content in agent.InvokeAsync(chat, arguments, kernel))
    {
        if (!content.Items.Any(i => i is FunctionCallContent || i is FunctionResultContent))
        {
            chat.Add(content);
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Assistant: '{content.Content}'");
    }
}


async Task EmbedData()
{
    Console.WriteLine("Embedding Started.");
    FileContent content = new(MimeTypes.PlainText);
    var pdfDecoder = new PdfDecoder();
    content = await pdfDecoder.DecodeAsync(domainGuide);

    int pageIndex = 1;
    foreach (FileSection section in content.Sections)
    {
        if (!string.IsNullOrEmpty(section.Content))
        {
            await memory.SaveInformationAsync(domainName, id: $"page{pageIndex}", text: section.Content);
            pageIndex++;
        }
    }
    Console.WriteLine("Embedding Ended.");
}

#pragma warning restore SKEXP0070
#pragma warning restore SKEXP0050
#pragma warning restore SKEXP0001
#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0020
#pragma warning restore SKEXP0110
#pragma warning restore KMEXP00