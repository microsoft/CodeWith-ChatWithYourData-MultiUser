using DocumentFormat.OpenXml.Wordprocessing;
using Infrastructure.Implementations.AISearch;
using Infrastructure.Implementations.SemanticKernel.Tools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Implementations.SemanticKernel.Agents
{
    internal class ValidationAgent
    {
        public ChatCompletionAgent CreateAgent(Kernel kernel, string agentName, string guidelineName)
        {
            // Clone kernel instance to allow for agent specific plug-in definition
            Kernel agentKernel = kernel.Clone();

            string instruction = $"""
                    You are the best validator there is, you have a laser sharp focus to determine if a requirement is ok not.
                    You are tasked to perform a review of a set of requirements against the {guidelineName} guidelines.
                    Go over each individual requirement and assess and validate if the requirement satisfies the {guidelineName} guidelines
                    The conclusion of each validation per requirement is needed, together with suggestions on how to improve.
                    IMPORTANT: DO NOT REWRITE THE REQUIREMENTS, ONLY ASSESS AND VALIDATE THEM AND BE VERY ACCURATE.

                   The following structure should be used to build a list of all the assessed requirements:
                   ### IncoseTemplate
                    -- Id
                    -- Requirement
                    -- Description
                    -- Remarks
                    -- Incose status
                    -- Incose assessment
                    -- Incose notes
                    ###

                    For the output, format the list of requirements into a markdown table. Provide a legend on how to read/interpret the data.
                """;

            return
                new ChatCompletionAgent()
                {
                    Name = agentName,
                    Description = "Agent responsible for doing the validation for certain rulesets (like Incose) against a set of requirements.",
                    Instructions = instruction,
                    Kernel = agentKernel,
                    Arguments = new KernelArguments(
                        new OpenAIPromptExecutionSettings()
                        {
                            ServiceId = "reasoning",
                        })
                };
        }
    }
}
