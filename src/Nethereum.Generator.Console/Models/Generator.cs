﻿using System.Collections.Generic;
using System.IO;
using Nethereum.Generator.Console.Configuration;
using Nethereum.Generators;
using Nethereum.Generators.Core;

namespace Nethereum.Generator.Console.Models
{
    public class Generator
    {
        public CodeGenLanguage Language { get; set; }

        public string Namespace { get; set; }

        public string OutputFolder { get; set; }

        public List<ContractDefinition> Contracts { get; set; }

        public IEnumerable<ContractProjectGenerator> GetProjectGenerators()
        {
            foreach (var contract in Contracts)
            {
                yield return new ContractProjectGenerator(
                    contract.Abi,
                    contract.ContractName,
                    contract.Bytecode,
                    Namespace,
                    $"{contract.ContractName}.Service",
                    $"{contract.ContractName}.CQS",
                    $"{contract.ContractName}.DTO",
                    OutputFolder,
                    Path.DirectorySeparatorChar.ToString(),
                    Language
                );
            }
        }
    }
}
