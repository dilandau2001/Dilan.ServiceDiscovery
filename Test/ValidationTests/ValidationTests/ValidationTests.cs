namespace BlazorServerAppTests.ValidationTests
{
    public class ValidationTests
    {
        private readonly XUnitLoggerProvider _loggerProvider;

        public ValidationTests(ITestOutputHelper testOutputHelper)
        {
            _loggerProvider = new XUnitLoggerProvider(testOutputHelper);
        }

        [Theory]
        [AutoDomainData]
        public async Task WhenServerIsRunningThenIAmAbleToResolveAvailableServices(
            ServiceDiscoveryClient client,
            ServiceDiscoveryClient client2)
        {
            // Arrange
            client.Logger = _loggerProvider.CreateLogger<ServiceDiscoveryClient>();
            await client.Start();

            // Act
            var res = await client2.FindService(client.Options.ServiceName);

            // Assert
            Assert.True(res.Ok);
        }
    }
}
