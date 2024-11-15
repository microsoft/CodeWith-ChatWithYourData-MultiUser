import { Citation, IChatMessage } from "../../models/ChatMessage";


type ParsedAnswer = {
    citations: Citation[];
    markdownFormatText: string;
};

let filteredCitations = [] as Citation[];

// Define a function to check if a citation with the same Chunk_Id already exists in filteredCitations
const isDuplicate = (citation: Citation,citationIndex:string) => {
    return filteredCitations.some((c) => c.chunk_id === citation.chunk_id) && !filteredCitations.find((c) => c.id === citationIndex) ;
};

export function parseAnswer(answer: IChatMessage): ParsedAnswer {
    let answerText = answer.content;
    const citationLinks = answerText.match(/\[(doc\d\d?\d?)]/g);

    const lengthDocN = "[doc".length;

    filteredCitations = [] as Citation[];
    let citationReindex = 0;
    citationLinks?.forEach(link => {
        // Replacing the links/citations with number
        let citationIndex = link.slice(lengthDocN, link.length - 1);
        let citation = cloneDeep(answer.citations![Number(citationIndex) - 1]) as Citation;
        if (!isDuplicate(citation, citationIndex) && citation !== undefined) {
            answerText = answerText.split(link).join(` ^${++citationReindex}^ `);
            citation.id = citationIndex; // original doc index to de-dupe
            citation.reindex_id = citationReindex.toString(); // reindex from 1 for display
            filteredCitations.push(citation);
          } else {
            // Replacing duplicate citation with original index
            let matchingCitation = filteredCitations.find((ct) => citation.chunk_id == ct.chunk_id);
            if (matchingCitation) {
              answerText = answerText.split(link).join(` ^${matchingCitation.reindex_id}^ `);
            }
          }
    })


    return {

        citations: filteredCitations,
        markdownFormatText: answerText
    };
}
function cloneDeep(citation: Citation): Citation {
    return JSON.parse(JSON.stringify(citation));
}

