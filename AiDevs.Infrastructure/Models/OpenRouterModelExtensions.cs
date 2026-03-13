namespace AiDevs.Infrastructure.Models;

public enum OpenRouterModel
{
    Gpt4o,
    Gpt41,
    Gpt41Mini,
    Claude35Sonnet,
    Claude37Sonnet,
    Gemini25Flash,
    Gemini25Pro,
    Gemini3FlashPreview,
    DeepSeekChat
}

public static class OpenRouterModelExtensions
{
    public static string ToModelId(this OpenRouterModel model) => model switch
    {
        OpenRouterModel.Gpt4o => "openai/gpt-4o",
        OpenRouterModel.Gpt41 => "openai/gpt-4.1",
        OpenRouterModel.Gpt41Mini => "openai/gpt-4.1-mini",
        OpenRouterModel.Claude35Sonnet => "anthropic/claude-3.5-sonnet",
        OpenRouterModel.Claude37Sonnet => "anthropic/claude-3.7-sonnet",
        OpenRouterModel.Gemini25Flash => "google/gemini-2.5-flash",
        OpenRouterModel.Gemini25Pro => "google/gemini-2.5-pro",
        OpenRouterModel.Gemini3FlashPreview => "google/gemini-3-flash-preview",
        OpenRouterModel.DeepSeekChat => "deepseek/deepseek-chat",
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
    };
}
