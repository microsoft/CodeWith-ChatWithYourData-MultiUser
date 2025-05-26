import { Button, Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow, makeStyles, tokens, Dialog, DialogSurface, DialogBody, DialogContent, Spinner } from '@fluentui/react-components';
import { Delete12Regular, DocumentText24Regular, ArrowDownload24Regular, DocumentRegular } from '@fluentui/react-icons/fonts';
import { IDocument } from '../../models/Document';
import { useState } from 'react';
import { ChunkViewer } from '../admin/ChunkViewer';
import { DocumentService } from '../../services/DocumentService';
import { useAuth } from '../../hooks/useAuth';
import { IIndexDoc } from '../../models/IndexDoc';
import ReactMarkdown from 'react-markdown';

const useClasses = makeStyles({
    container: {
        display: 'flex',
        marginTop: tokens.spacingVerticalM,
        width: '100%',
        overflowX: 'auto'
    },
    deleteColumn: {
        display: 'flex',
        gap: tokens.spacingHorizontalS,
        justifyContent: 'flex-end'
    },
    headerText: {
        fontWeight: tokens.fontWeightSemibold,
    },
    statusAvailable: {
        color: tokens.colorPaletteGreenForeground1
    },
    statusPending: {
        color: tokens.colorPaletteYellowForeground1
    },
    dialogSurface: {
        maxWidth: '90vw !important',
        width: '90vw !important',
        height: '80vh !important'
    },
    dialogContent: {
        height: 'calc(80vh - 80px)',
        overflowY: 'auto'
    },
    loadingContainer: {
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100%'
    },
    markdownContent: {
        padding: tokens.spacingHorizontalM
    }
});

// Enhanced columns with more fields from IDocument
const columns = [
    { columnKey: "documentName", label: "Document Name" },
    { columnKey: "fileSize", label: "Size" },
    { columnKey: "uploadDate", label: "Upload Date" },
    { columnKey: "status", label: "Status" },
    { columnKey: "extractStatus", label: "Extract Status" },
    { columnKey: "actions", label: "" }
];

type documentGridProps = { 
      documents?: IDocument[];
      deleteDocument: ({chatId, documentId}:{ chatId: string; documentId: string; }) => Promise<boolean>;
      getDocumentChunks: (threadId: string, documentId: string) => Promise<IIndexDoc[]>;
      getDocumentExtract: (threadId: string, documentId: string) => Promise<string>;
      extractDocument: (threadId: string, documentId: string) => Promise<boolean>;
}

