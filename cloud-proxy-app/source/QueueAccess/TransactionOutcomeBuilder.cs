using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;

namespace Glasswall.IcapServer.CloudProxyApp.QueueAccess
{
    public static class TransactionOutcomeBuilder
    {
        private static readonly Dictionary<string, ReturnOutcome> OutcomeMap = new Dictionary<string, ReturnOutcome>
        {
            ["Unknown"] = ReturnOutcome.GW_ERROR,
            ["Rebuilt"] = ReturnOutcome.GW_REBUILT,
            ["Unmanaged"] = ReturnOutcome.GW_UNPROCESSED,
            ["Failed"] = ReturnOutcome.GW_FAILED,
            ["Error"] = ReturnOutcome.GW_ERROR
        };

        public static TransactionOutcomeMessage Build(Message message)
        {
            if (message.Label != message.Label)
                throw new InvalidMessageException(message.Label, message.Label);

            var outcomeMessage = new TransactionOutcomeMessage
            {
                FileId = GetFileId(message),
                FileOutcome = GetFileOutcome(message),
                FileRebuildSas = GetFileRebuildSas(message)
            };

            return outcomeMessage;
        }

        private static string GetFileRebuildSas(Message message)
        {
            const string FileRebuildSasKey = "file-rebuild-sas";
            var value = message.UserProperties[FileRebuildSasKey] as string;

            if (string.IsNullOrEmpty(value))
                throw new InvalidMessageException($"{message.Label}: Missing content for '{FileRebuildSasKey}'");

            return value;
        }

        private static ReturnOutcome GetFileOutcome(Message message)
        {
            const string FileOutcomeKey = "file-outcome";
            var value = message.UserProperties[FileOutcomeKey] as string;
            if (string.IsNullOrEmpty(value))
                throw new InvalidMessageException($"{message.Label}: Missing content for '{FileOutcomeKey}'");

            return OutcomeMap[value];
        }

        private static Guid GetFileId(Message message)
        {
            const string FileIdKey = "file-id";
            var value = message.UserProperties[FileIdKey] as string;

            if (string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var fileId))
                throw new InvalidMessageException($"{message.Label}: Missing content for '{FileIdKey}'");

            return fileId;
        }
    }
}
