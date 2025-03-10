using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace SmartLayoutSearch.Services;

#pragma warning disable SKEXP0001

public record UserPreference
{
    [VectorStoreRecordKey]
    [TextSearchResultName]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    [TextSearchResultValue]
    public string Description { get; init; } = string.Empty;

    [VectorStoreRecordVector(Dimensions: 1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}

#pragma warning restore SKEXP0001
