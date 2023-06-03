namespace BlazorServerAppTests.UnitTests
{
    public class ServiceDiscoveryServerTests
    {
        public class RegisterService
        {
            [Theory]
            [AutoDomainData]
            public async Task WhenAddingDtoThenItIsAdded(
                [Frozen] Mock<IServerManagerLogic> managerMock,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                ServiceDto dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { {"test1", "value1"} }
                };

                managerMock.Setup(n => n.AddOrUpdate(dto1)).Returns(() => new ServiceModel()
                {
                    Id = "id"
                });
                
                // Act
                var res = await sut.RegisterService(dto1, null);
                
                // Assert
                Assert.NotNull(res);
                Assert.True(res.Ok);
                managerMock.Verify(n=>n.AddOrUpdate(dto1), Times.Once);
            }

            [Theory]
            [AutoDomainData]
            public async Task WhenThereIsExceptionThenResultIsFalse(
                [Frozen] Mock<IServerManagerLogic> managerMock,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                ServiceDto dto1 = new ServiceDto
                {
                    HealthState = EnumServiceHealth.Healthy,
                    Scope = "env1",
                    ServiceHost = "localhost",
                    ServiceName = "name1",
                    ServicePort = 5000,
                    Metadata = { {"test1", "value1"} }
                };

                managerMock.Setup(n => n.AddOrUpdate(dto1)).Throws(new Exception("test"));
                
                // Act
                var res = await sut.RegisterService(dto1, null);
                
                // Assert
                Assert.NotNull(res);
                Assert.False(res.Ok);
                managerMock.Verify(n=>n.AddOrUpdate(dto1), Times.Once);
            }
        }

        public class FindService
        {
            [Theory]
            [AutoDomainData]
            public async Task WhenFindIsCalledThenForward(
                [Frozen] Mock<IServerManagerLogic> managerMock,
                FindServiceRequest dto1,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                
                // Act
                var res = await sut.FindService(dto1, null);
                
                // Assert
                Assert.NotNull(res);
                Assert.True(res.Ok);
                managerMock.Verify(n=>n.FindService(dto1.Name, dto1.Scope), Times.Once);
            }

            [Theory]
            [AutoDomainData]
            public async Task WhenThereIsExceptionThenResultIsFalse(
                [Frozen] Mock<IServerManagerLogic> managerMock,
                FindServiceRequest dto1,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                managerMock.Setup(n => n.FindService(dto1.Name, dto1.Scope)).Throws(new Exception("test"));
                
                // Act
                var res = await sut.FindService(dto1, null);
                
                // Assert
                Assert.NotNull(res);
                Assert.False(res.Ok);
                managerMock.Verify(n=>n.FindService(dto1.Name, dto1.Scope), Times.Once);
            }
        }

        public class Start
        {
            /// <summary>
            /// When server is Started with auto-discover enabled, then a multicast message is sent periodically
            /// with information about this server.
            /// </summary>
            [Theory]
            [AutoDomainData]
            public void WhenStartWithAutoDiscoverThenMessageSent(
                [Frozen] Mock<IServerManagerLogic> managerMock,
                Mock<ILogger<ServiceDiscoveryServer>> logger,
                Mock<IMulticastClient> client,
                ServiceConfigurationOptions options,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                sut.Dispose();
                options.EnableAutoDiscover = true;
                options.AutoDiscoverFreq = 1;
                options.Port = StaticHelpers.GetAvailablePort(5000);
                sut = new ServiceDiscoveryServer(logger.Object, managerMock.Object, options, client.Object);

                bool called = false;
                client.Setup(n => n.Send(It.IsAny<string>(), options.AutoDiscoverPort)).Callback(() => called = true);

                // Act
                sut.Start();
                SpinWait.SpinUntil(() => called, TimeSpan.FromSeconds(2));
                
                // Assert
                Assert.True(called);
            }
        }

        public class ServiceModelListChanged
        {
            [Theory]
            [AutoDomainData]
            public void WhenManagerFiresListChangeThenFired(
                [Frozen] Mock<IServerManagerLogic> managerMock,
                ServiceDiscoveryServer sut)
            {
                // Arrange
                bool called = false;
                sut.ServiceModelListChanged += (sender, args) => called = true;

                // Act
                managerMock.Raise(n=>n.ServiceModelListChanged += null, this, EventArgs.Empty);
                
                // Assert
                Assert.True(called);
            }
        }
    }
}
