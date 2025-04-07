

// using BitrixGpt.Logs;
// using BitrixGpt.Enums;

// using Moq;
// using Xunit;


// namespace BitrixGpt.Tests
// {
//     public class LogTests
//     {


//         private readonly Log _log;


//         public LogTests()
//         {
//             _log = new Log();
//             // Очищаємо папки перед тестами
//             if (Directory.Exists(Constants.ConstantFolders.LOGS_FOLDER)) Directory.Delete(Constants.ConstantFolders.LOGS_FOLDER, true);
//             if (Directory.Exists(Constants.ConstantFolders.MSG_FOLDER)) Directory.Delete(Constants.ConstantFolders.MSG_FOLDER, true);
//             Directory.CreateDirectory(Constants.ConstantFolders.LOGS_FOLDER);
//             Directory.CreateDirectory(Constants.ConstantFolders.MSG_FOLDER);
//         }

//         [Fact]
//         public async Task LogDelegate_WritesToFileAndScreen()
//         {
//             // Arrange
//             var mockPrint = new Mock<PrintToScreen>();
//             PrintToScreen.AddLine = mockPrint.Object.AddLine;

//             // Act
//             await _log.LogDelegate(typeof(LogTests), "Test message", Enums.LogLevels.Info);
//             await Task.Delay(100); // Даємо час черзі обробити

//             // Assert
//             string logFile = Path.Combine(Constants.ConstantFolders.LOGS_FOLDER, DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
//             Assert.True(File.Exists(logFile));
//             string content = File.ReadAllText(logFile);
//             Assert.Contains("Test message", content);
//             mockPrint.Verify(p => p(Enums.LogLevels.Info, It.Is<string>(s => s.Contains("Test message"))), Times.Once());
//         }

//         [Fact]
//         public async Task LogMsgDelegate_WritesMessageToFile()
//         {
//             // Arrange
//             string userId = "123";
//             string msg = "Test chat message";

//             // Act
//             await _log.LogMsgDelegate(userId, msg);
//             await Task.Delay(100); // Даємо час черзі обробити

//             // Assert
//             string msgFile = Path.Combine(Constants.ConstantFolders.MSG_FOLDER, $"{userId}.txt");
//             Assert.True(File.Exists(msgFile));
//             string content = File.ReadAllText(msgFile);
//             Assert.Equal(msg + Environment.NewLine, content);
//         }

//         [Fact]
//         public async Task LogDelegate_FileError_LogsToScreen()
//         {
//             // Arrange
//             var mockPrint = new Mock<PrintToScreen>();
//             PrintToScreen.AddLine = mockPrint.Object.AddLine;
//             Directory.Delete(Constants.ConstantFolders.LOGS_FOLDER, true); // Видаляємо папку, щоб викликати помилку

//             // Act
//             await _log.LogDelegate(typeof(LogTests), "Test error", Enums.LogLevels.Error);
//             await Task.Delay(100);

//             // Assert
//             mockPrint.Verify(p => p(Enums.LogLevels.Error, It.Is<string>(s => s.Contains("Error"))), Times.AtLeastOnce());
//         }
//     }

//     // Mock для PrintToScreen
//     public class MockPrintToScreen
//     {
//         public virtual void AddLine(LogLevels level, string message) { }
//     }
// }