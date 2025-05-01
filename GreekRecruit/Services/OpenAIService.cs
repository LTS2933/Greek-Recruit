//using OpenAI;
//using OpenAI.Chat;
//using OpenAI.Models;

//namespace GreekRecruit.Services
//{
//    public class OpenAIService
//    {
//        private readonly OpenAIClient _client;

//        public OpenAIService(string apiKey)
//        {
//            _client = new OpenAIClient(new OpenAIAuthentication(apiKey));
//        }

//        public async Task<string> GeneratePNMSummaryAsync(List<string> comments, int eventsAttended)
//        {
//            string prompt = BuildPrompt(comments, eventsAttended);

//            var chatRequest = new ChatRequest(
//                new[]
//                {
//                    new Message(Role.System, prompt)
//                },
//                model: Model.GPT4o
//            );

//            var response = await _client.ChatEndpoint.GetCompletionAsync(chatRequest);

//            return response.FirstChoice.Message.Content;
//        }

//        private string BuildPrompt(List<string> comments, int eventsAttended)
//        {
//            var sb = new System.Text.StringBuilder();
//            sb.AppendLine("You are summarizing chapter member feedback about a potential new member.");
//            sb.AppendLine("Write a 2–4 sentence summary based on their tone.");
//            sb.AppendLine($"This PNM has attended {eventsAttended} event{(eventsAttended == 1 ? "" : "s")}.");
//            sb.AppendLine("Here are the comments:");

//            foreach (var c in comments)
//                sb.AppendLine($"- {c}");

//            return sb.ToString();
//        }
//    }
//}
