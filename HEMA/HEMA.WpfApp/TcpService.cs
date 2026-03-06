using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace HEMA.WpfApp;

public sealed class TcpService : IDisposable
{
	private const int tcpPort = 5002;
	private const int udpPort = 8888;

	private CancellationTokenSource? _cts;
	private Task? _udpTask;
	private Task? _tcpTask;

	private UdpClient? _udpServer;
	private TcpListener? _tcpListener;

	public string ServerName => Environment.MachineName;

	/// <summary>
	/// Запустить фоновые слушатели (UDP discovery + TCP server).
	/// Вызывать один раз, например на старте приложения.
	/// </summary>
	public void Start(Func<string> handleFightsRequest, Action<string, string> acceptFights)
	{
		if (_cts != null) return; // уже запущено

		_cts = new CancellationTokenSource();

		_udpTask = RunUdpDiscoveryResponderAsync(_cts.Token);
		_tcpTask = RunTcpServerAsync(handleFightsRequest, acceptFights, _cts.Token);
	}

	/// <summary>
	/// Остановить слушатели.
	/// </summary>
	public async Task StopAsync()
	{
		var cts = _cts;
		if (cts == null) return;

		_cts = null;
		cts.Cancel();

		// Важно: закрываем сокеты, чтобы прервать Receive/Accept
		try { _udpServer?.Close(); } catch { }
		try { _tcpListener?.Stop(); } catch { }

		try
		{
			await Task.WhenAll(_udpTask ?? Task.CompletedTask, _tcpTask ?? Task.CompletedTask);
		}
		catch (OperationCanceledException) { }
		finally
		{
			_udpServer?.Dispose();
			_udpServer = null;

			_tcpListener = null;

			cts.Dispose();
		}
	}

	public void Dispose()
	{
		_ = StopAsync();
	}

	// ---------- CLIENT API ----------

	public async Task<Dictionary<string, IPAddress>> GetAllHosts(int waitInSeconds, bool ignoreSefHost = true)
	{
		using var udp = new UdpClient();
		udp.EnableBroadcast = true;

		var probe = Encoding.UTF8.GetBytes("DISCOVER");

		foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
		{
			if (ni.OperationalStatus != OperationalStatus.Up)
				continue;

			var props = ni.GetIPProperties();

			foreach (var addr in props.UnicastAddresses)
			{
				if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
					continue;

				var mask = addr.IPv4Mask;
				if (mask == null)
					continue;

				var ip = addr.Address.GetAddressBytes();
				var maskBytes = mask.GetAddressBytes();

				var broadcast = new byte[4];
				for (int i = 0; i < 4; i++)
					broadcast[i] = (byte)(ip[i] | (~maskBytes[i]));

				var broadcastIp = new IPAddress(broadcast);

				await udp.SendAsync(probe, probe.Length, new IPEndPoint(broadcastIp, udpPort));
			}
		}

		var found = new Dictionary<string, IPAddress>();
		var stopAt = DateTime.UtcNow.AddSeconds(waitInSeconds);

		while (DateTime.UtcNow <= stopAt)
		{
			var waitTask = udp.ReceiveAsync();
			var done = await Task.WhenAny(waitTask, Task.Delay(200));

			if (done != waitTask)
				continue;

			var response = waitTask.Result;
			var name = Encoding.UTF8.GetString(response.Buffer);

			if (ignoreSefHost && name == ServerName)
			{
				continue;
			}
			found.TryAdd(name, response.RemoteEndPoint.Address);
		}

		return found;
	}

	public async Task ConnectAndRequestFights(string serverIp, CancellationToken cancellationToken)
	{
		using var client = new TcpClient();
		await client.ConnectAsync(serverIp, tcpPort, cancellationToken);
		await using var stream = client.GetStream();

		// R = запрос
		await stream.WriteAsync(Encoding.UTF8.GetBytes("R"), cancellationToken);
		// дальше сервер пришлёт "S"+len+payload — вы можете принять это в отдельном методе при необходимости
	}

	public async Task ConnectAndSendFights(string serverIp, string fights, CancellationToken cancellationToken)
	{
		using var client = new TcpClient();
		await client.ConnectAsync(serverIp, tcpPort, cancellationToken);
		await using var stream = client.GetStream();

		await SendFightsAsync(stream, fights, cancellationToken);
	}

