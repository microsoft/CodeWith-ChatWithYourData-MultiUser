import { Button, Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow, makeStyles, tokens } from '@fluentui/react-components';
import { Delete12Regular } from '@fluentui/react-icons/fonts';
import { IDocument } from '../../models/Document';

const useClasses = makeStyles({
    container: {
        display: 'flex',
        marginTop: tokens.spacingVerticalM,
    },
    deleteColumn: {
        display: 'flex',
        justifyContent: 'flex-end'
    },
    headerText: {
        fontWeight: tokens.fontWeightSemibold,
    }
});

const columns = [
    { columnKey: "fileName", label: "Document name" },
    { columnKey: "status", label: "Status" }
];

type documentGridProps = { 
      documents?: IDocument[];
      deleteDocument:  ({chatId, documentId}:{ chatId: string; documentId: string; }) => Promise<boolean>
}

export function DocumentGrid({ documents, deleteDocument } : documentGridProps) {

    const classes = useClasses();

    const handleDelete = async (chatId: string, documentId: string) => {
        await deleteDocument({chatId: chatId, documentId: documentId});
    }

    return (
        <div className={classes.container}>
            <Table
                role="grid"
                aria-label="File table"
            >
                <TableHeader>
                    <TableRow>
                        {columns.map((column) => (
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
                                {item.availableInSearchIndex ? "Available" : "Pending"}
                            </TableCell>
                           
                            <TableCell role="gridcell">
                                <TableCellLayout className={classes.deleteColumn}>
                                    <Button icon={<Delete12Regular />} onClick={() => handleDelete(item.threadId, item.id)}/>
                                </TableCellLayout>
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </div>
    );
};