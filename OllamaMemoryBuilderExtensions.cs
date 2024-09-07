#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
using Microsoft.SemanticKernel.Memory;
using SKLocalRAGSearchWithFunctionCalling.Embedding;

namespace SKLocalRAGSearchWithFunctionCalling
{
    public static class OllamaMemoryBuilderExtensions
    {
        public static MemoryBuilder WithOllamaTextEmbeddingGeneration(this MemoryBuilder builder,string modelId,Uri baseUrl)
        {
            builder.WithTextEmbeddingGeneration((logger, httpclient) => new OllamaTextEmbeddingGeneration(modelId,baseUrl.AbsoluteUri, httpclient, logger));
            return builder;
        }
    }
}
#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0001
#pragma warning restore SKEXP0050