	// ---------- SERVER LOOPS ----------

	private async Task RunUdpDiscoveryResponderAsync(CancellationToken ct)
	{
		_udpServer = new UdpClient(udpPort);

		try
		{
			while (!ct.IsCancellationRequested)
			{
				UdpReceiveResult req;
				try
				{
					req = await _udpServer.ReceiveAsync(); // отмена через Close()
				}
				catch (ObjectDisposedException) { break; }
				catch (SocketException) { if (ct.IsCancellationRequested) break; continue; }

				var text = Encoding.UTF8.GetString(req.Buffer);
				if (text == "DISCOVER")
				{
					var bytes = Encoding.UTF8.GetBytes(ServerName);
					await _udpServer.SendAsync(bytes, bytes.Length, req.RemoteEndPoint);
				}
			}
		}
		finally
		{
			_udpServer?.Dispose();
			_udpServer = null;
		}
	}

	private async Task RunTcpServerAsync(
		Func<string> handleFightsRequest,
		Action<string, string> acceptFights,
		CancellationToken ct)
	{
		_tcpListener = new TcpListener(IPAddress.Any, tcpPort);
		_tcpListener.Start();

		try
		{
			while (!ct.IsCancellationRequested)
			{
				TcpClient client;
				try
				{
					client = await _tcpListener.AcceptTcpClientAsync(ct);
				}
				catch (OperationCanceledException) { break; }
				catch (ObjectDisposedException) { break; }

				_ = HandleClientAsync(client, handleFightsRequest, acceptFights, ct);
			}
		}
		finally
		{
			try { _tcpListener.Stop(); } catch { }
			_tcpListener = null;
		}
	}

	private async Task HandleClientAsync(
		TcpClient client,
		Func<string> handleFightsRequest,
		Action<string, string> acceptFights,
		CancellationToken ct)
	{
		await using var stream = client.GetStream();

		while (!ct.IsCancellationRequested)
		{
			// читаем 1 байт команды гарантированно
			var command = await ReadExactlyStringAsync(stream, 1, ct);
			if (command.Length == 0) return; // disconnect

			var endpoint = (IPEndPoint)client.Client.RemoteEndPoint!;
			string ip = endpoint.Address.ToString();

			switch (command)
			{
				case "S": // клиент прислал данные
					var fightsMessage = await ReadFightsMessageAsync(stream, ct);
					acceptFights(fightsMessage, ip);
					break;

				case "R": // клиент запросил данные
					var fights = handleFightsRequest();
					await SendFightsAsync(stream, fights, ct);
					break;
			}
		}
	}

	// ---------- PROTOCOL HELPERS ----------

	private static async Task<string> ReadFightsMessageAsync(NetworkStream stream, CancellationToken ct)
	{
		var lenBytes = await ReadExactlyAsync(stream, 4, ct);
		var length = BitConverter.ToInt32(lenBytes, 0);
		var payload = await ReadExactlyAsync(stream, length, ct);
		return Encoding.UTF8.GetString(payload);
	}

	private static async Task SendFightsAsync(NetworkStream stream, string fights, CancellationToken ct)
	{
		// S = сервер отправляет данные
		await stream.WriteAsync(Encoding.UTF8.GetBytes("S"), ct);

		var payload = Encoding.UTF8.GetBytes(fights);
		var len = BitConverter.GetBytes(payload.Length);

		await stream.WriteAsync(len, ct);
		await stream.WriteAsync(payload, ct);
	}

	private static async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int size, CancellationToken ct)
	{
		var buffer = new byte[size];
		var offset = 0;

		while (offset < size)
		{
			var read = await stream.ReadAsync(buffer.AsMemory(offset, size - offset), ct);
			if (read == 0) return Array.Empty<byte>(); // disconnect
			offset += read;
		}

		return buffer;
	}

	private static async Task<string> ReadExactlyStringAsync(NetworkStream stream, int size, CancellationToken ct)
	{
		var bytes = await ReadExactlyAsync(stream, size, ct);
		if (bytes.Length == 0) return "";
		return Encoding.UTF8.GetString(bytes);
	}
}