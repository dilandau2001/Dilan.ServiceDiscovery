namespace BlazorServerAppTests.IntegrationTests
{
    public class ServiceDiscoveryServerTests
    {
        public class RegisterService
        {
            private readonly XUnitLoggerProvider _loggerProvider;

            public RegisterService(ITestOutputHelper testOutputHelper)
            {
                _loggerProvider = new XUnitLoggerProvider(testOutputHelper);
            }

            [Theory]
            [AutoDomainData]
            public async Task WhenRegisteringThenSuccess(
                ServiceDiscoveryClient client,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                client.Logger = _loggerProvider.CreateLogger<ServiceDiscoveryClient>();
                sut.Logger = _loggerProvider.CreateLogger<ServiceDiscoveryServer>();
                sut.Start();

                var dto = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    ServiceHost = "localhost",
                    ServicePort = 4567,
                    ServiceName = "MyName",
                    Metadata = { { "key1", "value1" } }
                };

                // Act
                var res = await client.RegisterService(dto);

                // Assert
                Assert.True(res.Ok);
            }

            [Theory]
            [AutoDomainData]
            public async Task WhenRegisteringByUsingAutomaticConnectionThenSuccess(
                ServiceDiscoveryClient client,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                client.Logger = _loggerProvider.CreateLogger<ServiceDiscoveryClient>();
                sut.Logger = _loggerProvider.CreateLogger<ServiceDiscoveryServer>();
                sut.Start();

                // Act
                await client.Start();
                var res = await client.FindService(client.Options.ServiceName);

                // Assert
                Assert.True(client.State == ServiceDiscoveryClient.States.Connected);
                Assert.True(res.Ok);
                Assert.Single(res.Services);
                Assert.Single(sut.ServiceDictionary);
            }

            [Theory]
            [AutoDomainData]
            public async Task WhenRegistering2ClientsByUsingAutomaticConnectionThenSuccess(
                ServiceDiscoveryClient client1,
                ServiceDiscoveryClient client2,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                client1.Logger = _loggerProvider.CreateLogger<ServiceDiscoveryClient>();
                sut.Logger = _loggerProvider.CreateLogger<ServiceDiscoveryServer>();
                sut.Start();

                // change port
                client2.Options.CallbackPort = 7001;

                // Act
                await client1.Start();
                await client2.Start();
                var res = await client1.FindService(client1.Options.ServiceName);

                // Assert
                Assert.True(client1.State == ServiceDiscoveryClient.States.Connected);
                Assert.True(res.Ok);
                Assert.Equal(2, res.Services.Count);
                Assert.Equal(2, sut.ServiceDictionary.Count);
            }

            [Theory]
            [AutoDomainData]
            public async Task WhenRegistrationTimesOutThenServiceIsOffline(
                ServiceDiscoveryClient client1,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                client1.Logger = _loggerProvider.CreateLogger<ServiceDiscoveryClient>();
                sut.Logger = _loggerProvider.CreateLogger<ServiceDiscoveryServer>();
                sut.Start();
                await client1.Start();

                // Act
                await client1.Stop();
                await Task.Delay(TimeSpan.FromSeconds(sut.Options.TimeOutInSeconds + 1));
                
                var res = await client1.FindService(client1.Options.ServiceName);

                // Assert
                Assert.Single(sut.ServiceDictionary);
                Assert.Equal(EnumServiceHealth.Offline, sut.ServiceDictionary.Values.First().HealthState);

                Assert.True(res.Ok);
                Assert.Empty(res.Services);
            }
        }
    }

    public class MyTestClass
    {   
        public MyTestClass(ILogger<MyTestClass> logger)
        {
            Logger = logger;
            Logger.BeginScope(nameof(MyTestClass));
        }

        public ILogger<MyTestClass> Logger { get; set; }

        public bool Example()
        {
            using (Logger.BeginScope(nameof(Example)))
            {
                Logger.LogDebug("My test");
                return true;
            }
        }
    }

    public class MyUnitTests
    {
        private readonly XUnitLoggerProvider _loggerProvider;
        
        public MyUnitTests(ITestOutputHelper testOutputHelper)
        {
            _loggerProvider = new XUnitLoggerProvider(testOutputHelper);
        }

        [Theory]
        [AutoDomainData]
        public void WhenThen(MyTestClass sut)
        {
            // Arrange
            sut.Logger = _loggerProvider.CreateLogger<MyTestClass>();
            
            // Act
            var res = sut.Example();

            // Assert
            Assert.True(res);
        }
    }
}