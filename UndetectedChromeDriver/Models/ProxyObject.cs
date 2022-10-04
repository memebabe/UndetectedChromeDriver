using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndetectedChromeDriver.Models.Network.Proxy
{
    public class ProxyObject
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Method { get; set; } = "socks5";

        public override string ToString()
        {
            if (string.IsNullOrEmpty(User))
                return $"{Host}:{Port}";
            else
                return $"{Host}:{Port}:{User}:{Password}:{Method}";
        }

        public string ToJsonString()
        {
            JObject json = new JObject();

            json["auth"] = new JObject();
            json["auth"]["user"] = User;
            json["auth"]["pass"] = Password;

            json["socks_type"] = "socks5";
            json["pac_type"] = "file://";
            json["bypasslist"] = "";
            json["rules_mode"] = "Whitelist";
            json["proxy_rule"] = "singleProxy";
            json["internal"] = "";

            switch (Method)
            {
                case "socks4":
                    json["socks_host"] = Host;
                    json["socks_port"] = Port;
                    json["socks_type"] = "socks4";
                    break;
                case "socks5":
                    json["socks_host"] = Host;
                    json["socks_port"] = Port;
                    json["socks_type"] = "socks5";
                    break;
                case "http":
                    json["http_host"] = Host;
                    json["http_port"] = Port;
                    break;
                case "https":
                    json["https_host"] = Host;
                    json["https_port"] = Port;
                    break;
            }

            return json.ToString();
        }

        public static ProxyObject Parse(string proxyString)
        {
            try
            {
                ProxyObject proxyObject = new ProxyObject();
                var strs = proxyString.Split(new char[] { ':', '|' });
                if (strs.Length > 1)
                {
                    proxyObject.Host = strs[0];
                    proxyObject.Port = int.Parse(strs[1]);
                    if (strs.Length > 2)
                        proxyObject.User = strs[2];
                    if (strs.Length > 3)
                        proxyObject.Password = strs[3];
                    if (strs.Length > 4)
                        proxyObject.Method = strs[4];

                    return proxyObject;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    
    }
}
