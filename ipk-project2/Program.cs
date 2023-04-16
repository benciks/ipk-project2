// Author: Simon Bencik
// Login: xbenci01

using CommandLine;
using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;

namespace ipk_project2
{
    class Program
    {
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
            var arguments = new Options();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
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
                    arguments = o;

                });
            
            var device = (LibPcapLiveDevice)CaptureDeviceList.Instance[arguments.Interface];
            if (device == null)
            {
                Console.WriteLine("Device {0} not found.", arguments.Interface);
                return;
            }

            device.OnPacketArrival += new PacketArrivalEventHandler(OnPacketArrival);
            
            device.Open(DeviceModes.Promiscuous);
            device.Capture(arguments.Number);
        }
        
        private static void OnPacketArrival(object sender, PacketCapture e)
        {
            // Extract the packet data
            var packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);
            var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            var udpPacket = packet.Extract<PacketDotNet.UdpPacket>();
            var ipPacket = packet.Extract<PacketDotNet.IPPacket>();
            var ethernetPacket = packet.Extract<PacketDotNet.EthernetPacket>();

            // Print the packet data
            var time = e.GetPacket().Timeval.Date.ToUniversalTime().ToString("yyy-MM-dd'T'HH:mm:ss.fffK");
            var srcMac = ethernetPacket?.SourceHardwareAddress.ToString() ?? "N/A";
            var dstMac = ethernetPacket?.DestinationHardwareAddress.ToString() ?? "N/A";
            var frameLength = e.GetPacket().Data.Length;
            var srcIp = ipPacket?.SourceAddress.ToString() ?? "N/A";
            var dstIp = ipPacket?.DestinationAddress.ToString() ?? "N/A";
            var srcPort = tcpPacket?.SourcePort ?? udpPacket?.SourcePort ?? 0;
            var dstPort = tcpPacket?.DestinationPort ?? udpPacket?.DestinationPort ?? 0;
            var byteOffset = BitConverter.ToString(e.GetPacket().Data);

            Console.WriteLine("timestamp: {0}", time);
            Console.WriteLine("src MAC: {0}", srcMac);
            Console.WriteLine("dst MAC: {0}", dstMac);
            Console.WriteLine("frame length: {0}", frameLength);
            Console.WriteLine("src IP: {0}", srcIp);
            Console.WriteLine("dst IP: {0}", dstIp);
            Console.WriteLine("src port: {0}", srcPort);
            Console.WriteLine("dst port: {0}", dstPort);
            Console.WriteLine();
            Console.WriteLine(" {0}", byteOffset);
            Console.WriteLine();
        }
    }
}