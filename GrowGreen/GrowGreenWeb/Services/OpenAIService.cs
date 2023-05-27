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
    private readonly int _maxTokensPerMsg;

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
        string? maxTokens = 
            Environment.GetEnvironmentVariable("OPENAI_MAX_TOKENS_PER_CHAT");

        // Load Prompt Headers
        string? promptSummaryText =
            Environment.GetEnvironmentVariable("CARDGPT_SUMMARY_PROMPT_HEADER");
        string? promptCardText =
            Environment.GetEnvironmentVariable("CARDGPT_CARDTEXT_PROMPT_HEADER");
        string? promptCardXml =
            Environment.GetEnvironmentVariable("CARDGPT_CARDXML_PROMPT_HEADER");

        // Check if environment variables are set
        if (apiKey == null || model == null || maxTokens == null)
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
        _maxTokensPerMsg = int.Parse(maxTokens);
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
            _logger.LogInformation("Starting gptTask!");
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
            _logger.LogInformation("Received gptTask!");
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
        // Only for DaVinci or non-chat models
        // var prompt = _flashCardTextPromptHeader + " " + inputText;
        // _logger.LogInformation("Prompt: {Prompt}", prompt);

        // var completionOptions = new CompletionsOptions
        // {
        //     Prompts = { prompt },
        //     MaxTokens = 5000
        // };

        // try
        // {
        //     Completions response = await _client.GetCompletionsAsync(
        //         _openAIModel,
        //         completionOptions);
        //     _logger.LogInformation("Result: {Response}", response.Choices.First().Text);
        //     _logger.LogInformation("Result (Last): {Response}", response.Choices.Last().Text);
        //
        //     return response.Choices.First().Text;
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogError("OpenAI: {ExMessage}", ex.Message);
        //     return "Error generating flashcards: " + ex.Message;
        // }
        
        var completionOptions = new ChatCompletionsOptions
        {
            MaxTokens = _maxTokensPerMsg
        };
        completionOptions.Messages.Add(
            new ChatMessage(ChatRole.System, _flashCardTextPromptHeader));
        
        completionOptions.Messages.Add(
            new ChatMessage(ChatRole.User, inputText));

        completionOptions.Messages.ToList().ForEach(m =>
        {
            _logger.LogInformation("ChatGPT Message: {Content}", m.Content);
        });

        try
        {
            ChatCompletions response = await _client.GetChatCompletionsAsync(
                _openAIModel,
                completionOptions);
            _logger.LogInformation(
                "Result: {Response}", response.Choices.First().Message.Content);
            _logger.LogInformation(
                "Result (Last): {Response}", response.Choices.Last().Message.Content);
        
            return response.Choices.First().Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError("OpenAI: {ExMessage}", ex.Message);
            return "Error generating flashcards: " + ex.Message;
        }
    }

    private string CreateFlashCardXml(string inputText)
    {
        throw new NotImplementedException();
    }
}