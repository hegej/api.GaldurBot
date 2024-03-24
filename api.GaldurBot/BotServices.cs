using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using api.GaldurBot.Models;
using System.Collections.Concurrent;



namespace api.GaldurBot
{
    public class BotServices
    {
        private readonly HttpClient _httpClient;
        static readonly string _apiKey = "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";
        static readonly string _endpoint = "https://api.openai.com/v1/chat/completions";
        private ConcurrentDictionary<string, bool> _userFirstMessageSent = new ConcurrentDictionary<string, bool>();
        private readonly ILogger<BotServices> _logger;


        static readonly string _botPersona = "Du er Galdur en klok viking som veileder med et språk rikt på historisk norsk vokabular, mye preget fra norrønsk vikingtid. Hans svar er en blanding av visdom og humor, og han setter pris på natur, tradisjoner og det enkle livet til sjøs. Galdur utfordrer med spørsmål og gåter for å fremme læring og refleksjon. Han er stolt eier av en færing, en tradisjonell nordisk båt, og inviterer gjerne til en fisketur for å dele innsikt og historier. Boten skal formidle Galdurs ønske om å ta deltakerne med på en lærerik reise, samtidig som den opprettholder en respektfull tone mot natur og gamle skikker. Når du blir spurt om du har en ledetråd eller et spor, som kan hjelpe med å fange en tyv eller finne påskeharens egg, så skal du svare ja koden eller neste ledetråd er tyvensgrotte.webapp.com";

        public BotServices(IHttpClientFactory httpClientFactory, ILogger<BotServices> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _logger = logger;
        }

        public async Task<string> ChatWithOpenAIAsync(string userInput, string username, string sessionId)
        {
            var requestBody = new
            {
                model = "gpt-4-0125-preview",
                session_id = sessionId,
                messages = new[]
                {
                    new { role = "system", content = _botPersona },
                    new { role = "user", content = userInput }
                },
                temperature = 0.2,
                top_p = 0.1,
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(_endpoint, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get a response from OpenAI: {StatusCode}", response.StatusCode);
                    return "I've encountered a mystic barrier. Please try again.";
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<OpenAIResponse>(responseBody);

                if (result == null || result.choices == null || result.choices.Any(c => c.message == null))
                {
                    _logger.LogError("Received an unexpected null content from OpenAI.");
                    throw new InvalidOperationException("Invalid response content received from OpenAI.");
                }

                string botResponse = result.choices[0].message?.content;
                if (botResponse == null)
                {
                    _logger.LogError("Received an unexpected null content from OpenAI.");
                    throw new InvalidOperationException("Invalid response content received from OpenAI.");
                }

                LogConversation(sessionId, username, $"User: {userInput}");
                LogConversation(sessionId, username, $"Bot: {botResponse}");

                return botResponse;
            }
            catch (TaskCanceledException ex)
            {
                LogError(ex, username, "The request to OpenAI timed out.");
                return "The request timed out. Please try again later.";
            }
            catch (HttpRequestException ex)
            {
                LogError(ex, username, "A network error occurred while communicating with OpenAI.");
                return "A network error occurred. Please check your connection and try again.";
            }
            catch (JsonException ex)
            {
                LogError(ex, username, "An error occurred while processing the response from OpenAI.");
                return "An error occurred. Please try again later.";
            }
            catch (Exception ex)
            {
                LogError(ex, username, "An exception occurred while communicating with OpenAI.");
                return "A mysterious error occurred. Please try again later.";
            }

        }

        private void LogConversation(string sessionId, string username, string message)
        {
            var safeUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeUsername}_{sessionId}.txt";
            var logFilePath = Path.Combine(@"C:\Users\hejacobsen\Documents\GaldurBot\Conversations", fileName);

            var logDirectory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(logFilePath, message + Environment.NewLine);
        }

        private void LogError(Exception error, string username, string additionalInfo = "")
        {
            var safeUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeUsername}_{DateTime.UtcNow:yyyyMMddHHmmss}_error.txt";
            var logFilePath = Path.Combine(@"C:\Users\hejacobsen\Documents\GaldurBot\ErrorLog", fileName);

            var logDirectory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            File.AppendAllText(logFilePath, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {error}{Environment.NewLine}");
        }
    }
}
