using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using OpenAI;

namespace GreekRecruit.Services
{
    public class OpenAIServiceWrapper
    {
        private readonly OpenAIService _openAIClient;

        public OpenAIServiceWrapper(string apiKey)
        {
            _openAIClient = new OpenAIService(new OpenAiOptions
            {
                ApiKey = apiKey
            });
        }

        public async Task<string> GeneratePNMSummaryAsync(List<string> comments, int eventsAttended)
        {
            string prompt = BuildPrompt(comments, eventsAttended);

            var chatRequest = new ChatCompletionCreateRequest
            {
                Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo,
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(prompt)
                },
                MaxTokens = 280 // ~200–250 words
            };


            var response = await _openAIClient.ChatCompletion.CreateCompletion(chatRequest);

            if (response.Successful)
            {
                return response.Choices[0].Message.Content.Trim();
            }
            else
            {
                throw new Exception($"OpenAI API Error: {response.Error?.Message ?? "Unknown error"}");
            }
        }

        private string BuildPrompt(List<string> comments, int eventsAttended)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are summarizing chapter member feedback about a potential new member.");
            sb.AppendLine("Write a concise 2–4 sentence summary capturing the tone of the comments.");
            sb.AppendLine($"This PNM has attended {eventsAttended} event{(eventsAttended == 1 ? "" : "s")}.");
            sb.AppendLine("Here are the comments:");

            foreach (var c in comments)
                sb.AppendLine($"- {TrimComment(c, 30)}");

            return sb.ToString();
        }


        private string TrimComment(string comment, int maxWords)
        {
            var words = comment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= maxWords)
                return comment;

            var trimmed = string.Join(' ', words.Take(maxWords)) + "…";
            return trimmed;
        }

    }
}
