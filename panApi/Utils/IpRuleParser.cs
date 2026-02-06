using System.Net;
using System.Net.Sockets;

namespace PanSystem.Utils
{
    public static class IpRuleParser
    {
        public static bool TryParseRule(string ruleText, out long startIp, out long endIp, out string normalizedRule, out string error)
        {
            startIp = 0;
            endIp = 0;
            normalizedRule = string.Empty;
            error = string.Empty;

            var raw = (ruleText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "规则不能为空";
                return false;
            }

            if (raw.Contains('/'))
            {
                var parts = raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    error = "CIDR 格式无效，应为 x.x.x.x/前缀";
                    return false;
                }

                if (!TryParseIpv4ToUInt(parts[0], out var ipValue))
                {
                    error = "CIDR 的IP不合法，仅支持IPv4";
                    return false;
                }

                if (!int.TryParse(parts[1], out var prefix) || prefix < 0 || prefix > 32)
                {
                    error = "CIDR 前缀范围应为 0-32";
                    return false;
                }

                uint mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
                uint start = ipValue & mask;
                uint end = start | ~mask;
                startIp = start;
                endIp = end;
                normalizedRule = $"{UIntToIpv4(start)}/{prefix}";
                return true;
            }

            if (raw.Contains('-'))
            {
                var parts = raw.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    error = "IP段格式无效，应为 x.x.x.x-x.x.x.x";
                    return false;
                }

                if (!TryParseIpv4ToUInt(parts[0], out var start) || !TryParseIpv4ToUInt(parts[1], out var end))
                {
                    error = "IP段中存在非法IP，仅支持IPv4";
                    return false;
                }

                if (start > end)
                {
                    error = "IP段起始地址不能大于结束地址";
                    return false;
                }

                startIp = start;
                endIp = end;
                normalizedRule = $"{UIntToIpv4(start)}-{UIntToIpv4(end)}";
                return true;
            }

            if (!TryParseIpv4ToUInt(raw, out var single))
            {
                error = "IP格式无效，仅支持IPv4";
                return false;
            }

            startIp = single;
            endIp = single;
            normalizedRule = UIntToIpv4(single);
            return true;
        }

        public static bool TryParseClientIp(string? ipText, out long ipNumber, out string normalizedIp)
        {
            ipNumber = 0;
            normalizedIp = string.Empty;

            if (string.IsNullOrWhiteSpace(ipText))
            {
                return false;
            }

            var raw = ipText.Trim();
            if (!IPAddress.TryParse(raw, out var parsed))
            {
                return false;
            }

            if (parsed.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (IPAddress.IsLoopback(parsed))
                {
                    parsed = IPAddress.Loopback;
                }
                else if (parsed.IsIPv4MappedToIPv6)
                {
                    parsed = parsed.MapToIPv4();
                }
                else
                {
                    return false;
                }
            }

            if (parsed.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            var bytes = parsed.GetAddressBytes();
            ipNumber = ((long)bytes[0] << 24) | ((long)bytes[1] << 16) | ((long)bytes[2] << 8) | bytes[3];
            normalizedIp = parsed.ToString();
            return true;
        }

        public static string NormalizeIpForDisplay(string? ipText)
        {
            if (string.IsNullOrWhiteSpace(ipText))
            {
                return string.Empty;
            }

            if (!IPAddress.TryParse(ipText.Trim(), out var parsed))
            {
                return ipText.Trim();
            }

            if (parsed.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (IPAddress.IsLoopback(parsed))
                {
                    return IPAddress.Loopback.ToString();
                }

                if (parsed.IsIPv4MappedToIPv6)
                {
                    return parsed.MapToIPv4().ToString();
                }
            }

            return parsed.ToString();
        }

        private static bool TryParseIpv4ToUInt(string ipText, out uint value)
        {
            value = 0;
            if (!IPAddress.TryParse(ipText, out var ip)) return false;
            if (ip.AddressFamily == AddressFamily.InterNetworkV6 && ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }
            if (ip.AddressFamily != AddressFamily.InterNetwork) return false;

            var bytes = ip.GetAddressBytes();
            value = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
            return true;
        }

        private static string UIntToIpv4(uint ip)
        {
            return $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
        }
    }
}
