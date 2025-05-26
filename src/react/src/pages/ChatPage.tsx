import { makeStyles } from '@fluentui/react-components';
import { Chatlist } from '../components/Chat/Chatlist';
import { ChatInterface } from '../components/Chat/ChatInterface';
import { useChats } from '../hooks/useChats';
import { useState, useEffect } from 'react';
import { breakpoints } from '../styles/sharedStyles';

const useClasses = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'row',
    height: '100%',
    width: '100%',
    overflow: 'hidden',
    minHeight: 0,
  },
  mobileContainer: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    width: '100%',
    overflow: 'hidden',
    minHeight: 0,
  },
  sidebar: {
    width: '280px',
    flexShrink: 0,
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
    transition: 'transform 0.3s ease-in-out',
  },
  sidebarVisible: {
    transform: 'translateX(0)',
  },
  sidebarHidden: {
    transform: 'translateX(-100%)',
    position: 'absolute',
    zIndex: 10,
    backgroundColor: '#fff',
    height: '100%',
  },
  chatArea: {
    flexGrow: 1,
    height: '100%',
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
    minHeight: 0,
  },
});

export function ChatPage() {
    const classes = useClasses();
    const chats = useChats();

    const handleSelectChat = (chatId?: string) => {
        chats.selectChat(chatId);
    };

    return (
        <div className={classes.container}>
            <div className={`${classes.sidebar} ${classes.sidebarVisible}`}>
                <Chatlist 
                    chats={chats.chats} 
                    selectedChatId={chats.selectedChatId} 
                    selectChat={handleSelectChat} 
                    addChat={chats.addChat} 
                    deleteChat={chats.deleteChat}
                    updateChatName={chats.updateChatName}
                    loading={chats.isPending}
                />
            </div>
            <div className={classes.chatArea}>
                <ChatInterface 
                    selectedChatId={chats.selectedChatId}
                />
            </div>
        </div>
    );
};