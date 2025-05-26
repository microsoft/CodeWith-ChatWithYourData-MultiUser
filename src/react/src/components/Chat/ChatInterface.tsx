import { makeStyles, Toast, Toaster, ToastIntent, ToastTitle, tokens, useId, useToastController, Dialog, DialogTrigger, DialogSurface, Button, DialogBody, DialogTitle, DialogContent, DialogActions  } from '@fluentui/react-components';
import { useChatMessages } from '../../hooks/useChatMessages';
import { useState, useEffect } from 'react';
import { MessageList } from './MessageList';
import { ChatInput } from './ChatInput';
import { DocumentViewer } from '../Documents/DocumentViewer';
import { ChatHeader } from './ChatHeader';
import { HeartbeatStatus } from './HeartbeatStatus';
import { useSettings } from '../../hooks/useSettings';
import { useContainerStyles } from '../../styles/sharedStyles';
import { useChats } from '../../hooks/useChats';
import LoggingService from '../../services/LoggingService';
import { useChatDocuments } from '../../hooks/useChatDocuments';
import { IChatMessageRequest } from '../../models/ChatMessageRequest';

const useClasses = makeStyles({
  root: {
    display: 'flex',
    width: "100%",
    height: "100%",
    flexDirection: 'column',
    overflow: 'hidden',
    paddingTop: 0, // remove padding causing overflow
  },
  body: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    overflow: 'hidden',
    minHeight: 0, // critical fix
  },
  headerContainer: {
    height: "48px",
    flexShrink: 0,
    display: "flex",
    alignItems: "center",
    padding: `0 ${tokens.spacingHorizontalM}`,
  },
  toast: {
    width: '200%'
  },
  menuButton: {
    display: 'none',
    '@media (max-width: 599px)': {
      display: 'flex',
    },
  },
  emptyStateContainer: {
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'center',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingHorizontalL,
    textAlign: 'center',
  },
  emptyStateHeading: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalS,
  },
  emptyStateText: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    maxWidth: '500px',
  },
});

type chatInterfaceType = {
    selectedChatId: string | undefined;
    toggleSidebar?: () => void;
    isMobile?: boolean;
    sidebarVisible?: boolean;
}

export function ChatInterface({ selectedChatId }: chatInterfaceType) {    
    const classes = useClasses();
    const containerClasses = useContainerStyles();
    
    const { messages, sendMessageStream, sendCompliancyMessageStream, chatPending, deleteMessages, heartbeatStatus, forceRefreshMessages } = useChatMessages(selectedChatId);
    const { documents } = useChatDocuments(selectedChatId);
    const { settings } = useSettings();
    const [userInput, setUserInput] = useState<string>("");
    const [selectedTab, setSelectedTab] = useState<string>("chat");
    const toasterId = useId("toaster");
    const { dispatchToast } = useToastController(toasterId);
    const [isDialogVisible, setIsDialogVisible] = useState(false);
    const chatManager = useChats();    

    useEffect(() => {
        if (selectedChatId) {
            LoggingService.log(`Selected chat changed to ${selectedChatId}, forcing refresh`);
            forceRefreshMessages();
        }
    }, [selectedChatId, forceRefreshMessages]);

    const handleCreateNewChat = async () => {
        try {
            // Simply call addChat without trying to capture and use the return value
            // The useChats hook has an onSuccess handler that will handle selection            
            await chatManager.addChat();
            
            // Add a console log to help debug the issue
            LoggingService.log("Chat created, selection should happen automatically");
        } catch (error) {
            notify('error', "Failed to create new chat.");
            LoggingService.error("Error creating chat:", error);
        }
    };

    const notify = (intent:ToastIntent, notification:string) =>
        dispatchToast(
            <Toast className={classes.toast}>
                <ToastTitle>{notification}</ToastTitle>
            </Toast>,
            { position:'top-end', intent: intent, timeout: 5000 }
        );

    const submitMessage = async (message: string, documentIds?: string[], toolIds?: string[]) => {
        if (selectedChatId && message) {
            setUserInput("");
            const messagepayload:IChatMessageRequest = {
                message : message,
                documentIds : documentIds,
                tools: toolIds,
            };

            let success = false;
            // if (message.toLowerCase().includes("review")) {
            //     LoggingService.log("Using compliancy endpoint for review message");
            //     success = await sendCompliancyMessageStream({ messagepayload });
            // } 
            // else {
                LoggingService.log("Using streaming endpoint for regular message");
                success = await sendMessageStream({message:messagepayload });
            // }
            
            if (!success) 
                notify('error', "Failed to send message.");
        }
    }    
    const handleFollowUp = (question: string) => {
        submitMessage(question, undefined);
    }

    const clearChat = async () => {
        setIsDialogVisible(true);
    }

    const confirmClearChat = async () => {
        setIsDialogVisible(false);
        const success = await deleteMessages();
        if (!success) notify('error', "Failed to clear chat.");
        else notify('success', "Chat cleared.");
    }

    console.log("ChatInterface render", selectedChatId);

    if (!selectedChatId) {
        return (
            <div className={classes.root}>
                <div className={classes.emptyStateContainer}>
                    <div>
                        <h2 className={classes.emptyStateHeading}>Welcome to Chat with Your Data</h2>
                        <p className={classes.emptyStateText}>
                            Select an existing chat from the sidebar or create a new one to get started.
                        </p>
                    </div>
                    <Button appearance="primary" onClick={handleCreateNewChat}>
                        Create New Chat
                    </Button>
                </div>
            </div>
        );
    }

    return (
        <div className={classes.root}>
            <div className={classes.headerContainer}>
                {selectedChatId && (<ChatHeader selectedTab={selectedTab} setSelectedTab={setSelectedTab} />)}
            </div>
            <div className={classes.body}>
                <Toaster toasterId={toasterId} />
                {(selectedTab === "chat" && selectedChatId) && (
                    <>
                        <Dialog open={isDialogVisible} onOpenChange={(_event, data) => setIsDialogVisible(data.open)}>
                            <DialogSurface className={containerClasses.dialog}>
                                <DialogBody>
                                    <DialogTitle>Clear Messages</DialogTitle>
                                    <DialogContent>
                                        Are you sure you want to clear this thread?
                                    </DialogContent>
                                    <DialogActions>
                                        <DialogTrigger disableButtonEnhancement>
                                            <Button appearance="secondary">No</Button>
                                        </DialogTrigger>
                                        <Button appearance="primary" onClick={confirmClearChat}>Yes</Button>
                                    </DialogActions>
                                </DialogBody>
                            </DialogSurface>
                        </Dialog>
                        <MessageList messages={messages} loading={chatPending} onFollowUp={handleFollowUp} />                          
                        <ChatInput 
                            value={userInput} 
                            setValue={setUserInput} 
                            onSubmit={(documentIds, tools) => submitMessage(userInput, documentIds, tools)} 
                            clearChat={clearChat} 
                            documents={documents}
                            predefinedPrompts={settings?.predefinedPrompts || []}
                            tools={settings?.tools || []}
                        />
                        <HeartbeatStatus heartbeatStatus={heartbeatStatus} />
                    </>
                )}
                {(selectedTab === "documents" && selectedChatId) && (<DocumentViewer chatId={selectedChatId} />)}
            </div>
        </div>
    );
};
