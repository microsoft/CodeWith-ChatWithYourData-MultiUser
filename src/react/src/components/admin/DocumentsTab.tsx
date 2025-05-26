import { Card, Spinner, makeStyles, tokens, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow, Input, Button, Select, Option } from "@fluentui/react-components";
import { IDocument } from "../../models/Document";
import { useState, useMemo } from "react";
import { ArrowSortDown24Regular, ArrowSortUp24Regular, FilterRegular } from "@fluentui/react-icons";
import { useLayoutStyles } from "../../styles/sharedStyles";

const useStyles = makeStyles({
    container: {
        padding: tokens.spacingVerticalM,
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
    },
    
    tableContainer: {
        flex: '1 1 auto',
        height: 'calc(100vh - 250px)',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'auto',
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
    tableContent: {
        flex: '1 1 auto',
    }
});

export interface DocumentsTabProps {
    documents?: IDocument[];
    isLoading: boolean;
}

export function DocumentsTab({ documents, isLoading }: DocumentsTabProps) {
    const styles = useStyles();
    const layoutStyles = useLayoutStyles();
    const [threadIdFilter, setThreadIdFilter] = useState<string>("");
    const [sortConfig, setSortConfig] = useState<{ key: keyof IDocument | null; direction: 'ascending' | 'descending' | null }>({
        key: null,
        direction: null
    });

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

    // Handle sorting when a column header is clicked
    const handleSort = (key: keyof IDocument) => {
        let direction: 'ascending' | 'descending' | null = 'ascending';
        
        if (sortConfig.key === key) {
            if (sortConfig.direction === 'ascending') {
                direction = 'descending';
            } else if (sortConfig.direction === 'descending') {
                direction = null;
            }
        }
        
        setSortConfig({ key, direction });
    };

    // Filter and sort documents
    const filteredAndSortedDocuments = useMemo(() => {
        if (!documents) return [];
        
        // First filter by threadId if a filter is set
        let filteredDocs = documents;
        if (threadIdFilter) {
            filteredDocs = documents.filter(doc => 
                doc.threadId.toLowerCase().includes(threadIdFilter.toLowerCase())
            );
        }
        
        // Then sort if needed
        if (sortConfig.key && sortConfig.direction) {
            return [...filteredDocs].sort((a, b) => {
                // Handle different data types appropriately
                if (sortConfig.key === 'uploadDate') {
                    const dateA = new Date(a[sortConfig.key]).getTime();
                    const dateB = new Date(b[sortConfig.key]).getTime();
                    return sortConfig.direction === 'ascending' ? dateA - dateB : dateB - dateA;
                } else if (sortConfig.key === 'fileSize') {
                    return sortConfig.direction === 'ascending' 
                        ? a[sortConfig.key] - b[sortConfig.key]
                        : b[sortConfig.key] - a[sortConfig.key];
                } else if (sortConfig.key) {
                    // String comparison for other fields
                    const valueA = String(a[sortConfig.key]);
                    const valueB = String(b[sortConfig.key]);
                    return sortConfig.direction === 'ascending'
                        ? valueA.localeCompare(valueB)
                        : valueB.localeCompare(valueA);
                }
                return 0; // Fallback return
            });
        }
        
        return filteredDocs;
    }, [documents, threadIdFilter, sortConfig]);

    // Generate sort icon for table headers
    const getSortIcon = (key: keyof IDocument) => {
        if (sortConfig.key !== key) return null;
        if (sortConfig.direction === 'ascending') return <ArrowSortUp24Regular />;
        if (sortConfig.direction === 'descending') return <ArrowSortDown24Regular />;
        return null;
    };

    if (isLoading) {
        return <Spinner />;
    }

    return (
        <div className={styles.container}>
            <Card>
                <div style={{ marginBottom: tokens.spacingVerticalM, display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
                    <Input
                        contentBefore={<FilterRegular />}
                        placeholder="Filter by Thread ID"
                        value={threadIdFilter}
                        onChange={(_, data) => setThreadIdFilter(data.value)}
                        style={{ width: '300px' }}
                    />
                    {threadIdFilter && (
                        <Button 
                            appearance="subtle"
                            onClick={() => setThreadIdFilter("")}
                        >
                            Clear Filter
                        </Button>
                    )}
                </div>
                <div className={styles.tableContainer}>
                    {filteredAndSortedDocuments.length > 0 ? (
                        <Table aria-label="Documents table" className={`${styles.tableContent}`}>
                            <TableHeader>
                                <TableRow>
                                    <TableHeaderCell 
                                        className={styles.headerText}
                                        onClick={() => handleSort('id')}
                                        style={{ cursor: 'pointer' }}
                                    >
                                        Document Id {getSortIcon('id')}
                                    </TableHeaderCell>
                                    <TableHeaderCell 
                                        className={styles.headerText}
                                        onClick={() => handleSort('documentName')}
                                        style={{ cursor: 'pointer' }}
                                    >
                                        Document Name {getSortIcon('documentName')}
                                    </TableHeaderCell>
                                    <TableHeaderCell 
                                        className={styles.headerText}
                                        onClick={() => handleSort('threadId')}
                                        style={{ cursor: 'pointer' }}
                                    >
                                        Thread Id {getSortIcon('threadId')}
                                    </TableHeaderCell>
                                    <TableHeaderCell 
                                        className={styles.headerText}
                                        onClick={() => handleSort('fileSize')}
                                        style={{ cursor: 'pointer' }}
                                    >
                                        Size {getSortIcon('fileSize')}
                                    </TableHeaderCell>
                                    <TableHeaderCell 
                                        className={styles.headerText}
                                        onClick={() => handleSort('uploadDate')}
                                        style={{ cursor: 'pointer' }}
                                    >
                                        Upload Date {getSortIcon('uploadDate')}
                                    </TableHeaderCell>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {filteredAndSortedDocuments.map((document) => (
                                    <TableRow key={document.id}>
                                        <TableCell>{document.id}</TableCell>
                                        <TableCell>{document.documentName}</TableCell>
                                        <TableCell>{document.threadId}</TableCell>
                                        <TableCell>{formatFileSize(document.fileSize)}</TableCell>
                                        <TableCell>{formatDate(document.uploadDate)}</TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    ) : (
                        <div>No documents found</div>
                    )}
                </div>
            </Card>
        </div>
    );
}