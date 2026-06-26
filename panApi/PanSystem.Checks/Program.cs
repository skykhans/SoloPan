using PanSystem.Utils;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var hash = PasswordHelper.HashPassword("abc12345");
Check(PasswordHelper.VerifyPassword("abc12345", hash, out var needsRehash), "PBKDF2 password should verify");
Check(!needsRehash, "Fresh PBKDF2 hash should not need rehash");
Check(!PasswordHelper.VerifyPassword("bad-pass", hash, out _), "Wrong password should fail");

var legacy = HashHelper.ComputeMd5("abc12345");
Check(PasswordHelper.VerifyPassword("abc12345", legacy, out needsRehash), "Legacy MD5 password should verify");
Check(needsRehash, "Legacy MD5 password should need rehash");

Check(IpRuleParser.TryParseRule("192.168.1.0/24", out var start, out var end, out var rule, out _), "CIDR should parse");
Check(rule == "192.168.1.0/24", "CIDR should normalize");
Check(start == 3232235776 && end == 3232236031, "CIDR range should match /24");
Check(IpRuleParser.TryParseClientIp("::ffff:192.168.1.8", out var ip, out var normalized), "IPv4 mapped IPv6 should parse");
Check(ip == 3232235784 && normalized == "192.168.1.8", "IPv4 mapped IPv6 should normalize");

Console.WriteLine("checks passed");
