import { makeStyles } from '@fluentui/react-components';
import { Message } from './Message';
import { useEffect, useRef } from 'react';
import { IChatMessage } from '../../models/ChatMessage';
import { useLayoutStyles } from '../../styles/sharedStyles';

const useClasses = makeStyles({
    scrollContainer: {
        flex: 1,
        height: '100%',
        display: 'flex',
        overflow: 'scroll',
        overflowX: 'hidden',
        flexDirection: 'column',
        '&::-webkit-scrollbar': {
            width: '4px',
        },
        '&::-webkit-scrollbar-thumb': {
            backgroundColor: 'rgba(0, 0, 0, 0.2)',
            borderRadius: '4px',
        },
        scrollBehavior: 'smooth',
    },
    messageContainer: {
        width: '70%',
        margin: 'auto',
        height: 'calc(100vh - 160px)',
        paddingBottom: '20px',
        '@media (max-width: 768px)': {
            width: '90%',
        },
    },
    messageEnter: {
        opacity: 0,
        transform: 'translateY(20px)',
    },
    messageEnterActive: {
        opacity: 1,
        transform: 'translateY(0)',
        transition: 'opacity 300ms, transform 300ms',
    },
    emptyState: {
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        height: '100%',
        textAlign: 'center',
        opacity: 0.7,
    },
    emptyStateText: {
        fontSize: '1.1rem',
        maxWidth: '500px',
    }
});

type messageListProps = {
    messages: IChatMessage[];
    loading: boolean;
    onFollowUp: (question: string) => void;
}

export function MessageList({ messages, loading, onFollowUp }: messageListProps) {
    const classes = useClasses();
    const layoutClasses = useLayoutStyles();
    const containerRef = useRef<HTMLDivElement>(null);
    const lastMessageRef = useRef<HTMLDivElement>(null);

    // Scroll to bottom when new messages are added
    useEffect(() => {
        if (containerRef.current && messages.length > 0) {
            const scrollContainer = containerRef.current;
            scrollContainer.scrollTop = scrollContainer.scrollHeight;
        }
    }, [messages]);

    return (
        <div ref={containerRef} className={classes.scrollContainer}>
            <div className={classes.messageContainer}>
                {messages.length === 0 && !loading && (
                    <div className={classes.emptyState}>
                        <p className={classes.emptyStateText}>
                            Start a conversation by sending a message below.
                        </p>
                    </div>
                )}
                
                {messages.map((message, index) => (
                    <div 
                        key={message.id || index} 
                        ref={index === messages.length - 1 ? lastMessageRef : undefined}
                        className={`${index === messages.length - 1 ? classes.messageEnter + ' ' + classes.messageEnterActive : ''}`}
                    >
                        <Message message={message} onFollowUp={onFollowUp} />
                    </div>
                ))}
                
                {loading && (
                    <div className={classes.messageEnter + ' ' + classes.messageEnterActive}>
                        <Message 
                            message={{ role: 'assistant', content: '' }}
                            onFollowUp={onFollowUp}
                        />
                    </div>
                )}
            </div>
        </div>
    );
};