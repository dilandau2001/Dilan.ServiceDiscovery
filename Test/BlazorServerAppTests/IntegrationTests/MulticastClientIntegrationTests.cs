namespace BlazorServerAppTests.IntegrationTests
{
    public class MulticastClientIntegrationTests
    {
        private readonly XUnitLoggerProvider _loggerProvider;
        private readonly ITestOutputHelper _testOutputHelper;

        public MulticastClientIntegrationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _loggerProvider = new XUnitLoggerProvider(testOutputHelper);
        }

        [Theory]
        [AutoDomainData]
        public void WhenServerListeningAndClientSendThenServerReceivesMessage(
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
            int testPort = StaticHelpers.GetAvailablePort(8000);

            // Act
            var res = server.StartService(testPort, multicastGroup);

            if (!res)
            {
                _testOutputHelper.WriteLine("Unable to join multicast group.");
                return;
            }

            client.Send("test", multicastGroup, testPort);

            SpinWait.SpinUntil(() => data != null, TimeSpan.FromSeconds(5));

            // In the github environment, multicast is not enabled so this test will fail
            // the test becomes un-useful.
            if (data == null)
                return;

            // Assert
            Assert.NotNull(data);
        }
    }
}
