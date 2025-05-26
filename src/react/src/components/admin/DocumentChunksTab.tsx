import { Button, Card, CardHeader, Field, Input, Spinner, makeStyles, tokens } from "@fluentui/react-components";
import { ChunkViewer } from "./ChunkViewer";
import { IIndexDoc } from "../../models/IndexDoc";
import { useLayoutStyles } from "../../styles/sharedStyles";

const useStyles = makeStyles({
    container: {
        padding: tokens.spacingVerticalM,
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
    },
    searchSection: {
        display: 'flex',
        gap: '10px',
        alignItems: 'flex-end',
        marginBottom: tokens.spacingVerticalM
    },
    chunksContainer: {
        flex: '1 1 auto',
        height: 'calc(100vh - 250px)',
        display: 'flex',
        flexDirection: 'column',
    }
});

export interface DocumentChunksTabProps {
    documentId: string;
    setDocumentId: (id: string) => void;
    documentChunks?: IIndexDoc[];
    isLoading: boolean;
    onSearch: (id: string) => void;
}

export function DocumentChunksTab({ 
    documentId, 
    setDocumentId, 
    documentChunks, 
    isLoading, 
    onSearch 
}: DocumentChunksTabProps) {
    const styles = useStyles();

    return (
        <div className={styles.container}>
            <Card>
                <CardHeader header="Search Document Chunks" />
                <div className={styles.searchSection}>
                    <Field label="Document ID">
                        <Input
                            value={documentId}
                            onChange={(_e, data) => setDocumentId(data.value)}
                            placeholder="Enter document ID"
                        />
                    </Field>
                    <Button onClick={() => onSearch(documentId)}>Search</Button>
                </div>
                
                {isLoading && <Spinner />}
                
                <div className={styles.chunksContainer}>
                    <ChunkViewer chunks={documentChunks} />
                </div>
            </Card>
        </div>
    );
}