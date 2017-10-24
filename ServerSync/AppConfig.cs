using System;

namespace ServerSync
{
	/// <summary>
	/// Данные приложения, которые изменяются в процессе работы.
	/// </summary>
	public class AppConfig
	{
		/// <summary>
		/// Дата последнего запуска приложения.
		/// </summary>
		public DateTime LastRun { get; set; } = DateTime.MinValue;
	}
}
