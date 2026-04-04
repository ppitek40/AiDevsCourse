namespace AiDevs.Infrastructure.Models;

public enum OpenRouterModel
{
    Gpt4o,
    Gpt41,
    Gpt41Mini,
    Gpt41Nano,
    Gpt5Nano,
    Gpt54Nano,
    Gpt54Mini,
    Claude45Sonnet,
    Claude45Haiku,
    Gemini25Flash,
    Gemini25FlashLite,
    Gemini25Pro,
    Gemini3FlashPreview,
    Gemini31FlashLitePreview,
    Grok41Fast, 
    DeepSeekChat,
    DeepSeekV32,
    HunterAlpha
}

public static class OpenRouterModelExtensions
{
    public static string ToModelId(this OpenRouterModel model) => model switch
    {
        OpenRouterModel.Gpt4o => "openai/gpt-4o",
        OpenRouterModel.Gpt41 => "openai/gpt-4.1",
        OpenRouterModel.Gpt41Mini => "openai/gpt-4.1-mini",
        OpenRouterModel.Gpt41Nano => "openai/gpt-4.1-nano",
        OpenRouterModel.Gpt5Nano => "openai/gpt-5-nano",                       
        OpenRouterModel.Gpt54Nano => "openai/gpt-5.4-nano",
        OpenRouterModel.Gpt54Mini => "openai/gpt-5.4-mini",
        OpenRouterModel.Claude45Sonnet => "anthropic/claude-sonnet-4.5",
        OpenRouterModel.Claude45Haiku => "anthropic/claude-haiku-4.5",
        OpenRouterModel.Gemini25Flash => "google/gemini-2.5-flash",
        OpenRouterModel.Gemini25FlashLite => "google/gemini-2.5-flash-lite",
        OpenRouterModel.Gemini25Pro => "google/gemini-2.5-pro",
        OpenRouterModel.Gemini3FlashPreview => "google/gemini-3-flash-preview",
        OpenRouterModel.Gemini31FlashLitePreview => "google/gemini-3.1-flash-lite-preview",
        OpenRouterModel.Grok41Fast => "x-ai/grok-4.1-fast",
        OpenRouterModel.DeepSeekChat => "deepseek/deepseek-chat",
        OpenRouterModel.DeepSeekV32 => "deepseek/deepseek-v3.2",
        OpenRouterModel.HunterAlpha => "openrouter/hunter-alpha",
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
    };

   
}
