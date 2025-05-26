// filepath: d:\repos\codewith\codewith-philips\CodeWith-ChatWithYourData-MultiUser\src\react\src\models\IndexDoc.ts
export interface IIndexDoc {
    chunk_id: string;
    content: string;
    file_name: string;
    document_id: string;
    thread_id: string;
    contentVector?: number[]; // Using array for vector data
}