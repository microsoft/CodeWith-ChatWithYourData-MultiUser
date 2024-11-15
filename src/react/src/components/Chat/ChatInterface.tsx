import { makeStyles, Toast, Toaster, ToastIntent, ToastTitle, tokens, useId, useToastController, Dialog, DialogTrigger, DialogSurface, Button, DialogBody, DialogTitle, DialogContent, DialogActions  } from '@fluentui/react-components';
import { useChatMessages } from '../../hooks/useChatMessages';
import { useState } from 'react';
import { MessageList } from './MessageList';
import { ChatInput } from './ChatInput';
import { DocumentViewer } from '../Documents/DocumentViewer';
import { ChatHeader } from './ChatHeader';
import React from 'react';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        width: "100%",
        paddingTop: tokens.spacingHorizontalM,
        flexDirection: 'column'
    },
    header: {
        height: "48px",
        display: "flex",
        flexDirection: 'column',
        paddingLeft: tokens.spacingVerticalM,
        paddingRight: tokens.spacingVerticalM,
        justifyContent: "center"
    },
    body: {
        display: 'flex',
        height: '100%',
        flexDirection: 'column',
    },
    toast: {
        width: '200%'
    }
});

type chatInterfaceType = {
    selectedChatId: string | undefined
}

export function ChatInterface({ selectedChatId }: chatInterfaceType) {
    const classes = useClasses();
    const { messages, sendMessage, chatPending, deleteMessages } = useChatMessages(selectedChatId);
    const [userInput, setUserInput] = useState<string>("");
    const [selectedTab, setSelectedTab] = useState<string>("chat");
    const toasterId = useId("toaster");
    const { dispatchToast } = useToastController(toasterId);
    const [isDialogVisible, setIsDialogVisible] = useState(false);
    const labelId: string = useId('dialogLabel');
    const subTextId: string = useId('subTextLabel');

    const notify = (intent:ToastIntent, notification:string) =>
        dispatchToast(
            <Toast className={classes.toast}>
                <ToastTitle>{notification}</ToastTitle>
            </Toast>,
            { position:'top-end', intent: intent, timeout: 5000 }
        );

    const submitMessage = async (message: string) => {
        if (selectedChatId && message) {
            setUserInput("");
            const success = await sendMessage({ message });
            if (!success) notify('error', "Failed to send message.");
        }
    }

    const handleFollowUp = (question: string) => {
        submitMessage(question);
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

    const cancelClearChat = () => {
        setIsDialogVisible(false);
    }
   
    const modalProps = React.useMemo(
        () => ({
            titleAriaId: labelId,
            subtitleAriaId: subTextId,
            isBlocking: false,
            styles: { main: { maxWidth: 450 }, backgroundColor:tokens.colorBrandBackground2},
        }),
        [labelId, subTextId],
    );


    return (
        <div className={classes.root}>
            <div className={classes.body}>
                <Toaster toasterId={toasterId} />
                {(selectedChatId) && (<ChatHeader selectedTab={selectedTab} setSelectedTab={setSelectedTab} />)}
                {(selectedTab === "chat" && selectedChatId) && (
                    <>
                         <Dialog open={isDialogVisible} onOpenChange={(event, data) => setIsDialogVisible(data.open)}>
                            <DialogSurface>
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
                        <ChatInput value={userInput} setValue={setUserInput} onSubmit={() => submitMessage(userInput)} clearChat={clearChat} />
                    </>
                )}
                {(selectedTab === "documents" && selectedChatId) && (<DocumentViewer chatId={selectedChatId} />)}
            </div>
        </div>
    );
};
