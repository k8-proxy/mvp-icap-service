using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Glasswall.IcapServer.CloudProxyApp.AdaptationService
{
    public class AdaptationOutcomeProcessor : IResponseProcessor
    {
        private readonly ILogger<AdaptationOutcomeProcessor> _logger;

        static readonly Dictionary<AdaptationOutcome, ReturnOutcome> OutcomeMap = new Dictionary<AdaptationOutcome, ReturnOutcome>
        {
            { AdaptationOutcome.Unmodified, ReturnOutcome.GW_UNPROCESSED},
            { AdaptationOutcome.Replace, ReturnOutcome.GW_REBUILT},
            { AdaptationOutcome.Failed, ReturnOutcome.GW_FAILED }
        };

        public AdaptationOutcomeProcessor(ILogger<AdaptationOutcomeProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ReturnOutcome Process(IDictionary<string, object> headers, byte[] body)
        {
            try
            {
                if (body == null)
                {
                    _logger.LogError($"Returning body content missing");
                    return ReturnOutcome.GW_ERROR;
                }

                var bodyContent = Encoding.UTF8.GetString(body);
                dynamic outcomeResponse = JsonConvert.DeserializeObject<dynamic>(bodyContent);

                var fileIdString = Convert.ToString(outcomeResponse.FileId);
                var fileId = Guid.Empty;
                if (fileIdString == null || !Guid.TryParse(fileIdString, out fileId))
                {
                    _logger.LogError($"Returning outcome fileId");
                    return ReturnOutcome.GW_ERROR;
                }

                var outcomeString = Convert.ToString(outcomeResponse.FileOutcome);
                AdaptationOutcome outcome = (AdaptationOutcome)Enum.Parse(typeof(AdaptationOutcome), outcomeString, ignoreCase: true);
                if (!OutcomeMap.ContainsKey(outcome))
                {
                    _logger.LogError($"Returning outcome unmapped: {outcomeString}");
                    return ReturnOutcome.GW_ERROR;
                }

                return OutcomeMap[outcome];
            }
            catch (ArgumentException aex)
            {
                _logger.LogError($"Unrecognised enumeration processing adaptation outcome {aex.Message}");
                return ReturnOutcome.GW_ERROR;
            }
            catch (JsonReaderException jre)
            {
                _logger.LogError($"Poorly formated adaptation outcome : {jre.Message}");
                return ReturnOutcome.GW_ERROR;
            }
        }
    }
}
