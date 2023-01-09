namespace BlazorServerAppTests.IntegrationTests
{
    public class MulticastClientIntegrationTests
    {
        private readonly XUnitLoggerProvider _loggerProvider;

        public MulticastClientIntegrationTests(ITestOutputHelper testOutputHelper)
        {
            _loggerProvider = new XUnitLoggerProvider(testOutputHelper);
        }

        [Theory]
        [AutoDomainData]
        public void WhenRegisteringThenSuccess(
            MulticastClient server,
            MulticastClient client)
        {
            // Arrange
            server.Logger = _loggerProvider.CreateLogger<MulticastClient>();
            client.Logger = _loggerProvider.CreateLogger<MulticastClient>();
            MulticastData? data = null;
            server.DataReceived += (sender, e) =>
            {
                data = e;
            };

            string multicastGroup = "224.0.0.1";
            int testPort = 6478;

            // Act
            server.StartService(testPort, multicastGroup);
            client.Send("test", multicastGroup, testPort);

            SpinWait.SpinUntil(() => data != null, TimeSpan.FromSeconds(5));

            // Assert
            Assert.NotNull(data);
        }
    }
}
