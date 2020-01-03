// Copyright (c) 2017 Leacme (http://leac.me). View LICENSE.md for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Leacme.Lib.WebServerPrime {

	public class Library {

		public IWebHost CurrentServer { get; set; }
		public IObservable<LogEvent> ServerLogObservable { get; set; }

		private CancellationToken currentServerCancellationToken = new CancellationToken();

		public Library() {
		}

		/// <summary>
		/// Starts the portable web server.
		/// /// </summary>
		/// <param name="port">The port to run the server on.</param>
		/// <param name="rootPath">The directory which the server will serve.</param>
		public void StartServer(int port = 80, string rootPath = null) {
			if (string.IsNullOrWhiteSpace(rootPath)) {
				rootPath = Directory.GetCurrentDirectory();
			}
			if (CurrentServer == null) {
				var fso = new FileServerOptions() { EnableDirectoryBrowsing = true };
				fso.StaticFileOptions.ServeUnknownFileTypes = true;
				CurrentServer = WebHost.CreateDefaultBuilder().
									ConfigureAppConfiguration((z, zz) => { })
										.ConfigureServices((z, zz) => { })
										.ConfigureKestrel((z, zz) => { zz.Configure().Options.ListenAnyIP(port); })
										.ConfigureLogging((z, zz) => {
											zz.ClearProviders();
											zz.AddSerilog(new LoggerConfiguration().WriteTo.Observers(zzz => { ServerLogObservable = zzz; }).CreateLogger(), true);
										})
										.Configure(z => { z.UseStaticFiles(); z.UseFileServer(fso); })
										.UseWebRoot(rootPath)
										.Build();
				Task.Run(async () => await CurrentServer.StartAsync(currentServerCancellationToken));
			}
		}

		/// <summary>
		/// Returns a list of IP addresses for the local machine on which the server can run.
		/// </summary>
		/// <returns></returns>
		public ISet<IPAddress> GetEnabledLocalInterfaces() {
			HashSet<IPAddress> ips = new HashSet<IPAddress>();
			foreach (var intf in NetworkInterface.GetAllNetworkInterfaces()) {
				if (intf.OperationalStatus.Equals(OperationalStatus.Up) && intf.SupportsMulticast &&
					(intf.GetIPProperties().GetIPv4Properties()?.Index.Equals(NetworkInterface.LoopbackInterfaceIndex) == false ||
					intf.GetIPProperties().GetIPv6Properties()?.Index.Equals(NetworkInterface.LoopbackInterfaceIndex) == false)) {
					foreach (var unicastIP in intf.GetIPProperties().UnicastAddresses) {
						if (unicastIP.Address.AddressFamily.Equals(AddressFamily.InterNetwork) || unicastIP.Address.AddressFamily.Equals(AddressFamily.InterNetworkV6)) {
							ips.Add(unicastIP.Address);
						}
					}
				}
			}
			return ips;
		}

		/// <summary>
		/// Stops the running portable web server.
		/// /// </summary>
		public void StopServer() {
			if (CurrentServer != null) {
				CurrentServer.StopAsync(currentServerCancellationToken);
				CurrentServer = null;
			}
		}
	}
}