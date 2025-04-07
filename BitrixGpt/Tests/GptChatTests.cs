

using BitrixGpt.Logs;
using BitrixGpt.Helpers;
using BitrixGpt.Bitrix24;
using BitrixGpt.Constants;
using BitrixGpt.ConnectOpenAI;

using Moq;
using Xunit;

using System.Text;


namespace BitrixGpt.Tests
{
    /// <summary>
    /// Тест 1: Перевіряє успішну роботу StartAsync, коли OpenAI повертає коректну відповідь. 
    //          Перевіряється виклик делегата з правильними параметрами.
    //  Тест 2: Перевіряє, що при помилці API логування відбувається коректно.
    //  MockHttpMessageHandler: Імітує відповіді від OpenAI, щоб уникнути реальних HTTP-запитів.
    /// </summary>
    public class GptChatTests
    {


        private readonly Mock<ILog> _mockLog;
        private readonly Settings_Prop _settings;


        public GptChatTests()
        {
            _mockLog = new Mock<ILog>();
            _settings = new Settings_Prop
            {
                OPENAI_KEY = "test-key",
                OPENAI_PATH = "http://mock-api",
                MODEL_GPT = "gpt-4o-mini",
                INSTRUCTS_FOR_GPT = "Test instructions",
                SEND_MANAGER = "Need manager",
                WEEKEND_TEXT = "Weekend message"
            };
            GptChat.Log = _mockLog.Object;
            GptChat.SETTINGS = _settings;
        }

        [Fact]
        public async Task StartAsync_SuccessfulResponse_ReturnsResponseViaDelegate()
        {
            // Arrange
            var httpMessageHandler = new MockHttpMessageHandler(
                "{\"choices\": [{\"message\": {\"content\": \"Test response\"}}]}",
                System.Net.HttpStatusCode.OK);
            var httpClient = new HttpClient(httpMessageHandler) { BaseAddress = new Uri(_settings.OPENAI_PATH) };
            var responseDelegate = new Mock<responseChatGpt>();
            string filePath = ConstantFolders.DATASET_PATH + "/Page.json";
            File.WriteAllText(filePath, "[{\"prompt\":\"test\",\"response\":\"test response\"}]");

            // Act
            await GptChat.StartAsync(123, "Test question", "Chat history", "Page", responseDelegate.Object);

            // Assert
            responseDelegate.Verify(d => d(123, "Test question", "Test response"), Times.Once());
            File.Delete(filePath); // Cleanup
        }

        [Fact]
        public async Task StartAsync_ApiError_LogsError()
        {
            // Arrange
            var httpMessageHandler = new MockHttpMessageHandler("", System.Net.HttpStatusCode.InternalServerError);
            var httpClient = new HttpClient(httpMessageHandler) { BaseAddress = new Uri(_settings.OPENAI_PATH) };
            var responseDelegate = new Mock<responseChatGpt>();
            string filePath = ConstantFolders.DATASET_PATH + "/Page.json";
            File.WriteAllText(filePath, "[]");

            // Act
            await GptChat.StartAsync(123, "Test question", null, "Page", responseDelegate.Object);

            // Assert
            _mockLog.Verify(l => l.LogDelegate(It.IsAny<Type>(), It.IsAny<string>(), Enums.LogLevels.Error), Times.AtLeastOnce());
            File.Delete(filePath);
        }
    }

    // Mock для HttpClient
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly System.Net.HttpStatusCode _statusCode;

        public MockHttpMessageHandler(string response, System.Net.HttpStatusCode statusCode)
        {
            _response = response;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_response, Encoding.UTF8, "application/json")
            });
        }
    }
}