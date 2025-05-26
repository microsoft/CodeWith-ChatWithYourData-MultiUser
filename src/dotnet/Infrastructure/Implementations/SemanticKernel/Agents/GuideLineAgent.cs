using Azure.Search.Documents.Indexes;
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
    public class GuideLineAgent
    {
        public ChatCompletionAgent CreateAgent(Kernel kernel, string agentName, string guidelineName)
        {
            // Clone kernel instance to allow for agent specific plug-in definition
            Kernel agentKernel = kernel.Clone();

            if (!agentKernel.Plugins.Contains($"get_{guidelineName}_rules"))
            {
                agentKernel.Plugins.AddFromType<IncoseGuideLinesTool>($"get_{guidelineName}_rules");
            }

            string instruction = $"""
                1.	Fetch {guidelineName} Rules:
                •	Use the get_{guidelineName}_rules function to retrieve the {guidelineName} ruleset for requirements validation.
                """;

            return
                new ChatCompletionAgent()
                {
                    Name = agentName,
                    Instructions = instruction,
                    Description = "Agent responsible for fetching guidelines to do the validation.",
                    Kernel = agentKernel,

                    Arguments = new KernelArguments(
                        new OpenAIPromptExecutionSettings()
                        {
                            //ResponseFormat = typeof(GuideLines),
                            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(),
                            ServiceId = "small",
                        })
                };
        }
    }
}

   
