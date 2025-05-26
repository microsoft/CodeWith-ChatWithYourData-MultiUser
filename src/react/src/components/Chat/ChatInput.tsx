import { makeStyles, tokens, Button, Textarea, Dialog, DialogBody, DialogContent, DialogSurface, DialogTitle } from '@fluentui/react-components';
import { BroomRegular, Send24Regular, Prompt20Regular, DismissRegular, Document20Regular, DocumentSignature16Regular, AppFolderRegular } from '@fluentui/react-icons';
import { PredefinedPrompt, Tool } from '../../models/Settings';
import { useEffect, useState, useRef } from 'react';

const useClasses = makeStyles({
    container: {
        display: 'flex',
        flexDirection: 'column',
        width: '70%',
        margin: 'auto',
        marginBottom: tokens.spacingVerticalL,
        alignItems: 'center',
        gap: tokens.spacingVerticalS,
        paddingTop: tokens.spacingVerticalL,
        '@media (max-width: 768px)': {
            width: '90%',
        },
    },
    selectedDocumentsBadges: {
        display: 'flex',
        flexWrap: 'wrap',
        gap: tokens.spacingHorizontalXS,
        padding: '4px 12px',
    },
    documentBadge: {
        display: 'flex',
        alignItems: 'center',
        gap: tokens.spacingHorizontalXXS,
        backgroundColor: tokens.colorNeutralBackground3,
        borderRadius: tokens.borderRadiusMedium,
        padding: '2px 8px',
        margin: '2px 0',
        fontSize: tokens.fontSizeBase200,
    },
    removeDocumentButton: {
        cursor: 'pointer',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '2px',
        borderRadius: '50%',
        '&:hover': {
            backgroundColor: tokens.colorNeutralBackground3Hover,
        }
    },
    inputContainer: {
        display: 'flex',
        width: '100%',
        flexDirection: 'column',
        gap: tokens.spacingHorizontalS,
    },
    inputWrapper: {
        display: 'flex',
        position: 'relative',
        width: '100%',
        flexDirection: 'column',
        border: `1px solid ${tokens.colorNeutralStroke1}`,
        borderRadius: tokens.borderRadiusMedium,
        backgroundColor: tokens.colorNeutralBackground1,
    },
    input: {
        flexGrow: 1,
        width: '100%',
        border: 'none',
        borderRadius: tokens.borderRadiusMedium,
        backgroundColor: 'transparent',
        padding: '12px 12px 40px 12px', // Extra padding at bottom for buttons
        resize: 'none',
        minHeight: '36px'        
    },
    inputHeader: {
        display: 'flex',
        flexWrap: 'wrap',
        gap: tokens.spacingHorizontalXS,
        padding: '4px 12px',
        backgroundColor: tokens.colorNeutralBackground3,
        borderTopLeftRadius: tokens.borderRadiusMedium,
        borderTopRightRadius: tokens.borderRadiusMedium,
    },
    inputFooter: {
        position: 'absolute',
        bottom: '4px',
        left: '0',
        right: '0',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '0 8px',
        height: '36px',
    },
    leftButtons: {
        display: 'flex',
        gap: '4px',
        alignItems: 'center',
    },
    rightButtons: {
        display: 'flex',
        gap: '4px',
    },
    promptsDropdown: {
        display: 'inline-flex',
    },
    actionButton: {
        height: '28px',
        padding: '4px',
        minWidth: 'auto',
        width: '28px',
    },
    sendButton: {
        height: '28px',
        padding: '4px',
        minWidth: 'auto',
        width: '28px',
        
    },
    modalTitle: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
        borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    },
    modalCloseButton: {
        padding: '4px',
        minWidth: 'auto',
        height: '28px',
        width: '28px',
    },
    promptsModal: {
        maxWidth: '600px',
        width: '90%',
    },
    promptsList: {
        display: 'flex',
        flexDirection: 'column',
        gap: tokens.spacingVerticalS,
        margin: 0,
        padding: 0,
        listStyle: 'none',
    },
    promptItem: {
        cursor: 'pointer',
        padding: tokens.spacingVerticalS,
        borderRadius: tokens.borderRadiusMedium,
        transition: 'all 0.1s ease',
        '&:hover': {
            backgroundColor: tokens.colorNeutralBackground3Hover,
        }
    },
    promptName: {
        fontWeight: tokens.fontWeightSemibold,
        marginBottom: tokens.spacingVerticalXS,
    },
    promptText: {
        color: tokens.colorNeutralForeground2,
        fontSize: tokens.fontSizeBase200,
        display: '-webkit-box',
        WebkitLineClamp: 2,
        WebkitBoxOrient: 'vertical',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },
    documentsList: {
        display: 'flex',
        flexDirection: 'column',
        gap: tokens.spacingVerticalS,
        margin: 0,
        padding: 0,
        listStyle: 'none',
    },
    documentItem: {
        cursor: 'pointer',
        padding: tokens.spacingVerticalS,
        borderRadius: tokens.borderRadiusMedium,
        transition: 'all 0.1s ease',
        '&:hover': {
            backgroundColor: tokens.colorNeutralBackground3Hover,
        }
    },
    documentName: {
        fontWeight: tokens.fontWeightSemibold,
        marginBottom: tokens.spacingVerticalXS,
    },
    documentInfo: {
        color: tokens.colorNeutralForeground2,
        fontSize: tokens.fontSizeBase200,
        display: 'flex',
        alignItems: 'center',
        gap: tokens.spacingHorizontalS,
    },
    documentsModal: {
        maxWidth: '600px',
        width: '90%',
    },
    noDocuments: {
        padding: tokens.spacingVerticalM,
        textAlign: 'center',
        color: tokens.colorNeutralForeground2,
    },
    toolsList: {
        display: 'flex',
        flexDirection: 'column',
        gap: tokens.spacingVerticalS,
        margin: 0,
        padding: 0,
        listStyle: 'none',
    },
    toolItem: {
        cursor: 'pointer',
        padding: tokens.spacingVerticalS,
        borderRadius: tokens.borderRadiusMedium,
        transition: 'all 0.1s ease',
        '&:hover': {
            backgroundColor: tokens.colorNeutralBackground3Hover,
        }
    },
    toolName: {
        fontWeight: tokens.fontWeightSemibold,
        marginBottom: tokens.spacingVerticalXS,
    },
    toolDescription: {
        color: tokens.colorNeutralForeground2,
        fontSize: tokens.fontSizeBase200,
        display: '-webkit-box',
        WebkitLineClamp: 2,
        WebkitBoxOrient: 'vertical',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },
    toolsModal: {
        maxWidth: '600px',
        width: '90%',
    },
    noTools: {
        padding: tokens.spacingVerticalM,
        textAlign: 'center',
        color: tokens.colorNeutralForeground2,
    },
    toolBadge: {
        display: 'flex',
        alignItems: 'center',
        gap: tokens.spacingHorizontalXXS,
        backgroundColor: tokens.colorBrandBackground,
        color: tokens.colorNeutralForegroundOnBrand,
        borderRadius: tokens.borderRadiusMedium,
        padding: '2px 8px',
        margin: '2px 0',
        fontSize: tokens.fontSizeBase200,
    },
    selectedToolsBadges: {
        display: 'flex',
        flexWrap: 'wrap',
        gap: tokens.spacingHorizontalXS,
        padding: '4px 12px',
    },
    removeTool: {
        cursor: 'pointer',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '2px',
        borderRadius: '50%',
        '&:hover': {
            backgroundColor: tokens.colorNeutralBackground1,
        }
    },
});

