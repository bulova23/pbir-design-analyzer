using System.Net;
using System.Net.Sockets;

if (args.Length != 1) return 2;
switch (args[0])
{
    case "success": Console.Write("fixture-ok"); return 0;
    case "bounded-output": Console.Write("bounded"); return 0;
    case "excessive-output": Console.Write(new string('x', 4096)); return 0;
    case "timeout": await Task.Delay(Timeout.InfiniteTimeSpan); return 0;
    case "environment": Console.Write(Environment.GetEnvironmentVariable("PHASE35E_SECRET") is null ? "secret-absent" : "secret-present"); return 0;
    case "forbidden-read": Console.Write(File.ReadAllText("/etc/hosts")); return 0;
    case "forbidden-write": File.WriteAllText("/tmp/phase35e-forbidden", "forbidden"); return 0;
    case "child-process": System.Diagnostics.Process.Start("/usr/bin/true"); return 0;
    case "network": using (var client = new TcpClient()) { await client.ConnectAsync(IPAddress.Loopback, 9); } return 0;
    case "malformed-result": Console.Write("not-a-result-envelope"); return 0;
    case "non-zero": return 7;
    default: return 2;
}
