using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Glasswall.IcapServer.CloudProxyApp.AdaptationService
{
    public class AdaptationOutcomeProcessor : IResponseProcessor
    {
        private readonly ILogger<AdaptationOutcomeProcessor> _logger;

        public AdaptationOutcomeProcessor(ILogger<AdaptationOutcomeProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public ReturnOutcome Process(IDictionary<string, object> headers, byte[] body)
        {

            var bodyContent = Encoding.UTF8.GetString(body);
            _logger.LogInformation($"Returning body content : {bodyContent}");

            return ReturnOutcome.GW_UNPROCESSED;
        }
    }
}
