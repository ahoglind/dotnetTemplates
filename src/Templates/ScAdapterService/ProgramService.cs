﻿using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using ServiceControl.TransportAdapter;

namespace ScAdapterService
{
    class ProgramService : ServiceBase
    {
        const string runAsServiceArg = "--run-as-service";

        ITransportAdapter adapter;

        static ILog logger;

        static ProgramService()
        {
            //TODO: optionally choose a custom logging library
            // https://docs.particular.net/nservicebus/logging/#custom-logging
            // LogManager.Use<TheLoggingFactory>();
            logger = LogManager.GetLogger<ProgramService>();
        }

        static void Main(string[] args)
        {
            using (var service = new ProgramService())
            {
                // pass argument at command line to run as a windows service
                if (args.Contains(runAsServiceArg))
                {
                    Run(service);
                    return;
                }

                Console.Title = "ScAdapterService";
                Console.CancelKeyPress += (sender, e) => { service.OnStop(); };
                service.OnStart(null);
                Console.WriteLine("\r\nPress enter key to stop program\r\n");
                Console.Read();
                service.OnStop();
            }
        }

        protected override void OnStart(string[] args)
        {
            AsyncOnStart().GetAwaiter().GetResult();
        }

        async Task AsyncOnStart()
        {
            try
            {
                var adapterConfig = new TransportAdapterConfig<LearningTransport, LearningTransport>("TransportAdapter.WindowsService");

                adapterConfig.CustomizeEndpointTransport(t =>
                {
                //TODO: Customize the endpoint-facing side of the adapter
                //Use exactly same settings as in regular endpoints
            });

                adapterConfig.CustomizeServiceControlTransport(t =>
                {
                //TODO: Customize the ServiceControl-facing side of the adapter
                //e.g. specify the same connection string as ServiceControl uses.
            });

                adapter = TransportAdapter.Create(adapterConfig);

                await adapter.Start().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Exit("Failed to start", exception);
            }
        }

        void Exit(string failedToStart, Exception exception)
        {
            logger.Fatal(failedToStart, exception);

            //TODO: When using an external logging framework it is important to flush any pending entries prior to calling FailFast
            // https://docs.particular.net/nservicebus/hosting/critical-errors#when-to-override-the-default-critical-error-action
            Environment.FailFast(failedToStart, exception);
        }

        protected override void OnStop()
        {
            adapter?.Stop().GetAwaiter().GetResult();
        }
    }
}