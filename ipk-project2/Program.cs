// Author: Simon Bencik
// Login: xbenci01

using System.Reflection.PortableExecutable;
using CommandLine;
using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;

namespace ipk_project2
{
    class Program
    {
        // Specify arguments for CommandLineParser
        public class Options
        {
            [Option('i', "interface", Required = false, HelpText = "Interface to use." )]
            public string? Interface { get; set; }
            
            [Option('t', "tcp", Required = false, HelpText = "Display TCP segments")]
            public bool Tcp { get; set; }
            
            [Option('u', "udp", Required = false, HelpText = "Display UDP segments")]
            public bool Udp { get; set; }
            
            [Option('p', Required = false, HelpText = "Filter by port number")]
            public string? Port { get; set; }
            
            [Option(longName:"icmp4", Required = false, HelpText = "Display only ICMPv4 Packets")]
            public bool Icmp4 { get; set; }
            
            [Option(longName:"icmp6", Required = false, HelpText = "Display only ICMPv6 Packets")]
            public bool Icmp6 { get; set; }
            
            [Option(longName:"arp", Required = false, HelpText = "Display only ARP Packets")]
            public bool Arp { get; set; }
            
            [Option(longName:"ndp", Required = false, HelpText = "Display only ICMPv6 NDP packets")]
            public bool Ndp { get; set; }
            
            [Option(longName: "igmp", Required = false, HelpText = "Display only IGMP Packets")]
            public bool Igmp { get; set; }
            
            [Option(longName:"mld", Required = false, HelpText = "Display only ICMPv6 MLD packets")]
            public bool Mld { get; set; }
            
            [Option(shortName:'n', Required = false, Default = 1, HelpText = "Number of packets to display")]
            public int Number { get; set; }
        }

        static void Main(string[] args)
        {
            // Parse arguments
            var arguments = new Options();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    // Handle empty interface or print interfaces
                    if (o.Interface == null)
                    {
                        var interfaces = CaptureDeviceList.Instance;
                        
                        if (interfaces.Count < 1)
                        {
                            Console.WriteLine("No capture devices found.");
                            return;
                        }

                        Console.WriteLine("Active Network Interfaces:");
                        
                        foreach (var inter in interfaces)
                        {
                            Console.WriteLine("{0}", inter.Name);
                        }
                        
                        Environment.Exit(0);
                    }
                    
                    // Check correct port number and range
                    if (o.Port != null)
                    {
                        int portNumber;
                        if (!Int32.TryParse(o.Port, out portNumber))
                        {
                            Console.WriteLine("Port number is not a number");
                            Environment.Exit(1);
                        }
                        
                        if (portNumber < 0 || portNumber > 65535)
                        {
                            Console.WriteLine("Port number out of range (0-65535)");
                            Environment.Exit(1);
                        }
                    }
                    
                    arguments = o;

                });
            
            // Check if interface exists and get it
            var device = CaptureDeviceList.Instance.FirstOrDefault(
                    d => d.Name.Equals(arguments.Interface, StringComparison.OrdinalIgnoreCase))
                    as LibPcapLiveDevice;            
            
            // Otherwise if device is null print error and exit
            if (device == null)
            {
                Console.WriteLine("Device {0} not found.", arguments.Interface);
                Environment.Exit(1);
            }

            // Define packet handling method
            device.OnPacketArrival += new PacketArrivalEventHandler(OnPacketArrival);
            
            // Create dictionary with arguments to iterate over
            Dictionary<string, bool> filterOptions = new Dictionary<string, bool>();
            filterOptions["tcp"] = arguments.Tcp;
            filterOptions["udp"] = arguments.Udp;
            filterOptions["icmp"] = arguments.Icmp4;
            filterOptions["icmp6"] = arguments.Icmp6;
            filterOptions["arp"] = arguments.Arp;
            filterOptions["ndp"] = arguments.Ndp;
            filterOptions["igmp"] = arguments.Igmp;
            filterOptions["mld"] = arguments.Mld;
            
            // Helper list for filters
            List<string> filters = new List<string>();
            
