

// using BitrixGpt.Logs;
// using BitrixGpt.Bitrix24;
// using BitrixGpt.ConnectOpenAI;

// using Moq;
// using Xunit;


// namespace BitrixGpt.Tests
// {
//     /// <summary>
//     /// Тест 1: Перевіряє додавання нового запиту в чергу.
//     // Тест 2: Перевіряє об’єднання питань для одного userId.
//     // Тест 3: Перевіряє обробку черги в StartWhileAsync. Оскільки це приватний метод, 
//     //          викликаємо через рефлексію (у реальному проєкті краще зробити його тестопридатним через DI).
//     // Зауваження: Для тестування приватних полів використано рефлексію, 
//     //             але краще зробити чергу доступною через властивість для тестів.
//     /// </summary>
//     public class QueueToGptTests
//     {


//         private readonly Mock<ILog> _mockLog;
//         private readonly Mock<responseChatGpt> _mockResponseDelegate;


//         public QueueToGptTests()
//         {
//             _mockLog = new Mock<ILog>();
//             _mockResponseDelegate = new Mock<responseChatGpt>();
//             QueueToGpt.Log = _mockLog.Object;
//             QueueToGpt.IsBreak = false; // Скидаємо для тестів
//         }

//         [Fact]
//         public async Task SetToQueueAsync_NewRequest_AddsToQueue()
//         {
//             // Arrange
//             int userId = 123;
//             string question = "Test question";
//             string history = "Test history";
//             string project = "Page";

//             // Act
//             await QueueToGpt.SetToQueueAsync(userId, question, history, project, _mockResponseDelegate.Object);

//             // Assert (перевіряємо через рефлексію, оскільки _msgQueue приватна)
//             var queueField = typeof(QueueToGpt).GetField("_msgQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
//             var queue = queueField.GetValue(null) as System.Collections.Concurrent.ConcurrentQueue<(int, string, string, string)>;
//             Assert.Single(queue);
//             Assert.True(queue.TryPeek(out var result));
//             Assert.Equal(userId, result.Item1);
//             Assert.Equal(question, result.Item2);
//         }

//         [Fact]
//         public async Task SetToQueueAsync_ExistingUserId_CombinesQuestions()
//         {
//             // Arrange
//             int userId = 123;
//             await QueueToGpt.SetToQueueAsync(userId, "First question", "History", "Page", _mockResponseDelegate.Object);

//             // Act
//             await QueueToGpt.SetToQueueAsync(userId, "Second question", "History", "Page", _mockResponseDelegate.Object);

//             // Assert
//             var queueField = typeof(QueueToGpt).GetField("_msgQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
//             var queue = queueField.GetValue(null) as System.Collections.Concurrent.ConcurrentQueue<(int, string, string, string)>;
//             Assert.Single(queue);
//             Assert.True(queue.TryPeek(out var result));
//             Assert.Equal("First question Second question", result.Item2);
//         }

//         [Fact]
//         public async Task StartWhileAsync_ProcessesQueue()
//         {
//             // Arrange
//             var mockGptChat = new Mock<GptChat>();
//             mockGptChat.Setup(m => m.StartAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<responseChatGpt>())).Returns(Task.CompletedTask);
//             await QueueToGpt.SetToQueueAsync(123, "Test", "History", "Page", _mockResponseDelegate.Object);

//             // Act
//             await Task.Run(async () => await QueueToGpt.GetType().GetMethod("StartWhileAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null));
//             QueueToGpt.IsBreak = true; // Зупиняємо цикл

//             // Assert
//             mockGptChat.Verify(m => m.StartAsync(123, "Test", "History", "Page", _mockResponseDelegate.Object), Times.Once());
//         }
//     }
// }