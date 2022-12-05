using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Class for UDP communication
    /// </summary>
    public class MulticastClient : UdpClient
    {
        #region FIELDS

        private Thread _receiverThread;
        
        #endregion

        #region PUBLIC: CONSTRUCTOR

        /// <summary>
        /// Initializes a new instance of the UDP class
        /// </summary>
        public MulticastClient(
            ILogger<MulticastClient> logger)
        {
            Logger = logger;
            Enabled = false;
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
        /// 
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
            }
        }
        
        /// <summary>
        /// Stop Service. If Type= RX Reception thread will close
        /// </summary>
        public void StopService()
        {
            using (Logger.BeginScope(nameof(StopService)))
            {
                // disable service
                Enabled = false;

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
            }
        }

        /// <summary>
        /// Join this multicast group on all available network interfaces.
        /// </summary>
        /// <param name="multicastGroup"></param>
        /// <param name="rxPort"></param>
        /// <param name="timeToLive"></param>
        private void JoinMulticastGroup(string multicastGroup, int rxPort, int timeToLive = 1)
        {
            var list = StaticHelpers.GetAllLocalIpAddress();
            foreach (var s in list)
            {
                JoinMulticastGroup2(multicastGroup, rxPort, timeToLive, s);
            }
        }

        /// <summary>
        /// Join to a listening multicast group
        /// </summary>
        /// <param name="multicastGroup"></param>
        /// <param name="rxPort">multicast receiving port</param>
        /// <param name="timeToLive">Time to live. Number of network jumps a multicast packet can do </param>
        /// <param name="localIp"></param>
        public void JoinMulticastGroup2(string multicastGroup, int rxPort, int timeToLive = 1, string localIp = "")
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
                
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                ReceivePort = rxPort;
                EnableBroadcast = true;
                ExclusiveAddressUse = false;
                Client.Bind(localPt);
                
                // Join socket to a Multicast group
                JoinMulticastGroup(multicastAddress, timeToLive);
            }
            catch (Exception exc)
            {
                Debug(exc);
            }
        }
        
        /// <summary>
        /// Send Data to destination Ip and Port
        /// </summary>
        /// <param name="data">Byte Array</param>
        /// <param name="multicastGroup">Destination Ip</param>
        /// <param name="destinationPort">Destination Port</param>
        public void Send(string data, string multicastGroup, int destinationPort)
        {
            try
            {
                // Construct destination IPEndPoint
                Logger.LogDebug($"Send message {data} to {multicastGroup}:{destinationPort}" );
                IPAddress address = IPAddress.Parse(multicastGroup);
                IPEndPoint destination = new IPEndPoint(address, destinationPort);
                
                byte[] bytes = Encoding.ASCII.GetBytes(data);

                // Call udp client Send Data
                Send(bytes, bytes.Length, destination);
            }
            catch (Exception exc)
            {
                Debug(exc);
            }
        }


        //// TODO: I will need this in the future to allow user to select network interface for RED and BLUE
        /// <summary>
        /// show Network interfaces
        /// </summary>
        public static string[] ShowNetworkInterfaces()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            Console.WriteLine("Interface information for {0}.{1}     ",
                    computerProperties.HostName, computerProperties.DomainName);
            if (nics == null || nics.Length < 1)
            {
                Console.WriteLine("  No network interfaces found.");
                return null;
            }

            Console.WriteLine("  Number of interfaces .................... : {0}", nics.Length);
            string NetworkInterfaces = string.Empty;
            foreach (NetworkInterface adapter in nics)
            {
                UnicastIPAddressInformation info = adapter.GetIPProperties().UnicastAddresses[0];
                string index = info.Address.ToString();
                IPInterfaceProperties properties = adapter.GetIPProperties(); //  .GetIPInterfaceProperties();
                Console.WriteLine();
                Console.WriteLine(adapter.Description);

                if (NetworkInterfaces != string.Empty)
                {
                    NetworkInterfaces += ";";
                }
                NetworkInterfaces += adapter.Description;

                Console.WriteLine(String.Empty.PadLeft(adapter.Description.Length, '='));
                Console.WriteLine("  Interface type .......................... : {0}", adapter.NetworkInterfaceType);
                Console.Write("  Physical address ........................ : ");
                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();
                for (int i = 0; i < bytes.Length; i++)
                {
                    // Display the physical address in hexadecimal.
                    Console.Write("{0}", bytes[i].ToString("X2"));
                    // Insert a hyphen after each byte, unless we are at the end of the 
                    // address.
                    if (i != bytes.Length - 1)
                    {
                        Console.Write("-");
                    }
                }
                Console.WriteLine();
            }

            return NetworkInterfaces.Split(';');
        }

        //// TODO: I will need this in the future to allow user to select network interface for RED and BLUE
        /// <summary>
        /// show Network interfaces
        /// </summary>
        public static string[] ShowNetworkLocalIps()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            Console.WriteLine("Interface information for {0}.{1}     ",
                    computerProperties.HostName, computerProperties.DomainName);
            if (nics == null || nics.Length < 1)
            {
                Console.WriteLine("  No network interfaces found.");
                return null;
            }

            Console.WriteLine("  Number of interfaces .................... : {0}", nics.Length);
            string NetworkInterfaces = string.Empty;
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties property = adapter.GetIPProperties();
                if (property.UnicastAddresses.Count == 0)
                {
                    continue;
                }
                UnicastIPAddressInformation info = property.UnicastAddresses[0];
                string index = info.Address.ToString();
                Console.WriteLine();
                Console.WriteLine(adapter.Description);

                if (NetworkInterfaces != string.Empty)
                {
                    NetworkInterfaces += ";";
                }
                NetworkInterfaces += index;

                Console.WriteLine(String.Empty.PadLeft(adapter.Description.Length, '='));
                Console.WriteLine("  Interface type .......................... : {0}", adapter.NetworkInterfaceType);
                Console.Write("  Physical address ........................ : ");
                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();
                for (int i = 0; i < bytes.Length; i++)
                {
                    // Display the physical address in hexadecimal.
                    Console.Write("{0}", bytes[i].ToString("X2"));
                    // Insert a hyphen after each byte, unless we are at the end of the 
                    // address.
                    if (i != bytes.Length - 1)
                    {
                        Console.Write("-");
                    }
                }
                Console.WriteLine();
            }

            return NetworkInterfaces.Split(';');
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
                if (Available == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                if (!Enabled)
                    break;

                try
                {
                    byte[] dataReceived = Receive(ref sourceIp);
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
                    Debug(exc);
                }
            }

            Logger.LogDebug("Receiving thread finished.");
        }

        /// <summary>
        /// Send exceptions to a common debugging process. If DEBUG is on messages are sent to Console.
        /// If DEBUG is off any error will through an exception
        /// </summary>
        /// <param name="exc">Exception to analyze</param>
        private void Debug(Exception exc)
        {
            Logger.LogError(exc, exc.Message);
        }

        /// <summary>
        /// Send Debug text to Console only if DEBUG = true
        /// </summary>
        /// <param name="text">Text that will be written in the console</param>
        private void Debug(string text)
        {
            Logger.LogTrace(text);
        }

        #region Overrides of UdpClient

        /// <summary>Releases the unmanaged resources used by the <see cref="T:System.Net.Sockets.UdpClient"></see> and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            StopService();
            base.Dispose(disposing);    
        }

        #endregion

        #endregion
    }

}
