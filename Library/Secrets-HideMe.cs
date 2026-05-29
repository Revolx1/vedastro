namespace VedAstro.Library
{
    /// <summary>
    /// Self-host secrets stub (Estrella deployment).
    /// VedAstro is designed so the library always builds without real keys —
    /// Secrets.Get(key) reflects over these private static fields. Empty values
    /// mean cloud/storage/geo/LLM features degrade gracefully (return ""),
    /// while the deterministic astronomical calculations (SwissEphNet) work fully.
    /// Fill any value here (or wire via env in code) to enable the matching feature.
    /// </summary>
    public static partial class Secrets
    {
        // STORAGE (Azure Blob / Table) — only needed for image hosting, cache, analytics
        private static string API_STORAGE = "";
        private static string WEB_STORAGE = "";
        private static string CentralStorageConnectionString = "";
        private static string CentralStorageAccountName = "";
        private static string CentralStorageKey = "";
        private static string VedAstroApiStorageKey = "";

        // GEO LOCATION & TIME & IP
        private static string GoogleAPIKey = "";
        private static string AzureMapsAPIKey = "";
        private static string IpDataAPIKey = "";

        // MISC
        private static string Password = "";
        private static string SLACK_EMAIL_WEBHOOK = "";
        private static string BING_IMAGE_SEARCH = "";

        // LLM (Azure OpenAI / Cohere / Mistral / Llama) — only for AI-text features
        private static string AzureOpenAIAPIKey = "";
        private static string azureCohereCommandRPlusAPIKey = "";
        private static string azureCohereEmbedAPIKey = "";
        private static string azureMetaLlama3APIKey = "";
        private static string azureMistralLargeAPIKey = "";
        private static string azureMistralSmallAPIKey = "";
    }
}
