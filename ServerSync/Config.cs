namespace ServerSync
{
	/// <summary>
	/// Конфигурация программы.
	/// </summary>
	public class Config
	{
		/// <summary>
		/// Параметры исходного сервера.
		/// </summary>
		public ServerConfig SourceServer { get; set; }

		/// <summary>
		/// Параметры целевого сервера.
		/// </summary>
		public ServerConfig TargetServer { get; set; }

		/// <summary>
		/// Список номеров точек учёта, по которым должна проводиться синхронизация.
		/// </summary>
		public int[] MeasurePointNumbers { get; set; }
	}


	/// <summary>
	/// Конфигурация сервера.
	/// </summary>
	public class ServerConfig
	{
		/// <summary>
		/// Адрес сервера.
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// Порт сервера.
		/// </summary>
		public ushort Port { get; set; } = 10000;

		/// <summary>
		/// Логин на сервере.
		/// </summary>
		public string Login { get; set; }

		/// <summary>
		/// Пароль на сервере.
		/// </summary>
		public string Password { get; set; }
	}
}
