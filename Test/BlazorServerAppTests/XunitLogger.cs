using System.Text;

namespace BlazorServerAppTests
{
    internal sealed class XUnitLogger<T> : XUnitLogger, ILogger<T>
    {
        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string category)
            : base(testOutputHelper, scopeProvider, category)
        {
        }
    }

    internal class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;
        private readonly LoggerExternalScopeProvider _scopeProvider;

        public static ILogger CreateLogger(ITestOutputHelper testOutputHelper) => new XUnitLogger(testOutputHelper, new LoggerExternalScopeProvider(), "");
        public static ILogger<T> CreateLogger<T>(ITestOutputHelper testOutputHelper) => new XUnitLogger<T>(testOutputHelper, new LoggerExternalScopeProvider(), "");

        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _scopeProvider = scopeProvider;
            _categoryName = categoryName;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable BeginScope<TState>(TState state)
        {
            try
            {
                _testOutputHelper.WriteLine("Beginning " + state);
            }
            catch
            {
                //
            }
            
            return _scopeProvider.Push(state);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var sb = new StringBuilder();
            sb.Append(GetLogLevelString(logLevel))
              .Append(" [").Append(_categoryName).Append("] ");

            // Append scopes
            _scopeProvider.ForEachScope((scope, state) =>
            {
                state.Append(" => ");
                state.Append(scope);
            }, sb);

            if (exception != null)
            {
                sb.Append(exception);
                
            }

            sb.Append(state);

            if (exception != null) sb.Append(formatter(state, exception));

            try
            {
                _testOutputHelper.WriteLine(sb.ToString());
            }
            catch
            {
                // Ignore all trace errors.
            }
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "[VRB]",
                LogLevel.Debug => "[DBG]",
                LogLevel.Information => "[INF]",
                LogLevel.Warning => "[WAR]",
                LogLevel.Error => "[ERR]",
                LogLevel.Critical => "[CRT]",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }
    }
}