export function DocumentGrid({ documents, deleteDocument, getDocumentChunks, getDocumentExtract, extractDocument } : documentGridProps) {
    const classes = useClasses();
    const [showChunks, setShowChunks] = useState(false);
    const [showExtract, setShowExtract] = useState(false);
    const [selectedDocument, setSelectedDocument] = useState<IDocument | null>(null);
    const [documentChunks, setDocumentChunks] = useState<IIndexDoc[]>([]);
    const [extractContent, setExtractContent] = useState<string>('');
    const [loading, setLoading] = useState(false);    const handleDelete = async (chatId: string, documentId: string) => {
        await deleteDocument({chatId: chatId, documentId: documentId});
    }

    const handleExtract = async (document: IDocument) => {
        try {
            await extractDocument(document.threadId, document.id);
            // No need to refresh, as the document list will be refreshed via the query invalidation
        } catch (error) {
            console.error('Failed to extract document:', error);
        }
    };const handleViewChunks = async (document: IDocument) => {
        setSelectedDocument(document);
        setLoading(true);
        setShowChunks(true);
        
        try {
            const chunks = await getDocumentChunks(document.threadId, document.id);
            setDocumentChunks(chunks);
        } catch (error) {
            console.error('Failed to fetch document chunks:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleViewExtract = async (document: IDocument) => {
        setSelectedDocument(document);
        setLoading(true);
        setShowExtract(true);
        
        try {
            const extractText = await getDocumentExtract(document.threadId, document.id);
            setExtractContent(extractText);
        } catch (error) {
            console.error('Failed to fetch document extract:', error);
        } finally {
            setLoading(false);
        }
    };

    // Helper function to format file size
    const formatFileSize = (bytes: number): string => {
        if (bytes < 1024) return bytes + ' B';
        else if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB';
        else return (bytes / 1048576).toFixed(1) + ' MB';
    };

    // Helper function to format date
    const formatDate = (dateString: string): string => {
        const date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
    };

    return (
        <>
            <div className={classes.container}>
                <Table
                    role="grid"
                    aria-label="Documents table"
                >
                    <TableHeader>
                        <TableRow>
                            {columns.slice(0, -1).map((column) => (
                                <TableHeaderCell key={column.columnKey} className={classes.headerText}>
                                    {column.label}
                                </TableHeaderCell>
                            ))} 
                            <TableHeaderCell />
                        </TableRow>
                    </TableHeader>

                    <TableBody>
                        {documents && documents.map((item) => (
                            <TableRow key={item.id}>
                                <TableCell tabIndex={0} role="gridcell">
                                    {item.documentName}
                                </TableCell>
                                <TableCell tabIndex={0} role="gridcell">
                                    {formatFileSize(item.fileSize)}
                                </TableCell>
                                <TableCell tabIndex={0} role="gridcell">
                                    {formatDate(item.uploadDate)}
                                </TableCell>                                <TableCell tabIndex={0} role="gridcell">
                                    <span className={item.availableInSearchIndex ? classes.statusAvailable : classes.statusPending}>
                                        {item.availableInSearchIndex ? "Available" : "Pending"}
                                    </span>
                                </TableCell>
                                <TableCell tabIndex={0} role="gridcell">
                                    <span className={item.extractAvailable ? classes.statusAvailable : classes.statusPending}>
                                        {item.extractAvailable ? "Available" : "Pending"}
                                    </span>
                                </TableCell>                                <TableCell role="gridcell">
                                    <TableCellLayout className={classes.deleteColumn}>
                                        <Button 
                                            icon={<DocumentText24Regular />} 
                                            onClick={() => handleViewChunks(item)} 
                                            //disabled={!item.availableInSearchIndex} 
                                            title={item.availableInSearchIndex ? "View document chunks" : "Document not yet processed"}
                                        />
                                        <Button 
                                            icon={<DocumentRegular />} 
                                            onClick={() => handleViewExtract(item)}
                                            disabled={!item.extractAvailable}
                                            title={item.extractAvailable ? "View document extract" : "Extract not available"}
                                        />
                                        <Button 
                                            icon={<ArrowDownload24Regular />} 
                                            onClick={() => handleExtract(item)} 
                                            disabled={item.extractAvailable}
                                            title={item.extractAvailable ? "Extract already available" : "Extract document content"}
                                        />
                                        <Button 
                                            icon={<Delete12Regular />} 
                                            onClick={() => handleDelete(item.threadId, item.id)}
                                            title="Delete document"
                                        />
                                    </TableCellLayout>
                                </TableCell>
                            </TableRow>
                        ))} 
                    </TableBody>
                </Table>
            </div>

            <Dialog open={showChunks} onOpenChange={(_, { open }) => setShowChunks(open)}>
                <DialogSurface className={classes.dialogSurface}>
                    <DialogBody>
                        <DialogContent className={classes.dialogContent}>
                            {loading ? (
                                <div className={classes.loadingContainer}>
                                    <Spinner label="Loading document chunks..." />
                                </div>
                            ) : (
                                <ChunkViewer chunks={documentChunks} />
                            )}
                        </DialogContent>
                    </DialogBody>
                </DialogSurface>
            </Dialog>

            <Dialog open={showExtract} onOpenChange={(_, { open }) => setShowExtract(open)}>
                <DialogSurface className={classes.dialogSurface}>
                    <DialogBody>
                        <DialogContent className={classes.dialogContent}>
                            {loading ? (
                                <div className={classes.loadingContainer}>
                                    <Spinner label="Loading document extract..." />
                                </div>
                            ) : (
                                <div className={classes.markdownContent}>
                                    <ReactMarkdown>{extractContent}</ReactMarkdown>
                                </div>
                            )}
                        </DialogContent>
                    </DialogBody>
                </DialogSurface>
            </Dialog>
        </>
    );
};