using Glasswall.IcapServer.CloudProxyApp.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Glasswall.IcapServer.CloudProxyApp
{
    internal class CloudProxyApplication
    {
        private readonly IAppConfiguration _configuration;

        public CloudProxyApplication(IAppConfiguration configuration)
        {
            _configuration = configuration;
        }

        internal int Run()
        {
            if (!CheckConfigurationIsValid(_configuration))
            {
                return (int)ReturnOutcome.GW_ERROR;
            }

            return CopyFile(_configuration.InputFilepath, _configuration.OutputFilepath);
        }

        private int CopyFile(string sourceFilepath, string destinationFilepath)
        {
            try
            {
                var destinationFolder = System.IO.Path.GetDirectoryName(destinationFilepath);
                System.IO.Directory.CreateDirectory(destinationFolder);
                System.IO.File.Copy(sourceFilepath, destinationFilepath, true);
                return (int)ReturnOutcome.GW_REBUILT;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Processing 'input' {sourceFilepath}, {ex.Message}");
                return (int)ReturnOutcome.GW_ERROR;
            }
        }

        static bool CheckConfigurationIsValid(IAppConfiguration configuration)
        {
            var configurationErrors = new List<string>();

            if (string.IsNullOrEmpty(configuration.InputFilepath))
                configurationErrors.Add($"'InputFilepath' configuration is missing");

            if (string.IsNullOrEmpty(configuration.OutputFilepath))
                configurationErrors.Add($"'OutputFilepath' configuration is missing");

            if (configurationErrors.Any())
            {
                Console.WriteLine($"Error Processing Command {Environment.NewLine} \t{string.Join(',', configurationErrors)}");
            }

            return !configurationErrors.Any();
        }
    }
}