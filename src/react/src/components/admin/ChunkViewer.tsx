import { Card, makeStyles, tokens, Text, Badge } from "@fluentui/react-components";
import { IIndexDoc } from "../../models/IndexDoc";

const useStyles = makeStyles({
    container: {
        display: 'flex',
        flexDirection: 'column',
        gap: '16px', 
        height:'100vh',
        padding: '16px'
    },
    chunkCard: {
        padding: tokens.spacingVerticalM,
        marginBottom: tokens.spacingVerticalM,
        border: `1px solid ${tokens.colorNeutralStroke1}`,
        height:'auto',
        minHeight: '100vh',
        overflowY: 'auto',
    },
    header: {
        display: 'flex',
        justifyContent: 'space-between',
        marginBottom: tokens.spacingVerticalS,
        alignItems: 'center',
      
    },
    content: {
        whiteSpace: 'pre-wrap',
        fontFamily: 'monospace',
        fontSize: tokens.fontSizeBase300,
        color: tokens.colorNeutralForeground1,
        overflowY: 'auto',
        minHeight: '900px',
    },
    metadata: {
        display: 'flex',
        justifyContent: 'space-between',
        marginTop: tokens.spacingVerticalS,
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground3
    },
    badge: {
        marginLeft: tokens.spacingHorizontalS
    },
    chunkId: {
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground3,
        wordBreak: 'break-all'
    }
});

export interface ChunkViewerProps {
    chunks?: IIndexDoc[];
}

export function ChunkViewer({ chunks }: ChunkViewerProps) {
    const styles = useStyles();

    if (!chunks || chunks.length === 0) {
        return <Text>No chunks to display. Search for a document first.</Text>;
    }

    return (
        <><Text weight="semibold">
            {chunks[0].file_name}
        </Text><div className={styles.container}>
                {chunks.map((chunk) => (
                    <Card key={chunk.chunk_id} className={styles.chunkCard}>
                        <div className={styles.metadata}>
                            <div className={styles.chunkId}>
                                Chunk ID: {chunk.chunk_id}
                            </div>
                        </div>
                        <div className={styles.content}>
                            {chunk.content}
                        </div>
                    </Card>
                ))}
            </div></>
    );
}