using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Prompts
{
    public static class GPT4Prompts
    {
        public static string RAGSystemPrompt =
            @"Assistant that helps to understand documents they have uploaded, you are the expert in extracting the relevant information of each document.
            Answer ONLY with the facts listed in the list of sources below. 
            If there isn't enough information below, say you don't know. Do not generate answers that don't use the sources below. If asking a clarifying question to the user would help, ask the question.
            If the question is not in English, answer in the language used in the question.
            Each source has a name followed by colon and the actual information, always include the source name for each fact you use in the response. Use square brackets to reference the source, for example [info1.txt]. 
            Don't combine sources, list each source separately, for example [info1.txt][info2.pdf].
            Please use markdown to format the response
            ";

        public static string RewritePrompt =
                @"Below is a history of the conversation so far, and a new question asked by the user that needs to be answered by searching in Azure AI Search index.
                Generate a search query based on the conversation and the new question.
                Do not include cited source filenames and document names e.g. info.txt or doc.pdf in the search query terms.
                Do not include any text inside [] or <<>> in the search query terms.
                Do not include any special characters like '+'.
                If the question is not in English, translate the question to English before generating the search query.
                If you cannot generate a search query, return just the orignal question.";


        public static string DraftQuestionsFromDocument = $@"
            Using these documents, try to draft three questions that can help the user to understand the content of the documents.
            It could be a summary, a question about a specific part of the document, or a question about the document as a whole.
            
            # Format of the response
            Return the follow-up question as an array of strings
            e.g.
            [
                ""What is the deductible?"",
                ""What is the co-pay?"",
                ""What is the out-of-pocket maximum?""
            ]";

        public static string FollowUpPrompt = $@"
                        Generate three short, concise but relevant follow-up question based on the answer you just generated.
                        
                        # Format of the response
                        Return the follow-up question as an array of strings
                        e.g.
                        [
                            ""What is the deductible?"",
                            ""What is the co-pay?"",
                            ""What is the out-of-pocket maximum?""
                        ]
                    ";
    }
}
