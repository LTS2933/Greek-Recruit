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
                Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo, // or Models.Gpt_4o
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(prompt)
                }
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
            sb.AppendLine("Write a 3–5 sentence summary based on their tone.");
            sb.AppendLine($"This PNM has attended {eventsAttended} event{(eventsAttended == 1 ? "" : "s")}.");
            sb.AppendLine("Here are the comments:");

            foreach (var c in comments)
                sb.AppendLine($"- {c}");

            return sb.ToString();
        }
    }
}
