import { Text, Button, makeStyles, tokens } from '@fluentui/react-components';
import { ListItem } from './ListItem';
import { Add24Regular, Dismiss24Regular, Home24Regular, Settings24Regular, SignOut24Regular } from '@fluentui/react-icons';
import { IChat } from '../../models/Chat';
import { ListSkeleton } from '../Loading/ListSkeleton';
import { useMsal } from '@azure/msal-react';
import React from 'react';
import { useLayoutStyles } from '../../styles/sharedStyles';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        backgroundColor: tokens.colorNeutralBackground2,
        height: '100%',
        overflow: 'hidden', // prevent overflow
        paddingTop: tokens.spacingVerticalM,
        paddingRight: tokens.spacingHorizontalM,
        paddingLeft: tokens.spacingHorizontalM,
        paddingBottom: tokens.spacingVerticalM, // reduce excessive padding
        '@media (max-width: 599px)': {
            width: '100%',
        },
    },
    headerContainer: {
        height: "48px",
        flexShrink: 0, // explicitly prevent shrinking
        display: "flex",
        alignItems: 'center',
        justifyContent: 'space-between',
        marginBottom: tokens.spacingVerticalS, // small margin below header
    },
    scrollContainer: {
        flex: 1,
        overflowY: "auto",
        '&::-webkit-scrollbar': {
            width: '4px',
        },
        '&::-webkit-scrollbar-thumb': {
            backgroundColor: tokens.colorNeutralStroke2,
            borderRadius: tokens.borderRadiusCircular,
        }
    },
    signoutButtonContainer: {
        height: "48px",
        flexShrink: 0, // explicitly prevent shrinking
        marginTop: tokens.spacingVerticalS, // small margin above footer
        display: 'flex',
        gap: tokens.spacingHorizontalS,
    },
    signoutButton: {
        flex: 1,
    },
    listHeaderText: {
        marginTop: tokens.spacingVerticalS,
        marginBottom: tokens.spacingVerticalS,
        fontWeight: tokens.fontWeightSemibold,
        fontSize: tokens.fontSizeBase300
    },
    // remove unused drawerRoot if not needed
});

type chatListType = {
    chats: IChat[] | undefined,
    selectedChatId: string | undefined,
    selectChat: (chatId?: string) => void,
    addChat: () => Promise<IChat>,
    deleteChat: ({ chatId }: { chatId: string; }) => Promise<boolean>,
    updateChatName: ({ chatId, name }: { chatId: string; name: string; }) => Promise<boolean>,
    loading: boolean,
    toggleSidebar?: () => void,
    isMobile?: boolean
};

export function Chatlist({ 
    chats, 
    selectedChatId, 
    selectChat, 
    addChat, 
    deleteChat,
    updateChatName, 
    loading,
    toggleSidebar,
    isMobile = false
}: chatListType) {
    const classes = useClasses();
    const { instance } = useMsal();

    const handleAddChat = async () => {
        await addChat();
        if (isMobile && toggleSidebar) {
            toggleSidebar();
        }
    };

    const handleSelectChat = (chatId?: string) => {
        selectChat(chatId);
        
    };

    const chatListContent = (
        <>
            <div className={classes.headerContainer}>
                <Button onClick={() => selectChat()} size="large" icon={<Home24Regular />} />
                <Button onClick={handleAddChat} size="large" icon={<Add24Regular />} />
            </div>
            <Text className={classes.listHeaderText}>My chats</Text>
            <div className={classes.scrollContainer}>
                {loading && (
                    <ListSkeleton />
                )}
                {/* Chat list */}
                {(chats && !loading) && chats.map((item) => {
                    return <ListItem key={item.id} item={item} isSelected={selectedChatId === item.id} selectChat={handleSelectChat} deleteChat={deleteChat} updateChatName={updateChatName} />;
                })}
            </div>
            
            <div className={classes.signoutButtonContainer}>
                <Button className={classes.signoutButton} onClick={() => instance.logout()} size="large" icon={<SignOut24Regular />}>Sign out</Button>
            </div>
        </>
    );

    return (
        <div className={classes.root}>
            {chatListContent}
        </div>
    );
};