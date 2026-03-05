using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HEMA.WpfApp;

public class TcpService
{
	private const int tcpPort = 5002;
	private const int udpPort = 8888;
	private List<Task> listenTasks = new();

	public string ServerName => Environment.MachineName;

	/// <summary>
	/// Ожидание подключения.
	/// </summary>
	/// <param name="handleFightsRequest"></param>
	/// <param name="acceptFights"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task HostTask(
		Func<string> handleFightsRequest,
		Action<string> acceptFights,
		CancellationToken cancellationToken)
	{
		listenTasks.Add(AnswerAsServerTask());

		TcpListener listener = new TcpListener(IPAddress.Any, tcpPort);
		listener.Start();

		return Task.Run(async () =>
		{
			while (true)
			{
				var anyConnectionReqeust = listener.Pending();
				if (!anyConnectionReqeust)
				{
					await Task.Delay(200);
					continue;
				}

				TcpClient client = await listener.AcceptTcpClientAsync();
				NetworkStream stream = client.GetStream();
				listenTasks.Add(WaitForMessages(handleFightsRequest, acceptFights, stream, cancellationToken));
			}
		}, cancellationToken);
	}

	public async Task<Dictionary<string, IPAddress>> GetAllHosts(int waitInSeconds)
	{
		using var udp = new UdpClient();
		udp.EnableBroadcast = true;
		var probe = Encoding.UTF8.GetBytes("DISCOVER");
		await udp.SendAsync(probe, probe.Length, new IPEndPoint(IPAddress.Broadcast, udpPort));

		var found = new Dictionary<string, IPAddress>();
		var stopAt = DateTime.UtcNow.AddSeconds(waitInSeconds);

		while (DateTime.UtcNow <= stopAt)
		{
			var waitTask = udp.ReceiveAsync();
			var done = await Task.WhenAny(waitTask, Task.Delay(200));

			if (done != waitTask) continue;

			var response = waitTask.Result;
			var name = Encoding.UTF8.GetString(response.Buffer);
			found.TryAdd(name, response.RemoteEndPoint.Address);
		}

		return found;
	}

	public async Task ConnectAndReqeustFights(string serverIp, CancellationToken cancellationToken)
	{
		NetworkStream stream = await ConnectToServer(serverIp);
		await stream.WriteAsync(Encoding.UTF8.GetBytes("S"), cancellationToken);
	}

	public async Task ConnectAndSendFights(string serverIp, string fights, CancellationToken cancellationToken)
	{
		NetworkStream stream = await ConnectToServer(serverIp);
		await SendFightsAsync(stream, fights, cancellationToken);
	}

	private Task AnswerAsServerTask()
	{
		return Task.Run(async () =>
		{
			using var udp = new UdpClient(udpPort);
			while (true)
			{
				var req = await udp.ReceiveAsync();
				var text = Encoding.UTF8.GetString(req.Buffer);

				if (text == "DISCOVER")
				{
					var bytes = Encoding.UTF8.GetBytes($"{ServerName}");
					await udp.SendAsync(bytes, bytes.Length, req.RemoteEndPoint);
				}
				await Task.Delay(200);
			}
		});
	}

	private Task WaitForMessages(
		Func<string> handleFightsRequest,
		Action<string> acceptFights,
		NetworkStream stream,
		CancellationToken cancellationToken)
	{
		return Task.Run(async () =>
		{
			while (true)
			{
				int size = 1;
				var commanad = await GetMessageAsync(stream, size, cancellationToken);
				switch (commanad)
				{
					case "S":                                                                       // пришла команда на отправку
						string fightsMessage = await ReadFightsMessageAsync(stream, cancellationToken);
						acceptFights(fightsMessage);
						continue;
					case "R":                                                                       // пришла команда-запрос
						var fights = handleFightsRequest();
						await SendFightsAsync(stream, fights, cancellationToken);
						continue;
				}
				await Task.Delay(200);
			}
		});
	}

	private static async Task<NetworkStream> ConnectToServer(string serverIp)
	{
		TcpClient client = new TcpClient();
		await client.ConnectAsync(serverIp, tcpPort);
		NetworkStream stream = client.GetStream();
		return stream;
	}

	private static async Task<string> ReadFightsMessageAsync(NetworkStream stream, CancellationToken cancellationToken)
	{
		var fightsMessageLength = await GetIntMessageAsync(stream, cancellationToken);
		return await GetMessageAsync(stream, fightsMessageLength, cancellationToken);
	}

	private async Task SendFightsAsync(NetworkStream stream, string fights, CancellationToken cancellationToken)
	{
		await stream.WriteAsync(Encoding.UTF8.GetBytes("S"));                      // отправить команду на отправку
		var fightsMessage = Encoding.UTF8.GetBytes(fights);
		var fightsLengsMessage = BitConverter.GetBytes(fightsMessage.Length);
		await stream.WriteAsync(fightsLengsMessage, cancellationToken);            // отправить длинну сообщения
		await stream.WriteAsync(fightsMessage, cancellationToken);                 // отправить бои
	}

	private static async Task<string> GetMessageAsync(NetworkStream stream, int size, CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[size];
		int bytes = await stream.ReadAsync(buffer, cancellationToken);
		return Encoding.UTF8.GetString(buffer, 0, bytes);
	}

	private static async Task<int> GetIntMessageAsync(NetworkStream stream, CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[4];
		int bytes = await stream.ReadAsync(buffer, cancellationToken);
		return BitConverter.ToInt32(buffer, 0);
	}
}