namespace BlazorServerAppTests
{
    public sealed class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly LoggerExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_testOutputHelper, _scopeProvider, categoryName);
        }

        public ILogger<T> CreateLogger<T>()
        {
            return new XUnitLogger<T>(_testOutputHelper, _scopeProvider, typeof(T).FullName ?? string.Empty);
        }

        public void Dispose()
        {
        }
    }
}
