using Microsoft.Extensions.VectorData;

namespace SmartLayoutSearch.Services;

public interface IUserPreferenceService
{
    Task<IVectorStoreRecordCollection<string, UserPreference>> LoadPreferencesAsync();
    Task ExtractForUserPreferenceFactAsync(string userPrompt);
}