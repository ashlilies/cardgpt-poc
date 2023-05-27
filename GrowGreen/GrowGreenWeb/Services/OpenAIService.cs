using Azure.AI.OpenAI;
using GrowGreenWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace GrowGreenWeb.Services;

public class OpenAIService
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAIService> _logger;
    private readonly IDbContextFactory<GrowGreenContext> _contextFactory;

    private readonly string _openAIModel;

    private readonly string _summaryPromptHeader;
    private readonly string _flashCardTextPromptHeader;
    private readonly string _flashCardXmlPromptHeader;

    public OpenAIService(
        ILogger<OpenAIService> logger,
        IDbContextFactory<GrowGreenContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;

        // Load OpenAI keys from environment variables
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string? model = Environment.GetEnvironmentVariable("OPENAI_MODEL_NAME");

        // Load Prompt Headers
        string? promptSummaryText =
            Environment.GetEnvironmentVariable("CARDGPT_SUMMARY_PROMPT_HEADER");
        string? promptCardText =
            Environment.GetEnvironmentVariable("CARDGPT_CARDTEXT_PROMPT_HEADER");
        string? promptCardXml =
            Environment.GetEnvironmentVariable("CARDGPT_CARDXML_PROMPT_HEADER");

        // Check if environment variables are set
        if (apiKey == null || model == null)
        {
            throw new Exception(
                "OpenAI environment variables OPENAI_API_KEY and " +
                "OPENAI_MODEL_NAME are required.");
        }

        if (promptSummaryText == null
            || promptCardText == null
            || promptCardXml == null)
        {
            throw new Exception(
                "CardGPT environment variables CARDGPT_SUMMARY_PROMPT_HEADER, " +
                "CARDGPT_CARDTEXT_PROMPT_HEADER, and " +
                "CARDGPT_CARDXML_PROMPT_HEADER are required.");
        }

        // Set readonly variables
        _openAIModel = model;
        _summaryPromptHeader = promptSummaryText;
        _flashCardTextPromptHeader = promptCardText;
        _flashCardXmlPromptHeader = promptCardXml;

        // Create the client with options
        var options = new OpenAIClientOptions
        {
            Retry =
            {
                MaxDelay = new TimeSpan(0, 1, 0),
                MaxRetries = 5
            }
        };

        _client = new OpenAIClient(apiKey, options);
    }

    public async Task CreateFlashCardTextFromVideo(
        Video videoDb,
        string inputText)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(videoDb.Transcript))
        {
            videoDb.FlashCardText =
                "Transcript unavailable, unable to generate flashcard...";
            db.Update(videoDb);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("{Error} {Message}", ex, ex.Message);
            }
        }
        else
        {
            Task<string> gptTask = CreateFlashCardTextAsync(inputText);

            // set the field to loading
            videoDb.FlashCardText =
                "Flashcard Deck is unavailable or still generating, " +
                "please check back later...";
            
            db.Update(videoDb);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("{Error} {Message}", ex, ex.Message);
            }
            
            // wait for it to save
            videoDb.FlashCardText = await gptTask;
            db.Update(videoDb);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("{Error} {Message}", ex, ex.Message);
            }
        }
    }

    private string CreateSummaryText(string inputText)
    {
        throw new NotImplementedException();
    }

    private async Task<string> CreateFlashCardTextAsync(string inputText)
    {
        var prompt = _flashCardTextPromptHeader + " " + inputText;

        var completionOptions = new CompletionsOptions
        {
            Prompts = { prompt },
            MaxTokens = 5000
        };

        Completions response = await _client.GetCompletionsAsync(
            _openAIModel,
            completionOptions);

        return response.Choices.First().Text;
    }

    private string CreateFlashCardXml(string inputText)
    {
        throw new NotImplementedException();
    }
}