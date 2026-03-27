namespace QAsist.Application.Interfaces.IServices
{
    public interface IAiService
    {
        Task<string> GenerateCompletionAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default);

        Task<T> GenerateJsonResponseAsync<T>(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default) where T : class;
    }
}
