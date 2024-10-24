import { Subtitle2, makeStyles, tokens } from '@fluentui/react-components';
import { DocumentGrid } from './DocumentGrid';
import { DocumentUploader } from './DocumentUploader';
import { useChatDocuments } from '../../hooks/useChatDocuments';
import { ListSkeleton } from '../Loading/ListSkeleton';

const useClasses = makeStyles({
    container: {
        boxSizing: 'border-box',
        display: 'flex',
        flexDirection: 'column',
        width: '70%',
        margin: 'auto',
        backgroundColor: tokens.colorNeutralBackground2Hover,
        borderRadius: tokens.borderRadiusMedium,
        padding: tokens.spacingVerticalM,
        boxShadow: tokens.shadow2,
        marginTop: tokens.spacingVerticalL,
    },
    header: {
        flexGrow: 1,
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
    },
    title: {
        fontSize: tokens.fontSizeBase300,
        color: tokens.colorNeutralForeground1,
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        width: '200px',
        textOverflow: 'ellipsis',
    },
    selected: {
        backgroundColor: tokens.colorNeutralBackground2Pressed

    }
});


type documentViewerProps = { 
    chatId: string | undefined;
}

export function DocumentViewer({ chatId }: documentViewerProps) {

    const { documents, addDocuments, deleteDocument, documentsPending } = useChatDocuments(chatId);
    const classes = useClasses();

    return (
        <div className={classes.container}>
            <div className={classes.header}>
                <Subtitle2>Documents</Subtitle2>
                <DocumentUploader uploadDocuments={addDocuments} chatId={chatId} />
            </div>
            {documentsPending ? (
                <ListSkeleton/>
            ) : (
                <DocumentGrid documents={documents} deleteDocument={deleteDocument}/>
            ) }
            
        </div>
    );
};