using Glasswall.IcapServer.CloudProxyApp.AdaptationService;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace Glasswall.IcapServer.CloudProxyApp.Tests.AdaptationService
{
    public class AdaptationOutcomeProcessorTests
    {
        [Test]
        public void Replace_Return_Rebuilt_Outcome()
        {

            // Arrange
            var processor = new AdaptationOutcomeProcessor(Mock.Of<ILogger<AdaptationOutcomeProcessor>>());
            var headers = Mock.Of<IDictionary<string, object>>();
            var body = "{ \"FileId\":\"737ba1cc-492c-4292-9a2c-fc7bfc722dc6\",\"FileOutcome\":\"replace\",\"OutcomeHeader\":null,\"OutcomeMessage\":null}";

            // Act
            var result = processor.Process(headers, Encoding.UTF8.GetBytes(body));

            // Assert
            Assert.That(result, Is.EqualTo(ReturnOutcome.GW_REBUILT), "expected the outcome to be 'rebuilt'");
        }

        [Test]
        public void Unmodified_Return_Unprocessed_Outcome()
        {
            // Arrange
            var processor = new AdaptationOutcomeProcessor(Mock.Of<ILogger<AdaptationOutcomeProcessor>>());
            var headers = Mock.Of<IDictionary<string, object>>();
            var body = "{ \"FileId\":\"737ba1cc-492c-4292-9a2c-fc7bfc722dc6\",\"FileOutcome\":\"unmodified\",\"OutcomeHeader\":null,\"OutcomeMessage\":null}";

            // Act
            var result = processor.Process(headers, Encoding.UTF8.GetBytes(body));

            // Assert
            Assert.That(result, Is.EqualTo(ReturnOutcome.GW_UNPROCESSED), "expected the outcome to be 'unprocessed'");
        }

        [Test]
        public void Failed_Return_Failed_Outcome()
        {
            // Arrange
            var processor = new AdaptationOutcomeProcessor(Mock.Of<ILogger<AdaptationOutcomeProcessor>>());
            var headers = Mock.Of<IDictionary<string, object>>();
            var body = "{ \"FileId\":\"737ba1cc-492c-4292-9a2c-fc7bfc722dc6\",\"FileOutcome\":\"failed\",\"OutcomeHeader\":null,\"OutcomeMessage\":null}";

            // Act
            var result = processor.Process(headers, Encoding.UTF8.GetBytes(body));

            // Assert
            Assert.That(result, Is.EqualTo(ReturnOutcome.GW_FAILED), "expected the outcome to be 'failed'");
        }

        [Test]
        public void Incorrect_Message_Error_Outcome()
        {
            // Arrange
            var processor = new AdaptationOutcomeProcessor(Mock.Of<ILogger<AdaptationOutcomeProcessor>>());
            var headers = Mock.Of<IDictionary<string, object>>();
            var body = "-- Incorrect Message --";

            // Act
            var result = processor.Process(headers, Encoding.UTF8.GetBytes(body));

            // Assert
            Assert.That(result, Is.EqualTo(ReturnOutcome.GW_ERROR), "expected the outcome to be 'error'");
        }

        [Test]
        public void Missing_Body_Error_Outcome()
        {
            // Arrange
            var processor = new AdaptationOutcomeProcessor(Mock.Of<ILogger<AdaptationOutcomeProcessor>>());
            var headers = Mock.Of<IDictionary<string, object>>();

            // Act
            var result = processor.Process(headers, null);

            // Assert
            Assert.That(result, Is.EqualTo(ReturnOutcome.GW_ERROR), "expected the outcome to be 'error'");
        }
    }
}
