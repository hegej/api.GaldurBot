using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using api.GaldurBot.Models;



namespace api.GaldurBot
{
    public class BotServices
    {
        private readonly HttpClient _httpClient;
        static readonly string _apiKey = "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";
        static readonly string _endpoint = "https://api.openai.com/v1/chat/completions";
        private readonly ILogger<BotServices> _logger;

        static readonly string _botPersona = "Du er Galdur en klok viking som veileder med et språk rikt på historisk norsk vokabular, mye preget fra norrønsk vikingtid. Hans svar er en blanding av visdom og humor, og han setter pris på natur, tradisjoner og det enkle livet til sjøs. Galdur utfordrer med spørsmål og gåter for å fremme læring og refleksjon. Han er stolt eier av en færing, en tradisjonell nordisk båt, og inviterer gjerne til en fisketur for å dele innsikt og historier. Boten skal formidle Galdurs ønske om å ta deltakerne med på en lærerik reise, samtidig som den opprettholder en respektfull tone mot natur og gamle skikker. Når du blir spurt om du har en ledetråd eller et spor, som kan hjelpe med å fange en tyv eller finne påskeharens egg, så skal du svare ja koden er tyvensgrotte.webapp.com";

        public BotServices(IHttpClientFactory httpClientFactory, ILogger<BotServices> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _logger = logger;
        }

        public BotServices(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> ChatWithOpenAIAsync(string userInput)
        {
            var requestBody = new
            {
                model = "gpt-4-0125-preview",
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

                if (result == null || result.Choices == null || result.Choices.Any(c => c.Content == null))
                {
                    _logger.LogError("Received an unexpected null content from OpenAI.");
                    throw new InvalidOperationException("Invalid response content received from OpenAI.");
                }

               
                var botResponse = result.Choices.First().Content; 

                _logger.LogInformation("Galdur received: {UserInput}", userInput);
                _logger.LogInformation("Galdur responded: {BotResponse}", botResponse);

                return botResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while communicating with OpenAI.");
                return "A mysterious error occurred. Please try again later.";
            }
        }
    }
}
