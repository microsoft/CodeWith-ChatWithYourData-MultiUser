import React from 'react';
import { makeStyles, Spinner, Text, tokens } from '@fluentui/react-components';

interface HeartbeatStatusProps {
    heartbeatStatus: {
        content: string;
        agentName: string;
        timestamp: string;
        isActive: boolean;
    } | null;
}

const useStyles = makeStyles({
    container: {
        position: 'fixed',
        bottom: '80px', // Above the chat input
        right: '20px',
        maxWidth: '300px',
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
        color: 'white',
        padding: tokens.spacingHorizontalM,
        borderRadius: tokens.borderRadiusMedium,
        zIndex: 1000,
        display: 'flex',
        alignItems: 'center',
        gap: tokens.spacingHorizontalS,
        boxShadow: '0 4px 8px rgba(0, 0, 0, 0.2)',
    },
    textContainer: {
        display: 'flex',
        flexDirection: 'column',
    },
    agentName: {
        fontSize: tokens.fontSizeBase200,
        opacity: 0.8,
    },
    content: {
        fontSize: tokens.fontSizeBase300,
    }
});

/**
 * Component to display the current heartbeat status from the SSE stream
 * using FluentUI components
 */
export const HeartbeatStatus: React.FC<HeartbeatStatusProps> = ({ heartbeatStatus }) => {
    const styles = useStyles();
    
    if (!heartbeatStatus || !heartbeatStatus.isActive) {
        return null;
    }

    return (
        <div className={styles.container}>
            <Spinner size="tiny" appearance="inverted" />
            <div className={styles.textContainer}>
                <Text className={styles.agentName}>
                    {heartbeatStatus.agentName}
                </Text>
                <Text className={styles.content}>
                    {heartbeatStatus.content}
                </Text>
            </div>
        </div>
    );
};
