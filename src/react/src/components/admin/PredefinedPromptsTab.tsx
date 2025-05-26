import { 
    Button, 
    Card, 
    CardHeader, 
    Input, 
    Spinner, 
    makeStyles, 
    tokens,
    Table,
    TableHeader,
    TableRow,
    TableHeaderCell,
    TableBody,
    TableCell,
    Textarea,
    Dialog,
    DialogTrigger,
    DialogSurface,
    DialogTitle,
    DialogBody,
    DialogContent,
    DialogActions,
    Field
} from "@fluentui/react-components";
import { Delete24Regular, Edit24Regular, Add24Regular } from "@fluentui/react-icons";
import { useState, useEffect } from "react";
import { PredefinedPrompt } from "../../models/Settings";

const useStyles = makeStyles({
    container: {
        padding: tokens.spacingVerticalM,
    },
    card: {
        padding: tokens.spacingVerticalL,
        width: '100%'
    },
    header: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: tokens.spacingVerticalM
    },
    headerActions: {
        display: 'flex',
        gap: tokens.spacingHorizontalS
    },
    table: {
        marginTop: tokens.spacingVerticalM,
        width: '100%'
    },
    actionCell: {
        display: 'flex',
        gap: tokens.spacingHorizontalS
    },
    form: {
        display: 'flex',
        flexDirection: 'column',
        gap: tokens.spacingVerticalM
    },
    buttonIcon: {
        marginRight: tokens.spacingHorizontalS
    }
});

interface PredefinedPromptsTabProps {
    isLoading: boolean;
    prompts?: PredefinedPrompt[];
    onSavePrompts: (prompts: PredefinedPrompt[]) => void;
}

export function PredefinedPromptsTab({ isLoading, prompts = [], onSavePrompts }: PredefinedPromptsTabProps) {
    const styles = useStyles();
    const [localPrompts, setLocalPrompts] = useState<PredefinedPrompt[]>([]);
    const [currentPrompt, setCurrentPrompt] = useState<PredefinedPrompt>({ id: '', name: '', prompt: '' });
    const [isEditing, setIsEditing] = useState(false);
    const [dialogOpen, setDialogOpen] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    
    useEffect(() => {
        if (prompts) {
            console.log("PredefinedPromptsTab received prompts:", prompts);
            // Ensure we have a valid array of prompts
            const validPrompts = Array.isArray(prompts) ? prompts : [];
            setLocalPrompts(validPrompts.map(p => ({
                ...p,
                id: p.id || Date.now().toString()
            })));
        }
    }, [prompts]);

    const resetForm = () => {
        setCurrentPrompt({ id: Date.now().toString(), name: '', prompt: '' });
        setErrorMessage(null);
    };

    const handleAddPrompt = () => {
        setIsEditing(false);
        resetForm();
        setDialogOpen(true);
    };

    const handleEditPrompt = (prompt: PredefinedPrompt) => {
        setIsEditing(true);
        setCurrentPrompt({ ...prompt });
        setErrorMessage(null);
        setDialogOpen(true);
    };

    const handleDeletePrompt = (id: string) => {
        const updatedPrompts = localPrompts.filter(p => p.id !== id);
        console.log("Deleting prompt, new prompts array:", updatedPrompts);
        setLocalPrompts(updatedPrompts);
        onSavePrompts(updatedPrompts);
    };

    const handleSavePrompt = () => {
        const trimmedName = currentPrompt.name.trim();
        const trimmedPrompt = currentPrompt.prompt.trim();
        
        if (trimmedName === '' || trimmedPrompt === '') {
            setErrorMessage("Name and prompt text are required.");
            return;
        }

        const validPrompt = {
            id: currentPrompt.id || Date.now().toString(),
            name: trimmedName,
            prompt: trimmedPrompt
        };
        
        const updatedPrompts = isEditing 
            ? localPrompts.map(p => p.id === validPrompt.id ? validPrompt : p)
            : [...localPrompts, validPrompt];
        
        console.log("Saving prompt, new prompts array:", updatedPrompts);
        setLocalPrompts(updatedPrompts);
        onSavePrompts(updatedPrompts);
        setDialogOpen(false);
        resetForm();
    };

    const handleCloseDialog = () => {
        setDialogOpen(false);
        resetForm();
    };

    if (isLoading) {
        return <Spinner />;
    }

    return (
        <div className={styles.container}>
            <Card className={styles.card}>
                <div className={styles.header}>
                    <CardHeader header="Predefined Prompts" />
                    <div className={styles.headerActions}>
                        <Button icon={<Add24Regular />} onClick={handleAddPrompt}>
                            Add Prompt
                        </Button>
                    </div>
                </div>
                
                <Table className={styles.table}>
                    <TableHeader>
                        <TableRow>
                            <TableHeaderCell>Name</TableHeaderCell>
                            <TableHeaderCell>Prompt</TableHeaderCell>
                            <TableHeaderCell>Actions</TableHeaderCell>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {localPrompts.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={3}>No predefined prompts. Add one to get started.</TableCell>
                            </TableRow>
                        ) : (
                            localPrompts.map((prompt) => (
                                <TableRow key={prompt.id}>
                                    <TableCell>{prompt.name}</TableCell>
                                    <TableCell>{prompt.prompt}</TableCell>
                                    <TableCell>
                                        <div className={styles.actionCell}>
                                            <Button 
                                                icon={<Edit24Regular />} 
                                                onClick={() => handleEditPrompt(prompt)}
                                                aria-label="Edit prompt"
                                            />
                                            <Button 
                                                icon={<Delete24Regular />} 
                                                onClick={() => handleDeletePrompt(prompt.id)}
                                                aria-label="Delete prompt"
                                            />
                                        </div>
                                    </TableCell>
                                </TableRow>
                            ))
                        )}
                    </TableBody>
                </Table>
                
                <Dialog open={dialogOpen} onOpenChange={(_, data) => setDialogOpen(data.open)}>
                    <DialogSurface>
                        <DialogBody>
                            <DialogTitle>{isEditing ? 'Edit Prompt' : 'Add Prompt'}</DialogTitle>
                            <DialogContent>
                                <div className={styles.form}>
                                    <Field label="Name" required>
                                        <Input
                                            value={currentPrompt.name}
                                            onChange={(_, data) => setCurrentPrompt(prev => ({ ...prev, name: data.value }))}
                                            placeholder="E.g., Summary, Key Points, etc."
                                        />
                                    </Field>
                                    <Field label="Prompt Text" required>
                                        <Textarea
                                            value={currentPrompt.prompt}
                                            onChange={(_, data) => setCurrentPrompt(prev => ({ ...prev, prompt: data.value }))}
                                            placeholder="E.g., Give me a summary of this document"
                                            rows={4}
                                        />
                                    </Field>
                                </div>
                            </DialogContent>
                            <DialogActions>
                                <Button appearance="secondary" onClick={handleCloseDialog}>
                                    Cancel
                                </Button>
                                <Button appearance="primary" onClick={handleSavePrompt}>
                                    Save
                                </Button>
                            </DialogActions>
                        </DialogBody>
                    </DialogSurface>
                </Dialog>
            </Card>
        </div>
    );
}