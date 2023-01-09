using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Class for Multicast communication
    /// </summary>
    public sealed class MulticastClient : IDisposable, IMulticastClient
    {
        #region FIELDS

        private Thread _receiverThread;
        private readonly List<UdpClient> _clients = new List<UdpClient>();
        private readonly UdpClient _client;

        #endregion

        #region PUBLIC: CONSTRUCTOR

        /// <summary>
        /// Initializes a new instance of the MulticastClient class
        /// </summary>
        public MulticastClient(
            ILogger<MulticastClient> logger)
        {
            Logger = logger;
            Enabled = false;
            _client = new UdpClient();
        }
        
        #endregion

        #region PUBLIC: EVENTS

        /// <summary>
        /// Event OnReceived that fires when UDP command is captured
        /// </summary>
        public event EventHandler<MulticastData> DataReceived;

        #endregion


        #region PUBLIC: PROPERTIES

        /// <summary>
        /// Gets a value indicating the receiving RxPort.
        /// </summary>
        public int ReceivePort { get; private set; }
        
        /// <summary>
        /// Gets a value indicating whether UDP socket is started
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// Gets or sets the multicast group.
        /// </summary>
        public string MulticastGroup { get; private set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger<MulticastClient> Logger { get; set; }
        
        #endregion

        #region PUBLIC: FUNCTIONS

        /// <summary>
        /// Start service. If Rx type selected this function will start reception thread
        /// </summary>
        public void StartService(int port, string multicastGroup, int ttl = 1)
        {
            using (Logger.BeginScope(nameof(StartService)))
            {
                if (Enabled)
                {
                    Logger.LogDebug("Already enabled");
                    return;
                }

                ReceivePort = port;
                MulticastGroup = multicastGroup;
                JoinMulticastGroup(multicastGroup, port, ttl);
                
                _receiverThread = new Thread(ReceiveThread);
                _receiverThread.Start();
                
                Enabled = true;
                Logger.LogInformation($"Multicast service listening on port {port}");
            }
        }
        
        /// <summary>
        /// Stop Service. If Type= RX Reception thread will close
        /// </summary>
        public void StopService()
        {
            using (Logger.BeginScope(nameof(StopService)))
            {
                if (!Enabled)
                {
                    Logger.LogDebug("Not Started");
                    return;
                }

                if (_receiverThread != null)
                {
                    _receiverThread.Join(1000);

                    if (_receiverThread.IsAlive)
                    {
                        _receiverThread.Join(2000);
                    }
                }

                if (!string.IsNullOrEmpty(MulticastGroup))
                {
                    DropMulticastGroup(IPAddress.Parse(MulticastGroup));
                }

                MulticastGroup = string.Empty;

                // disable service
                Enabled = false;
            }
        }

        private void DropMulticastGroup(IPAddress parse)
        {
            foreach (var udpClient in _clients)
            {
                try
                {
                    udpClient.DropMulticastGroup(parse);
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e.Message);
                }
            }
        }

        /// <summary>
        /// Join this multicast group on all available network interfaces.
        /// </summary>
        /// <param name="multicastGroup"></param>
        /// <param name="rxPort"></param>
        /// <param name="timeToLive"></param>
        public void JoinMulticastGroup(string multicastGroup, int rxPort, int timeToLive = 1)
        {
            MulticastGroup = multicastGroup;
            var list = StaticHelpers.GetAllLocalIpAddress();
            
            foreach (var s in list)
            {
                JoinMulticastGroup(multicastGroup, rxPort, timeToLive, s);
            }
        }

        /// <summary>
        /// Join to a listening multicast group
        /// </summary>
        /// <param name="multicastGroup"></param>
        /// <param name="rxPort">multicast receiving port</param>
        /// <param name="timeToLive">Time to live. Number of network jumps a multicast packet can do </param>
        /// <param name="localIp"></param>
        private void JoinMulticastGroup(string multicastGroup, int rxPort, int timeToLive, string localIp)
        {
            try
            {   
                IPEndPoint localPt = new IPEndPoint(IPAddress.Any, rxPort);
                IPAddress multicastAddress = IPAddress.Parse(multicastGroup);

                if (!string.IsNullOrEmpty(localIp))
                {
                    var localAddress = IPAddress.Parse(localIp);
                    localPt = new IPEndPoint(localAddress, rxPort);
                }
                
                UdpClient client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.EnableBroadcast = true;
                client.ExclusiveAddressUse = false;
                client.Client.Bind(localPt);
                client.JoinMulticastGroup(multicastAddress, timeToLive);
                _clients.Add(client);
            }
            catch (Exception exc)
            {
                Logger.LogError(exc, exc.Message);
            }
        }
        
        /// <summary>
        /// Send Data to destination Ip and Port
        /// </summary>
        /// <param name="data">Byte Array</param>
        /// <param name="address">Destination Ip</param>
        /// <param name="destinationPort">Destination Port</param>
        public void Send(string data, string address, int destinationPort)
        {
            try
            {
                // Construct destination IPEndPoint
                Logger.LogDebug($"Send message {data} to {address}:{destinationPort}" );
                IPAddress netIp = IPAddress.Parse(address);
                IPEndPoint destination = new IPEndPoint(netIp, destinationPort);
                
                byte[] bytes = Encoding.ASCII.GetBytes(data);

                _client.Send(bytes, bytes.Length, destination);
            }
            catch (Exception exc)
            {
                Logger.LogError(exc, exc.Message);
            }
        }

        /// <summary>
        /// Send Data to the configured multicast group. In order to use this you had to previously call JoinMulticastGroup.
        /// </summary>
        /// <param name="data">Byte Array</param>
        /// <param name="destinationPort">Destination Port</param>
        public void Send(string data, int destinationPort)
        {
            try
            {
                if (string.IsNullOrEmpty(MulticastGroup))
                {
                    Logger.LogWarning("Cannot send to a multicast group as it is not configured. You have to call JoinMulticastGroup first");
                    return;
                }

                // Construct destination IPEndPoint
                Logger.LogDebug($"Send message {data} to {MulticastGroup}:{destinationPort}" );
                IPAddress address = IPAddress.Parse(MulticastGroup);
                IPEndPoint destination = new IPEndPoint(address, destinationPort);
                
                byte[] bytes = Encoding.ASCII.GetBytes(data);

                _clients.ForEach(n=>n.Send(bytes, bytes.Length, destination));
            }
            catch (Exception exc)
            {
                Logger.LogError(exc, exc.Message);
            }
        }

        #endregion

        #region PRIVATE: FUNCTIONS

        /// <summary>
        /// Receiving thread. Only for UDP Rx
        /// </summary>
        private void ReceiveThread()
        {
            IPEndPoint sourceIp = null;
            
            while (Enabled)
            {
                foreach (var udpClient in _clients)
                {
                    if (udpClient.Available == 0)
                    {
                        continue;
                    }

                    if (!Enabled)
                        break;

                    try
                    {
                        byte[] dataReceived = udpClient.Receive(ref sourceIp);
                        var str = Encoding.Default.GetString(dataReceived);
                        Logger.LogDebug($"Reveided {str} from {sourceIp}");

                        DataReceived?.Invoke(this, new MulticastData
                        {
                            Message = str,
                            Source = sourceIp
                        });
                    }
                    catch (Exception exc)
                    {
                        Logger.LogError(exc, exc.Message);
                    }
                }

                Thread.Sleep(100);
            }

            Logger.LogDebug("Receiving thread finished.");
        }

        #region Overrides of UdpClient

        public void Dispose()
        {
            StopService();

            _client.Dispose();
            _clients.ForEach(n=>n.Dispose());
            _clients.Clear();   
        }

        #endregion

        #endregion
    }

}
