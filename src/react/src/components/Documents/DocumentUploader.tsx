import { Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle, DialogTrigger, makeStyles, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow, tokens } from '@fluentui/react-components';
import { Add12Regular } from '@fluentui/react-icons';
import { useRef, useState } from 'react';

const useClasses = makeStyles({
    container: {
        boxSizing: 'border-box',
        display: 'flex',
        flexDirection: 'column',
        width: '100%',
        backgroundColor: tokens.colorNeutralBackground2Hover,
        borderRadius: tokens.borderRadiusMedium,
        padding: tokens.spacingVerticalM,
        boxShadow: tokens.shadow2,
        marginTop: tokens.spacingVerticalL,
    },
    headerText: {
        fontWeight: tokens.fontWeightSemibold,
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


type documentUploadButtonProps = {
    uploadDocuments: ({ chatId, documents }: { chatId: string; documents: File[]; }) => Promise<boolean>;
    chatId: string | undefined;
}

export function DocumentUploader({ uploadDocuments, chatId }: documentUploadButtonProps) {

    const fileInputRef = useRef<HTMLInputElement>(null);

    const classes = useClasses();
    const [files, setFiles] = useState<File[]>([]);
    const [showPopUp, setShowPopUp] = useState<boolean>(false);
    const [uploading, setUploading] = useState<boolean>(false);

    const handleFileSelection = (e: React.ChangeEvent<HTMLInputElement>) => {
        const files = e.target.files;
        if (!files) {
            setShowPopUp(false);
            setFiles([])
            return;
        }
        const filesArray: File[] = Array.from(files);
        console.log(filesArray);
        setFiles(filesArray);
        setShowPopUp(true);
    }

    const handleButtonClick = () => {
        if (fileInputRef.current) {
            fileInputRef.current.click();
        };
    };

    const handleClosePopup = () => {
        setShowPopUp(false);
        setFiles([]);
    };

    const handleUploadFiles = async () => {
        if (chatId) {
            setUploading(true);
            await uploadDocuments({ chatId: chatId, documents: files });
            setFiles([]);
            setUploading(false);
            setShowPopUp(false);
        }
    }

    return (
        <div>
            <Button icon={<Add12Regular />} onClick={handleButtonClick}>Add documents</Button>
            <input onChange={handleFileSelection} multiple={true} ref={fileInputRef} type='file' hidden />

            <Dialog open={showPopUp}>

                <DialogSurface aria-describedby={undefined}>
                    <DialogBody>
                        <DialogTitle>Are you sure you want to upload these files?</DialogTitle>
                        <DialogContent>
                            <Table
                                role="grid"
                                aria-label="File table"
                            >
                                <TableHeader>
                                    <TableRow>
                                        <TableHeaderCell key={'filename'} className={classes.headerText}>
                                            File name
                                        </TableHeaderCell>
                                    </TableRow>
                                </TableHeader>

                                <TableBody>
                                    {files.map((file) => (
                                        <TableRow key={file.name}>
                                            <TableCell tabIndex={0} role="gridcell">
                                                {file.name}
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </DialogContent>
                        <DialogActions>
                            <DialogTrigger disableButtonEnhancement>
                                <Button appearance="primary" onClick={handleUploadFiles} disabled={uploading}>Upload</Button>
                            </DialogTrigger>
                            <DialogTrigger disableButtonEnhancement>
                                <Button appearance="secondary" onClick={handleClosePopup} disabled={uploading}>Cancel</Button>
                            </DialogTrigger>
                        </DialogActions>
                    </DialogBody>
                </DialogSurface>

            </Dialog>

        </div>
    );
};