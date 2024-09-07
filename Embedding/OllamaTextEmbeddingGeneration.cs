#pragma warning disable SKEXP0001

using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;
using System.Text.Json.Nodes;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace SKLocalRAGSearchWithFunctionCalling.Embedding
{
    public class OllamaTextEmbeddingGeneration : ITextEmbeddingGenerationService
    {

        public IReadOnlyDictionary<string, object?> Attributes => _attributes;

        private readonly Dictionary<string, object?> _attributes = new();
        protected readonly HttpClient httpClient;
        protected readonly ILogger logger;

        public OllamaTextEmbeddingGeneration(string modelId, string baseUrl, HttpClient httpClient, ILoggerFactory? loggerFactory)
        {
            _attributes.Add("model_id", modelId);
            _attributes.Add("base_url", baseUrl);
            this.httpClient = httpClient;
            logger = loggerFactory is not null ? loggerFactory.CreateLogger<OllamaTextEmbeddingGeneration>() : NullLogger<OllamaTextEmbeddingGeneration>.Instance;
        }

        public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, Kernel? kernel = null,
            CancellationToken cancellationToken = new())
        {
            var result = new List<ReadOnlyMemory<float>>(data.Count);

            foreach (var text in data)
            {
                var request = new
                {
                    model = Attributes["model_id"],
                    prompt = text
                };

                var response = await httpClient.PostAsJsonAsync($"{Attributes["base_url"]}api/embeddings", request, cancellationToken).ConfigureAwait(false);

                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    logger.LogError("Unable to connect to ollama at {url} with model {model}", Attributes["base_url"], Attributes["model_id"]);
                }

                var json = JsonSerializer.Deserialize<JsonNode>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

                var embedding = new ReadOnlyMemory<float>(json!["embedding"]?.AsArray().GetValues<float>().ToArray());

                result.Add(embedding);
            }

            return result;
        }
    }
}
#pragma warning restore SKEXP0001
