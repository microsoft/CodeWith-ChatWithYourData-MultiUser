using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Implementations.SemanticKernel.Tools
{
    public class IncoseTemplateTool
    {
        [KernelFunction("get_incose_template")]
        [Description("Retrieves a template for an INCOSE validation")]

        public string Template()
        {
            Console.WriteLine("--- Template Tool Invoked ---");

            return $$"""
            [
                {
                  "ID": "",
                  "Requirement": "",
                  "Description":"",              
                  "Remarks": "",
                  "INCOSE Assessment":"",
                  "INCOSE Notes":""
                }
            ]
            """;
        }
    }
}
