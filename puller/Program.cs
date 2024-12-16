using common.abstractions;
using common.configs;
using common.publisher;
using Microsoft.AspNetCore.Connections;
using puller;
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

			services.AddTransient<IPublisher, ToQueuePublisher>();
			services.AddTransient<IFileProcessorHandler, FileProcessorPullHandler>();
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

			services.AddHostedService<FileDownloadService>();
		});
