# IPK Project 2 - ZETA: Network sniffer
Author: Šimon Benčík, xbenci01

## Introduction
The goal of the project was to create a Network Sniffer using the *pcap library. The sniffer should allow various arguments to help specify the capture protocol, port and packet count.

## Theory
A network sniffer is a tool used to monitor and analyze network traffic. It captures packets flowing through the network on a configured interface and analyzes their contents. Sniffers are used for a variety of purposes, including diagnosing network problems, preventing security threats, or optimizing network performance.

## Design
Sniffer is written in C# using the .NET 6.0 framework. The **CommandLineParser** library was used to parse arguments, which handles many edge cases and helps keep the argument parsing code clean and short. The **SharpPcap** library was used to handle interfaces and data capture. This library is built on top of libpcap and WinPcap and provides a simple API for the .NET framework. In addition, the **PacketDotNet** library was used to parse incoming packets.

## Implementation
Initially, the arguments are parsed based on the Options class using CommandLineParser. If no interface is specified, the program displays a list of all active interfaces and exits, otherwise it checks if the specified interface is contained in the CaptureDeviceList class provided by SharpPcap. In addition, the correct format and port range are checked. When a device is assigned to a variable, its type is changed to control the amount of packets captured, since the CaptureDeviceList instance does not provide an argument to the Capture method. Then, device.onPacketArrival is assigned a handler, represented by a helper function. In this method, the necessary packet data is extracted and dumped. To print the byte offset, a helper function has been created that iterates the packet data and prints the hex byte offset as well as its ASCII representation. This byte offset output matches the output presented in Wireshark. A dictionary is created for filters and their bool values. If the value is true, the key is added to the filters array, except for the mld and ndp protocols, which need icmp6.type to be set. When the loop is complete, the filters array containing the strings is concatenated using the "or" keyword. This allows multiple protocols to be filtered at once.

## Testing
Testing was performed manually throughout the development process. The vast majority of testing focused on the output packet format (correct addresses, byte offset representation), filter functionality, and incorrect inputs to avoid unhandled exceptions. The process consisted of the following steps: 

1. First, packet capture for the various protocols on the loopback interface (since there is less traffic on the loopback interface) was initiated using **tcpreplay**. To test the different protocols, the captures were downloaded from [PacketLife](https://packetlife.net/captures/).
2. After capturing the packets with network sniffer, the output was compared to the one in Wireshark, where the capture file could be opened and analyzed.
3. Some protocols, namely ndp and mld, were more difficult to test this way (there were no captures available on PacketLife for mld), so a small python script using the scapy library was used to mock and send these packets.

The test environment was macOS 13.2.1 as well as a reference virtual machine running nixOS. You can see some sample outputs compared to wireshark in the following figures:

<table>
  <tr>
    <th> Sniffer </th>
    <th> Wireshark </th>
  </tr>
  <tr>
    <td>
        <p><b>TCP</b></p>
        <img src="images/tcp_sniffer.png">
    </td>
    <td>
        <img src="images/tcp_wireshark.png">
    </td>
  </tr>
  <tr>
    <td>
        <p><b>UDP</b></p>
        <img src="images/udp_sniffer.png">
    </td>
    <td>
        <img src="images/udp_wireshark.png">
    </td>
  </tr>
  <tr>
    <td>
        <p><b>ARP</b></p>
        <img src="images/arp_sniffer.png">
    </td>
    <td>
        <img src="images/arp_wireshark.png">
    </td>
  </tr>
  <tr>
    <td>
        <p><b>ICMPv6</b></p>
        <img src="images/icmp6_sniffer.png">
    </td>
    <td>
        <img src="images/icmp6_wireshark.png">
    </td>
  </tr>
</table>

## Sources
- C# reference - https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/
- CommandLineParser - https://github.com/commandlineparser/commandline
- sharppcap - https://github.com/dotpcap/sharppcap
- pcap-filter man page https://www.tcpdump.org/manpages/pcap-filter.7.html
- packetlife - https://packetlife.net/captures/
- tcpreplay man page https://linux.die.net/man/1/tcpreplay
- ICMPv6 Parameters - https://www.iana.org/assignments/icmpv6-parameters/icmpv6-parameters.xhtml