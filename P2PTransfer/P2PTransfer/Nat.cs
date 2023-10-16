// updated version of https://www.codeproject.com/articles/27992/nat-traversal-with-upnp-in-c﻿
namespace P2PTransfer
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Net.Sockets;
    using System.Net;
    using System.Xml;

    public class NAT
    {
        static TimeSpan _timeout = new TimeSpan(0, 0, 0, 3);
      
        static string? _serviceUrl;

        static readonly HttpClient _client = new HttpClient();


        public static async Task<bool> DiscoverAsync()
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            var req = "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: 239.255.255.250:1900\r\n" +
            "ST:upnp:rootdevice\r\n" +
            "MAN:\"ssdp:discover\"\r\n" +
            "MX:3\r\n\r\n";
            var data = Encoding.ASCII.GetBytes(req);
            var ipe = new IPEndPoint(IPAddress.Broadcast, 1900);
            var buffer = new byte[0x1000];
            s.ReceiveTimeout = 3000;

            s.SendTo(data, ipe);
            s.SendTo(data, ipe);
            s.SendTo(data, ipe);

            try
            {
                var length = s.Receive(buffer);

                var resp = Encoding.ASCII.GetString(buffer, 0, length).ToLower();
                if (resp.Contains("upnp:rootdevice"))
                {
                    resp = resp.Substring(resp.ToLower().IndexOf("location:") + 9);
                    resp = resp.Substring(0, resp.IndexOf("\r")).Trim();
                    if (!string.IsNullOrEmpty(_serviceUrl = await GetServiceUrlAsync(resp)))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        private static async Task<string?> GetServiceUrlAsync(string resp)
        {
            try
            {
                var desc = new XmlDocument();
                using var xmlStream = await _client.GetStreamAsync(new Uri(resp));
                desc.Load(xmlStream);
                var nsMgr = new XmlNamespaceManager(desc.NameTable);
                nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
                var typen = desc.SelectSingleNode("//tns:device/tns:deviceType/text()", nsMgr);

                if (!typen.Value.Contains("InternetGatewayDevice")) return null;

                var node = desc
                    .SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:WANIPConnection:1\"]/tns:controlURL/text()", nsMgr);

                if (node == null) return null;

                var eventnode = desc
                    .SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:WANIPConnection:1\"]/tns:eventSubURL/text()", nsMgr);

                return CombineUrls(resp, node.Value);
            }
            catch { return null; }
        }

        private static string CombineUrls(string resp, string p)
        {
            int n = resp.IndexOf("://");
            n = resp.IndexOf('/', n + 3);
            return resp.Substring(0, n) + p;
        }

        public static async Task ForwardPortAsync(string? localIP, int internalPort, int externalPort, ProtocolType protocol, string description)
        {
            if (string.IsNullOrEmpty(_serviceUrl)) throw new Exception("No UPnP service available or Discover() has not been called");

            await SOAPRequestAsync(_serviceUrl,
                "<u:AddPortMapping xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
                "<NewRemoteHost></NewRemoteHost>" +
                $"<NewExternalPort>{externalPort}</NewExternalPort>" +
                $"<NewProtocol>{protocol.ToString().ToUpper()}</NewProtocol>" +
                $"<NewInternalPort>{internalPort}</NewInternalPort>" +
                $"<NewInternalClient>{localIP}</NewInternalClient>" +
                $"<NewEnabled>{1}</NewEnabled>" +
                $"<NewPortMappingDescription>{description}</NewPortMappingDescription>" +
                $"<NewLeaseDuration>{0}</NewLeaseDuration>" +
                "</u:AddPortMapping>", "AddPortMapping");
        }

        public static async Task DeleteForwardingRuleAsync(int externalPort, ProtocolType protocol)
        {
            if (string.IsNullOrEmpty(_serviceUrl)) throw new Exception("No UPnP service available or Discover() has not been called");

            await SOAPRequestAsync(_serviceUrl,
                "<u:DeletePortMapping xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
                "<NewRemoteHost></NewRemoteHost>" +
                $"<NewExternalPort>{externalPort}</NewExternalPort>" +
                $"<NewProtocol>{protocol.ToString().ToUpper()}</NewProtocol>" +
                "</u:DeletePortMapping>", "DeletePortMapping");
        }

        public static async Task<IPAddress> GetExternalIPAsync()
        {
            if (string.IsNullOrEmpty(_serviceUrl)) throw new Exception("No UPnP service available or Discover() has not been called");

            var xdoc = await SOAPRequestAsync(_serviceUrl, 
                "<u:GetExternalIPAddress xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\"></u:GetExternalIPAddress>", "GetExternalIPAddress");
            var nsMgr = new XmlNamespaceManager(xdoc.NameTable);
            nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
            var IP = xdoc.SelectSingleNode("//NewExternalIPAddress/text()", nsMgr)?.Value;
            return IPAddress.Parse(IP);
        }

        private static async Task<XmlDocument> SOAPRequestAsync(string url, string soap, string function)
        {
            try
            {

                string req = "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                $"<s:Body>{soap}</s:Body>" +
                "</s:Envelope>";

                var content = new StringContent(req, Encoding.UTF8, "text/xml");
                    content.Headers.Add("SOAPACTION", $"\"urn:schemas-upnp-org:service:WANIPConnection:1#{function}\"");
                var response = await (await _client.PostAsync(url, content)).Content.ReadAsStreamAsync();
               
                XmlDocument resp = new XmlDocument();
                resp.Load(response);
                return resp;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}
