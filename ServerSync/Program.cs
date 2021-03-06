﻿using System;
using System.IO;
using System.Threading.Tasks;
using Lers;

namespace ServerSync
{
	class Program
	{
		/// <summary>
		/// Объект для протоколирования.
		/// </summary>
		private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Точка входа в приложение.
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			try
			{
				MainAsync().Wait();
			}
			catch (AggregateException exc)
			{
				logger.Error(exc.InnerException, $"Ошибка сихронизации серверов.");
			}
		}

		/// <summary>
		/// Асинхронная точка входа.
		/// </summary>
		/// <returns></returns>
		static async Task MainAsync()
		{
			logger.Info("==================================\r\n===== Запущена синхронизация данных между серверами ЛЭРС УЧЁТ...\r\n==================================");

			string appConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LERS");

			// Убедимся что папка, куда будут сохранены конфигурационные файлы, существует. Если её нет, создадим.

			if (!Directory.Exists(appConfigDir))
			{
				logger.Debug($"Создаётся папка {appConfigDir}");

				Directory.CreateDirectory(appConfigDir);
			}

			string appConfigFileName = Path.Combine(appConfigDir, "ServerSync.Data.json");

			logger.Debug($"Загрузка конфигурации...");

			var config = LoadConfig("config.json");

			logger.Debug($"Загрузка последних параметров из файла {appConfigFileName}");

			var appConfig = LoadAppConfig(appConfigFileName);

			var sourceServer = ConnectServer(config.SourceServer);
			var targetServer = ConnectServer(config.TargetServer);

			// Получаем данные экспорта по списку точек учёта. TODO: Нам нужны данные с момента последнего запуска приложения.

			var end  = DateTime.Now;
			var start = appConfig.LastRun;

			if (start == DateTime.MinValue)
			{
				// При первом запуске синхронизируем всё на 1 неделю назад.
				start = DateTime.Today.AddDays(-7);
			}

			// Экспортируем данные по указанным точкам учёта.

			logger.Info($"Экспорт данных по {config.MeasurePointNumbers.Length} точек учёта за интервал {start} - {end}");

			var exported = await sourceServer.Data.Export(config.MeasurePointNumbers, start, end,
				  Lers.Data.DeviceDataType.Day
				| Lers.Data.DeviceDataType.Hour
				| Lers.Data.DeviceDataType.Current
				| Lers.Data.DeviceDataType.Totals);


			// Импортируем данные на целевой сервер.

			logger.Info($"Импорт данных. Размер файла для импорта {exported.Length} байт. Таймаут {config.ImportTimeout} сек.");

			var result = await targetServer.Data.Import(exported, false, config.ImportTimeout);

			foreach (var mpResult in result.MeasurePointResultList)
			{
				if (mpResult.IsError)
				{
					logger.Warn($"Ошибка импорта данных по точке учёта {mpResult.MeasurePointTitle}. {mpResult.ErrorMessage}");
				}
				else
				{
					logger.Info($"Успешно импортированы данные по точке учёта {mpResult.MeasurePointTitle} ({mpResult.IntervalList?.Length ?? 0} интервалов.)");
				}
			}

			logger.Info("Импорт данных успешно завершён");

			appConfig.LastRun = DateTime.Today;

			SaveAppConfig(appConfigFileName, appConfig);
		}

		/// <summary>
		/// Устанавливает подключение к серверу.
		/// </summary>
		/// <param name="serverConfig"></param>
		/// <returns></returns>
		private static LersServer ConnectServer(ServerConfig serverConfig)
		{
			logger.Info($"Подключение к серверу {serverConfig.Address}:{serverConfig.Port}");

			var server = new LersServer("Утилита синхронизации данных по точкам учёта.");

			// Игнорируем разницу в версиях.
			server.VersionMismatch += (sender, e) => e.Ignore = true;

			server.Connect(serverConfig.Address, serverConfig.Port, new Lers.Networking.BasicAuthenticationInfo(
				serverConfig.Login, Lers.Networking.SecureStringHelper.ConvertToSecureString(serverConfig.Password)));

			return server;
		}

		private static Config LoadConfig(string configFileName)
		{
			string text = File.ReadAllText(configFileName);

			var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(text);

			if (config.SourceServer == null)
			{
				throw new ArgumentException("Не заданы параметры исходного сервера.");
			}

			if (config.TargetServer == null)
			{
				throw new ArgumentException("Не заданы параметры целевого сервера.");
			}

			if (config.MeasurePointNumbers == null || config.MeasurePointNumbers.Length == 0)
			{
				throw new ArgumentException("Не заданы номера точек учёта для синхронизации");
			}

			return config;
		}

		private static AppConfig LoadAppConfig(string configFileName)
		{
			AppConfig config = null;

			if (!File.Exists(configFileName))
			{
				config = new AppConfig();
			}
			else
			{
				try
				{
					string text = File.ReadAllText(configFileName);

					config = Newtonsoft.Json.JsonConvert.DeserializeObject<AppConfig>(text);
				}
				catch
				{
					config = new AppConfig();
				}
			}

			return config;
		}

		private static void SaveAppConfig(string configFileName, AppConfig config)
		{
			var text = Newtonsoft.Json.JsonConvert.SerializeObject(config);

			File.WriteAllText(configFileName, text);
		}
	}
}
