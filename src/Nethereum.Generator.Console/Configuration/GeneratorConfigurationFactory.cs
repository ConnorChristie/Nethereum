﻿using Nethereum.Generators.Core;
using System;
using System.Collections.Generic;
using System.IO;
using Nethereum.Generator.Console.Models;
using Newtonsoft.Json;

namespace Nethereum.Generator.Console.Configuration
{
    /// <summary>
    /// Responsible from retrieving code generator configuration and setting default values
    /// </summary>
    public class GeneratorConfigurationFactory : IGeneratorConfigurationFactory
    {
        public Models.Generator FromAbi(string contractName, string abiFilePath, string binFilePath, string baseNamespace, string outputFolder)
        {
            var abi = GeneratorConfigurationUtils.GetFileContent(outputFolder, abiFilePath);

            if (string.IsNullOrEmpty(abi))
                throw new ArgumentException("Could not find abi file or abi content is empty");

            if (string.IsNullOrEmpty(binFilePath))
                binFilePath = abiFilePath.Replace(".abi", ".bin");

            var byteCode = GeneratorConfigurationUtils.GetFileContent(outputFolder, binFilePath);

            if (string.IsNullOrEmpty(contractName))
                contractName = Path.GetFileNameWithoutExtension(abiFilePath);

            return new Models.Generator
            {
                Namespace = baseNamespace,
                OutputFolder = outputFolder,
                Contracts = new List<ContractDefinition>
                {
                    new ContractDefinition(abi)
                    {
                        ContractName = contractName,
                        Bytecode = byteCode
                    }
                }
            };
        }

        public Models.Generator FromProject(string destinationProjectFolderOrFileName, string assemblyName)
        {
            (string projectFolder, string projectFilePath) =
                GeneratorConfigurationUtils.GetFullFileAndFolderPaths(destinationProjectFolderOrFileName);

            if (string.IsNullOrEmpty(projectFolder) ||
                !Directory.Exists(projectFolder))
                return null;

            var codeGenConfig = FromConfigFile(projectFolder, assemblyName);
            if (codeGenConfig != null)
                return codeGenConfig;

            if (string.IsNullOrEmpty(projectFilePath))
                projectFilePath = GeneratorConfigurationUtils.FindFirstProjectFile(projectFolder);

            if (string.IsNullOrEmpty(projectFilePath) ||
                !File.Exists(projectFilePath))
                return null;

            var language = GeneratorConfigurationUtils.DeriveCodeGenLanguage(projectFilePath);

            return FromAbiFilesInProject(projectFilePath, assemblyName, language);
        }

        public Models.Generator FromAbiFilesInProject(string projectFileName, string assemblyName, CodeGenLanguage language)
        {
            var projectFolder = Path.GetDirectoryName(projectFileName);
            var abiFiles = Directory.GetFiles(projectFolder, "*.abi", SearchOption.AllDirectories);
            var contracts = new List<ContractDefinition>(abiFiles.Length);

            foreach (var file in abiFiles)
            {
                var contractName = Path.GetFileNameWithoutExtension(file);
                var binFileName = Path.Combine(Path.GetDirectoryName(file), contractName + ".bin");
                var byteCode = File.Exists(binFileName) ? File.ReadAllText(binFileName) : string.Empty;

                contracts.Add(new ContractDefinition(File.ReadAllText(file))
                {
                    ContractName = contractName,
                    Bytecode = byteCode
                });
            }

            return new Models.Generator
            {
                Language = language,
                Contracts = contracts,
                OutputFolder = projectFolder,
                Namespace = GeneratorConfigurationUtils.CreateNamespaceFromAssemblyName(assemblyName)
            };
        }

        public Models.Generator FromCompiledContractDirectory(string directory, string outputFolder, string baseNamespace, CodeGenLanguage language)
        {
            var directoryName = Path.GetDirectoryName(directory);
            var compiledContracts = Directory.GetFiles(directoryName, "*.json", SearchOption.AllDirectories);
            var contracts = new List<ContractDefinition>(compiledContracts.Length);

            foreach (var file in compiledContracts)
            {
                var contract = JsonConvert.DeserializeObject<CompiledContract>(File.ReadAllText(file));
                contracts.Add(contract.GetContractConfiguration());
            }

            return new Models.Generator
            {
                Language = language,
                Contracts = contracts,
                OutputFolder = outputFolder,
                Namespace = baseNamespace
            };
        }

        public Models.Generator FromConfigFile(string destinationProjectFolder, string assemblyName)
        {
            var configFilePath = GeneratorConfigurationUtils.DeriveConfigFilePath(destinationProjectFolder);

            if (!File.Exists(configFilePath))
                return null;

            var defaultNamespace = GeneratorConfigurationUtils.CreateNamespaceFromAssemblyName(assemblyName);
            var configuration = ABICollectionConfiguration.FromJson(configFilePath, defaultNamespace);

            return configuration.GetGeneratorConfiguration();
        }
    }
}