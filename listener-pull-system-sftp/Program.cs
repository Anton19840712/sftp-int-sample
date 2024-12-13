using common;
using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using IConnectionFactory = RabbitMQ.Client.IConnectionFactory;

CreateHostBuilder(args).Build().Run();
IHostBuilder CreateHostBuilder(string[] args) =>
	Host.CreateDefaultBuilder(args)
		.ConfigureServices((hostContext, services) =>
		{
			var sftpConfig = new SftpConfig
			{
				Host = AppSettings.Host,
				Port = AppSettings.Port,
				UserName = AppSettings.UserName,
				Password = AppSettings.Password,
				Source = AppSettings.Source
			};
			// регистрация сервисов
			services.AddSingleton(sftpConfig);

			services.AddSingleton<IConnectionFactory>(sp =>
			{
				var connectionFactory = new ConnectionFactory
				{
					HostName = "localhost",
					UserName = "service",
					Password = "A1qwert"
				};
				return connectionFactory;
			});

			services.AddSingleton<IHostedService, ListenerPullSystemService>();
		});
