using System.Net;

namespace HEMA.WpfApp;

public sealed class HostInfo
{
	public string Name { get; set; } = "";
	public IPAddress IpAddress { get; set; }

	public override string ToString()
	{
		return $"{Name} | {IpAddress}";
	}
}