            // Loop the dictionary and append filters
            foreach (KeyValuePair<string, bool> filter in filterOptions)
            {
                if (filter.Key == "ndp" && filter.Value)
                {
                    filters.Add("icmp6 and (icmp6[0] == 135 or icmp6[0] == 136)");
                }
                else if (filter.Key == "mld" && filter.Value)
                {
                    filters.Add("icmp6 and (icmp6[0] == 130 or icmp6[0] == 131 or icmp6[0] == 132)");
                }
                else if (filter.Key == "tcp" && filter.Value && arguments.Port != null)
                {
                    filters.Add($"tcp and port {arguments.Port}");
                }
                else if (filter.Key == "udp" && filter.Value && arguments.Port != null)
                {
                    filters.Add($"udp and port {arguments.Port}");
                }
                else if (filter.Value)
                {
                    filters.Add(filter.Key);
                }
            }
            
            // Join the filters with or and set the filter
            string filterString = string.Join(" or ", filters);
            
            device.Open(DeviceModes.Promiscuous, 1000);
            device.Filter = filterString;
            device.Capture(arguments.Number);
        }
        
        private static void OnPacketArrival(object sender, PacketCapture e)
        {
            // Extract the packet data into various objects (Some can be null)
            var packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);
            var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            var udpPacket = packet.Extract<PacketDotNet.UdpPacket>();
            var ipPacket = packet.Extract<PacketDotNet.IPPacket>();
            var ethernetPacket = packet.Extract<PacketDotNet.EthernetPacket>();

            // Print the packet data
            var time = e.GetPacket().Timeval.Date.ToUniversalTime().ToString("yyy-MM-dd'T'HH:mm:ss.fffK");
            var srcMac = ethernetPacket != null ? string.Join(":", ethernetPacket.SourceHardwareAddress.GetAddressBytes().Select(b => b.ToString("x2"))) : "N/A";
            var dstMac = ethernetPacket != null ? string.Join(":", ethernetPacket.DestinationHardwareAddress.GetAddressBytes().Select(b => b.ToString("x2"))) : "N/A";
            var frameLength = e.GetPacket().Data.Length;
            var srcIp = ipPacket?.SourceAddress.ToString() ?? null;
            var dstIp = ipPacket?.DestinationAddress.ToString() ?? null;
            var srcPort = tcpPacket?.SourcePort ?? udpPacket?.SourcePort ?? null;
            var dstPort = tcpPacket?.DestinationPort ?? udpPacket?.DestinationPort ?? null;

            Console.WriteLine();
            Console.WriteLine("timestamp: {0}", time);
            Console.WriteLine("src MAC: {0}", srcMac);
            Console.WriteLine("dst MAC: {0}", dstMac);
            Console.WriteLine("frame length: {0} bytes", frameLength);
            if (srcIp is not null)
            {
                Console.WriteLine("src IP: {0}", srcIp);
            }

            if (dstIp is not null)
            { 
                Console.WriteLine("dst IP: {0}", dstIp);
            }
            if (srcPort is not null)
            {
                Console.WriteLine("src port: {0}", srcPort);
            }
            if (dstPort is not null)
            {
                Console.WriteLine("dst port: {0}", dstPort);
            }
            Console.WriteLine();
            PrintDataOffset(e.GetPacket().Data);
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------------------------");
        }

        // Function that iterates over packet data and prints offset
        private static void PrintDataOffset(byte[] packetData)
        { 
            int byteOffset = 0;

            // Print the data offset
            while (byteOffset < packetData.Length)
            {
                Console.Write("0x{0:x4}: ", byteOffset);

                for (int i = 0; i < 16; i++)
                {
                    if (byteOffset + i < packetData.Length)
                    {
                        byte b = packetData[byteOffset + i];
                        Console.Write("{0:x2} ", b);
                    }
                    else
                    {
                        Console.Write("   ");
                    }
                }

                Console.Write(" ");

                // Print the ASCII offset
                for (int i = 0; i < 16; i++)
                {
                    if (byteOffset + i < packetData.Length)
                    {
                        byte b = packetData[byteOffset + i];
                        char c = (char)b;
                        if (b >= 32 && b <= 126)
                        {
                            Console.Write(c);
                        }
                        else
                        {
                            Console.Write(".");
                        }
                    }
                    else
                    {
                        Console.Write(" ");
                    }
                }

                Console.WriteLine();

                byteOffset += 16;
            }

        }
    }
}