import { IDocument } from '../../models/Document';

type chatInputType = {
    value: string,
    setValue: (value: string ) => void,
    onSubmit: (documentIds?: string[], toolIds?: string[]) => void,
    clearChat: () => void,
    predefinedPrompts?: PredefinedPrompt[],
    documents?: IDocument[],
    tools?: Tool[]
}

export function ChatInput({ value, setValue, onSubmit, clearChat, predefinedPrompts = [], documents = [], tools = [] }: chatInputType) {
    const classes = useClasses();
    const [availablePrompts, setAvailablePrompts] = useState<PredefinedPrompt[]>([]);    
    const [availableTools, setAvailableTools] = useState<Tool[]>([]);
    const [rows, setRows] = useState(1);
    const textAreaRef = useRef<HTMLTextAreaElement>(null);
    const [isPromptsModalOpen, setIsPromptsModalOpen] = useState(false);
    const [isDocumentsModalOpen, setIsDocumentsModalOpen] = useState(false);
    const [isToolsModalOpen, setIsToolsModalOpen] = useState(false);
    const [selectedDocumentIds, setSelectedDocumentIds] = useState<string[]>([]);
    const [selectedToolIds, setSelectedToolIds] = useState<string[]>([]);
      // Set available prompts and tools in a single effect
    useEffect(() => {
        setAvailablePrompts(Array.isArray(predefinedPrompts) ? predefinedPrompts : []);
        setAvailableTools(Array.isArray(tools) ? tools : []);
    }, [predefinedPrompts, tools]);
    
    useEffect(() => {
        // Auto-adjust textarea rows based on content
        if (textAreaRef.current) {
            const textArea = textAreaRef.current;
            textArea.style.height = 'auto';
            
            const newRows = Math.min(
                Math.max(Math.ceil(textArea.scrollHeight / 24), 1), // Approximate line height is 24px
                5 // Maximum 5 rows
            );
            
            setRows(newRows);
        }
    }, [value]);    
    
    const handleSubmit = () => {
        if (value.trim()) {
            onSubmit(
                selectedDocumentIds.length > 0 ? selectedDocumentIds : undefined,
                selectedToolIds.length > 0 ? selectedToolIds : undefined
            );
            // Reset selected documents after submission
           // setSelectedDocumentIds([]);
           
           // Reset selected tools after submission, 
           // so we can talk with the outcome after the submission
           setSelectedToolIds([]);
        }
    };
    
    const handleKeyDown = (event: React.KeyboardEvent<HTMLTextAreaElement>) => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            handleSubmit();
        }
    };

    const handlePromptSelect = (promptText: string) => {
        setValue(promptText);
        setIsPromptsModalOpen(false);
        if (textAreaRef.current) {
            textAreaRef.current.focus();
        }
    };    
    const handleDocumentSelect = (document: IDocument) => {
        // Add document ID to selectedDocumentIds if not already selected
        if (!selectedDocumentIds.includes(document.id)) {
            setSelectedDocumentIds([...selectedDocumentIds, document.id]);
        }
        setIsDocumentsModalOpen(false);
    };
    
    const handleToolSelect = (tool: Tool) => {
        // Add tool ID to selectedToolIds if not already selected
        if (!selectedToolIds.includes(tool.id)) {
            setSelectedToolIds([...selectedToolIds, tool.id]);
            setValue(value + ` ${tool.name}`);
        }
        setIsToolsModalOpen(false);
    };
    
    const removeSelectedDocument = (documentId: string) => {
        setSelectedDocumentIds(selectedDocumentIds.filter(id => id !== documentId));
    };

    const removeSelectedTool = (toolId: string) => {
        setSelectedToolIds(selectedToolIds.filter(id => id !== toolId));
    };

    return (
            <div className={classes.container}>
                <div className={classes.inputContainer}>                    
                    <div className={classes.inputWrapper}>
                        <div className={classes.inputHeader}>
                            {selectedDocumentIds.length > 0 && (
                            <div className={classes.selectedDocumentsBadges}>
                                {selectedDocumentIds.map(docId => {
                                    const document = documents.find(doc => doc.id === docId);
                                    return document && (
                                        <div key={docId} className={classes.documentBadge}>
                                            <Document20Regular />
                                            <span>{document.documentName}</span>
                                            <span 
                                                className={classes.removeDocumentButton}
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    removeSelectedDocument(docId);
                                                }}
                                            >
                                                <DismissRegular fontSize={10} />
                                            </span>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                        {selectedToolIds.length > 0 && (
                            <div className={classes.selectedToolsBadges}>                                {selectedToolIds.map(toolId => {
                                    const tool = availableTools.find(tool => tool.id === toolId);
                                    return tool && (
                                        <div key={toolId} className={classes.toolBadge}>
                                            <span>{tool.name}</span>
                                            <span 
                                                className={classes.removeTool}
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    removeSelectedTool(toolId);
                                                }}
                                            >
                                                <DismissRegular fontSize={10} />
                                            </span>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                        </div>
                        
                        <Textarea
                            ref={textAreaRef}
                            className={classes.input}
                            value={value}
                            onChange={(_e, data) => setValue(data.value)}
                            onKeyDown={handleKeyDown}
                            placeholder="Ask a question..."
                            resize="none"
                            rows={rows}
                        />
                        
                        <div className={classes.inputFooter}>                           
                             <div className={classes.leftButtons}>
                                <Button 
                                    className={classes.actionButton} 
                                    icon={<BroomRegular />} 
                                    onClick={clearChat} 
                                    aria-label="Clear session" 
                                    title="Clear session"
                                    appearance="subtle"
                                    size="small"
                                />
                                 
                               {availablePrompts.length > 0 && (
                                <Button
                                    className={classes.actionButton}
                                    icon={<Prompt20Regular />}
                                    onClick={() => setIsPromptsModalOpen(true)}
                                    aria-label="Predefined prompts"
                                    title="Predefined prompts"
                                    appearance="subtle"
                                    size="small"
                                />
                               )}
                                 {documents && documents.length > 0 && (
                                <Button
                                    className={classes.actionButton}
                                    icon={<DocumentSignature16Regular />}
                                    onClick={() => setIsDocumentsModalOpen(true)}
                                    aria-label="Available documents"
                                    title="Available documents"
                                    appearance="subtle"
                                    size="small"
                                />
                               )}
                               
                               {availableTools.length > 0 && (
                                <Button
                                    className={classes.actionButton}
                                    icon={<AppFolderRegular />}
                                    onClick={() => setIsToolsModalOpen(true)}
                                    aria-label="Available tools"
                                    title="Available tools"
                                    appearance="subtle"
                                    size="small"
                                />
                               )}                                                              
                            </div>                              
                            <div className={classes.rightButtons}>                                
                                <Button 
                                    className={classes.sendButton} 
                                    onClick={handleSubmit} 
                                    icon={<Send24Regular />}
                                    disabled={!value.trim() && selectedToolIds.length === 0}
                                    title="Send message"
                                    appearance={(value.trim() || selectedToolIds.length > 0) ? "primary" : "subtle"}
                                    size="small"
                                />
                            </div>
                        </div>
                    </div>
                </div>                 {/* Prompts Modal Dialog */}
                    <Dialog 
                        open={isPromptsModalOpen} 
                        onOpenChange={(_, data) => setIsPromptsModalOpen(data.open)}
                    >
                        <DialogSurface className={classes.promptsModal}>
                            <div className={classes.modalTitle}>
                                <DialogTitle>Select a prompt</DialogTitle>
                                <Button
                                    className={classes.modalCloseButton}
                                    icon={<DismissRegular />}
                                    onClick={() => setIsPromptsModalOpen(false)}
                                    appearance="subtle"
                                    size="small"
                                />
                            </div>
                            
                            <DialogBody>
                                <DialogContent>
                                    <ul className={classes.promptsList}>
                                        {availablePrompts.map((prompt) => (
                                            <li 
                                                key={prompt.id}
                                                className={classes.promptItem}
                                                onClick={() => handlePromptSelect(prompt.prompt)}
                                            >
                                                <div className={classes.promptName}>{prompt.name}</div>
                                                <div className={classes.promptText}>{prompt.prompt}</div>
                                            </li>
                                        ))}
                                    </ul>
                                </DialogContent>
                            </DialogBody>
                        </DialogSurface>
                    </Dialog>
                    
                    {/* Documents Modal Dialog */}
                    <Dialog 
                        open={isDocumentsModalOpen} 
                        onOpenChange={(_, data) => setIsDocumentsModalOpen(data.open)}
                    >
                        <DialogSurface className={classes.documentsModal}>
                            <div className={classes.modalTitle}>
                                <DialogTitle>Available Documents</DialogTitle>
                                <Button
                                    className={classes.modalCloseButton}
                                    icon={<DismissRegular />}
                                    onClick={() => setIsDocumentsModalOpen(false)}
                                    appearance="subtle"
                                    size="small"
                                />
                            </div>
                            
                            <DialogBody>
                                <DialogContent>
                                    {documents.length > 0 ? (
                                        <ul className={classes.documentsList}>
                                            {documents.map((document) => (
                                                <li 
                                                    key={document.id}
                                                    className={classes.documentItem}
                                                    onClick={() => handleDocumentSelect(document)}
                                                >
                                                    <div className={classes.documentName}>{document.documentName}</div>
                                                    <div className={classes.documentInfo}>
                                                        <Document20Regular /> {`${(document.fileSize / 1024).toFixed(1)} KB`}
                                                    </div>
                                                </li>
                                            ))}
                                        </ul>
                                    ) : (
                                        <div className={classes.noDocuments}>
                                            No documents available for this chat.
                                        </div>
                                    )}
                                </DialogContent>
                            </DialogBody>
                        </DialogSurface>
                    </Dialog>
                    {/* Tools Modal Dialog */}
                    <Dialog 
                        open={isToolsModalOpen} 
                        onOpenChange={(_, data) => setIsToolsModalOpen(data.open)}
                    >
                        <DialogSurface className={classes.toolsModal}>
                            <div className={classes.modalTitle}>
                                <DialogTitle>Available Tools</DialogTitle>
                                <Button
                                    className={classes.modalCloseButton}
                                    icon={<DismissRegular />}
                                    onClick={() => setIsToolsModalOpen(false)}
                                    appearance="subtle"
                                    size="small"
                                />
                            </div>
                            
                            <DialogBody>
                                <DialogContent>
                                    {availableTools.length === 0 ? (
                                        <div className={classes.noTools}>No tools available</div>
                                    ) : (
                                        <ul className={classes.toolsList}>
                                            {availableTools.map((tool) => (
                                                <li 
                                                    key={tool.id}
                                                    className={classes.toolItem}
                                                    onClick={() => handleToolSelect(tool)}
                                                >
                                                    <div className={classes.toolName}>{tool.name}</div>
                                                    <div className={classes.toolDescription}>{tool.description}</div>
                                                </li>
                                            ))}
                                        </ul>
                                    )}
                                </DialogContent>
                            </DialogBody>
                        </DialogSurface>
                    </Dialog>
            </div>

            
        );
    };