import { makeStyles } from '@fluentui/react-components';
import { Message } from './Message';
import { useEffect, useRef } from 'react';
import { IChatMessage } from '../../models/ChatMessage';

const useClasses = makeStyles({
    scrollContainer: {
        flex: 1,
        heigth: '100%',
        display: 'flex',
        overflow: 'scroll',
        overflowX: 'hidden',
        flexDirection: 'column',
        '&::-webkit-scrollbar': {
            display: 'none'
        },
    },
    messageContainer: {
        width: '70%',
        margin: 'auto',
        height: 'calc(100vh - 60px)',
    }
});

type messageListProps = {
    messages: IChatMessage[];
    loading: boolean;
    onFollowUp: (question: string) => void;
}

export function MessageList({ messages, loading, onFollowUp }: messageListProps) {

    const classes = useClasses();
    const containerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (containerRef.current ) {
            containerRef.current.scrollTo({
                top: containerRef.current.scrollTop = containerRef.current.scrollHeight,
                behavior: 'smooth',
            });
        }
    }, [messages]);

    return (
        <div ref={containerRef} className={classes.scrollContainer}>
            <div className={classes.messageContainer}>
                {messages.map((message) => (
                    <Message key={message.id} message={message} onFollowUp={onFollowUp} />
                ))}
                {loading && <div>Loading...</div>}
            </div>
        </div>
    );
};