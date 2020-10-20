using Glasswall.IcapServer.CloudProxyApp.AdaptationService;
using Glasswall.IcapServer.CloudProxyApp.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Glasswall.IcapServer.CloudProxyApp
{
    public class NativeProxyApplication
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger<NativeProxyApplication> _logger;
        private readonly CancellationTokenSource _processingCancellationTokenSource;
        private readonly TimeSpan _processingTimeoutDuration = TimeSpan.FromSeconds(60);

        private readonly IAdaptationServiceClient<AdaptationOutcomeProcessor> _adaptationServiceClient;

        readonly string OriginalStorePath = "/var/source";
        readonly string RebuiltStorePath = "/var/target";

        public NativeProxyApplication(IAdaptationServiceClient<AdaptationOutcomeProcessor> adaptationServiceClient, IAppConfiguration appConfiguration, ILogger<NativeProxyApplication> logger)
        {
            _adaptationServiceClient = adaptationServiceClient ?? throw new ArgumentNullException(nameof(adaptationServiceClient));
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processingCancellationTokenSource = new CancellationTokenSource(_processingTimeoutDuration);

        }

        public Task<int> RunAsync()
        {
            var fileId = Guid.NewGuid();
            try
            {
                var processingCancellationToken = _processingCancellationTokenSource.Token;

                var originalStoreFilePath = Path.Combine(OriginalStorePath, fileId.ToString());
                var rebuiltStoreFilePath = Path.Combine(RebuiltStorePath, fileId.ToString());

                _logger.LogInformation($"Updating 'Original' store for {fileId}");
                File.Copy(_appConfiguration.InputFilepath, originalStoreFilePath);

                _adaptationServiceClient.Connect();
                var outcome = _adaptationServiceClient.AdaptationRequest(fileId, originalStoreFilePath, rebuiltStoreFilePath, processingCancellationToken);
                _logger.LogInformation($"Returning '{outcome}' Outcome for {fileId}");

                if (outcome == ReturnOutcome.GW_REBUILT)
                {
                    _logger.LogInformation($"Copy from '{rebuiltStoreFilePath}' to {_appConfiguration.OutputFilepath}");
                    File.Copy(rebuiltStoreFilePath, _appConfiguration.OutputFilepath, overwrite: true);
                }

                return Task.FromResult((int)outcome);
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogError(oce, $"Error Processing Timeout 'input' {fileId} exceeded {_processingTimeoutDuration.TotalSeconds}s");
                return Task.FromResult((int)ReturnOutcome.GW_ERROR);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error Processing 'input' {fileId}");
                return Task.FromResult((int)ReturnOutcome.GW_ERROR);
            }
        }

    }
}
