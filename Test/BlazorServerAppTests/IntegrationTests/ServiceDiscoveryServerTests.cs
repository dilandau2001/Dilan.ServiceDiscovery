namespace BlazorServerAppTests.IntegrationTests
{
    public class ServiceDiscoveryServerTests
    {
        public class RegisterService : IAsyncLifetime
        {
            private readonly ServiceDiscoveryServer _sut;
            private readonly ServiceDiscoveryClient _client;
            private readonly ServiceDiscoveryClient _client1;
            private readonly ServiceDiscoveryClient _client2;

            public RegisterService(ITestOutputHelper testOutputHelper)
            {
                var loggerProvider = new XUnitLoggerProvider(testOutputHelper);
                var options = new ServiceConfigurationOptions
                {
                    EnableAutoDiscover = false,
                    RefreshTimeInSeconds = 10
                };

                var multicastClient = new MulticastClient(loggerProvider.CreateLogger<MulticastClient>());

                _sut = new ServiceDiscoveryServer(
                    loggerProvider.CreateLogger<ServiceDiscoveryServer>(),
                    new ServerManagerLogic(options),
                    options,
                    multicastClient);

                _client = new ServiceDiscoveryClient(
                    loggerProvider.CreateLogger<ServiceDiscoveryClient>(),
                    new ClientConfigurationOptions {DiscoveryServerHost = "localhost"},
                    multicastClient,
                    new List<IMetadataProvider>{new SystemInfoMetadataProvider()});
                
                _client1 = new ServiceDiscoveryClient(
                    loggerProvider.CreateLogger<ServiceDiscoveryClient>(),
                    new ClientConfigurationOptions {DiscoveryServerHost = "localhost"},
                    multicastClient, 
                    new List<IMetadataProvider>{new SystemInfoMetadataProvider()});

                _client2 = new ServiceDiscoveryClient(
                    new ConsoleLogger<ServiceDiscoveryClient>(),
                    new ClientConfigurationOptions {DiscoveryServerHost = "localhost"},
                    multicastClient, 
                    new List<IMetadataProvider>{new SystemInfoMetadataProvider()});
            }

            [Fact]
            public async Task WhenRegisteringThenSuccess()
            {
                // Arrange
                await _client.Start();
                var dto = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    ServiceHost = "localhost",
                    ServicePort = 4567,
                    ServiceName = "MyName",
                    Metadata = { { "key1", "value1" } },
                    Scope = ""
                };

                // Act
                var res = await _client.RegisterService(dto);

                // Assert
                Assert.True(res.Ok);
            }

            [Fact]
            public async Task WhenRegisteringByUsingAutomaticConnectionThenSuccess()
            {
                // Arrange
                await _client.Start();

                // Act
                var res = await _client.FindService(_client.Options.ServiceName);

                // Assert
                Assert.True(_client.State == ServiceDiscoveryClient.States.Connected);
                Assert.True(res.Ok);
                Assert.Single(res.Services);
                Assert.Single(_sut.ServiceDictionary);
            }

            [Fact]
            public async Task WhenRegistering2ClientsByUsingAutomaticConnectionThenSuccess()
            {
                // Arrange

                // change port
                _client2.Options.CallbackPort = 7001;

                // Act
                await _client1.Start();
                await _client2.Start();

                SpinWait.SpinUntil(
                    () => _client1.State == ServiceDiscoveryClient.States.Connected &&
                          _client2.State == ServiceDiscoveryClient.States.Connected,
                    TimeSpan.FromSeconds(5));
                var res = await _client1.FindService(_client1.Options.ServiceName);

                // Assert
                Assert.True(_client1.State == ServiceDiscoveryClient.States.Connected);
                Assert.True(res.Ok);
                Assert.Equal(2, res.Services.Count);
                Assert.Equal(2, _sut.ServiceDictionary.Count);
            }

            [Fact]
            public async Task WhenRegistrationTimesOutThenServiceIsOffline()
            {
                // Arrange
                await _client1.Start();

                // Act
                await _client1.Stop();
                await Task.Delay(TimeSpan.FromSeconds(_sut.Options.TimeOutInSeconds + 2));
                
                var res = await _client1.FindService(_client1.Options.ServiceName);

                // Assert
                Assert.Single(_sut.ServiceDictionary);
                Assert.Equal(EnumServiceHealth.Offline, _sut.ServiceDictionary.Values.First().HealthState);

                Assert.True(res.Ok);
                Assert.Empty(res.Services);
            }

            #region Implementation of IAsyncLifetime

            /// <summary>
            /// Called immediately after the class has been created, before it is used.
            /// </summary>
            public Task InitializeAsync()
            {
                _sut.Start();
                return Task.CompletedTask;
            }

            /// <summary>
            /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
            /// if the class also implements that.
            /// </summary>
            public Task DisposeAsync()
            {
                _sut.Dispose();
                return Task.CompletedTask;
            }

            #endregion
        }
    }
}