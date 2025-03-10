using System;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace SmartLayoverRAG;

#pragma warning disable SKEXP0001

public record FlightData
{
    [VectorStoreRecordKey]
    [TextSearchResultName]
    public string Id { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string Route { get; init; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string[] Airlines { get; init; } = [];

    [VectorStoreRecordData(IsFilterable = true)]
    public string Duration { get; init; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string BestAirline { get; init; } = string.Empty;

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    [TextSearchResultValue]
    public string Description { get; set; } = string.Empty;

    [VectorStoreRecordVector(Dimensions: 1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}

#pragma warning restore SKEXP0001
