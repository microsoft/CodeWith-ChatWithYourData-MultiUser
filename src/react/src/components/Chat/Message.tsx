import { Field, makeStyles, ProgressBar, tokens, Button, Badge, Divider, Tooltip } from '@fluentui/react-components';
import { Stack, Text } from '@fluentui/react'
import { IChatMessage } from '../../models/ChatMessage';
import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Info12Regular, Code20Regular, Copy16Regular, CheckmarkCircle16Regular } from '@fluentui/react-icons';

const useClasses = makeStyles({
    userContainer: {
        display: 'flex',
        justifyContent: 'flex-end',
        marginTop: tokens.spacingVerticalL,
        maxWidth: '90%',
        marginLeft: 'auto',
        '@media (max-width: 599px)': {
            maxWidth: '95%',
        },
    },
    assistantContainer: {
        display: 'flex',
        justifyContent: 'start',
        marginTop: tokens.spacingVerticalL,
        maxWidth: '90%',
        marginRight: 'auto',
        '@media (max-width: 599px)': {
            maxWidth: '95%',
        },
    },
    userTextContainer: {
        backgroundColor: tokens.colorBrandBackground,
        color: tokens.colorNeutralForegroundOnBrand,
        borderRadius: tokens.borderRadiusXLarge,
        maxWidth: '100%',
        padding: tokens.spacingHorizontalL,
        paddingTop: tokens.spacingVerticalM,
        paddingBottom: tokens.spacingVerticalM,
        boxShadow: tokens.shadow4,
        transition: 'all 0.2s ease',
        '&:hover': {
            boxShadow: tokens.shadow8,
        }
    },
    assistantTextContainer: {
        backgroundColor: tokens.colorNeutralBackground2,
        borderRadius: tokens.borderRadiusXLarge,
        maxWidth: '100%',
        padding: tokens.spacingHorizontalL,
        paddingTop: tokens.spacingVerticalM,
        paddingBottom: tokens.spacingVerticalM,
        boxShadow: tokens.shadow4,
        transition: 'all 0.2s ease',
        '&:hover': {
            boxShadow: tokens.shadow8,
        }
    },
    roleLabel: {
        fontSize: tokens.fontSizeBase100,
        marginBottom: tokens.spacingVerticalXS,
    },
    subheader: {
        marginTop: tokens.spacingVerticalS,
        paddingBottom: tokens.spacingVerticalXS,
        fontWeight: tokens.fontWeightRegular,
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground3
    },
    thinkingContainer: {
        width: '50%',
        backgroundColor: tokens.colorNeutralBackground1,
        borderRadius: tokens.borderRadiusXLarge,
        padding: tokens.spacingHorizontalL,
    },
    title: {
        flexGrow: 1,
        fontSize: tokens.fontSizeBase500,
    },
    followUpContainer: {
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'flex-end',
        marginTop: tokens.spacingVerticalM,
        marginLeft: 'auto',
        maxWidth: '80%',
        gap: tokens.spacingVerticalS,
    },
    followUpButton: {
        transition: 'transform 0.2s ease, background 0.2s ease',
        
    },
    markdownContent: {
        width: '100%',
        '& table': {
            borderCollapse: 'collapse',
            margin: '10px 0',
            width: '100%',
            border: `1px solid ${tokens.colorNeutralStroke1}`,
            tableLayout: 'fixed',
        },
        '& thead': {
            backgroundColor: tokens.colorNeutralBackground3,
        },
        '& th, & td': {
            border: `1px solid ${tokens.colorNeutralStroke1}`,
            padding: '8px 12px',
            textAlign: 'left',
            wordWrap: 'break-word',
        },
        '& th': {
            backgroundColor: tokens.colorNeutralBackground4,
            fontWeight: tokens.fontWeightSemibold,
        },
        '& tr:nth-child(even)': {
            backgroundColor: tokens.colorNeutralBackground2,
        },
        '& pre': {
            backgroundColor: tokens.colorNeutralBackground3,
            padding: '10px',
            borderRadius: '4px',
            overflowX: 'auto',
        },
        '& code': {
            fontFamily: 'monospace',
            backgroundColor: tokens.colorNeutralBackground3,
            padding: '2px 4px',
            borderRadius: '3px',
        },
        '& a': {
            color: tokens.colorBrandForeground1,
            textDecoration: 'none',
            '&:hover': {
                textDecoration: 'underline',
            },
        },
        '& img': {
            maxWidth: '100%',
            height: 'auto',
            borderRadius: tokens.borderRadiusMedium,
        },
        '& blockquote': {
            borderLeft: `4px solid ${tokens.colorBrandStroke1}`,
            padding: '0 16px',
            margin: '8px 0',
            color: tokens.colorNeutralForeground2,
        },
    },
    citationBadge: {
        cursor: 'pointer',
        marginRight: tokens.spacingHorizontalXS,
    },
    citationsContainer: {
        marginTop: tokens.spacingVerticalS,
        display: 'flex',
        flexWrap: 'wrap',
        gap: tokens.spacingHorizontalXS,
    },    jsonContainer: {
        backgroundColor: tokens.colorNeutralBackground3,
        padding: '16px',
        borderRadius: tokens.borderRadiusMedium,
        overflowX: 'auto',
        fontFamily: 'monospace',
        whiteSpace: 'pre',
        fontSize: tokens.fontSizeBase200,
        border: `1px solid ${tokens.colorNeutralStroke1}`,
        margin: '8px 0',
        boxShadow: tokens.shadow4,
        position: 'relative',
        transition: 'all 0.2s ease',
        '&:hover': {
            boxShadow: tokens.shadow8,
        }
    },
    jsonHeader: {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        marginBottom: '12px',
        fontWeight: tokens.fontWeightSemibold,
        color: tokens.colorBrandForeground1,
        borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
        paddingBottom: '8px',
    },
    jsonContent: {
        maxHeight: '500px',
        overflowY: 'auto',
        lineHeight: '1.5',
        paddingRight: '4px',
    },
});

// Custom renderer components for Markdown with improved table rendering
const MarkdownComponents = {
    table: (props: React.PropsWithChildren) => (
        <div style={{ overflowX: 'auto', width: '100%', marginBottom: '16px' }}>
            <table style={{ 
                borderCollapse: 'collapse', 
                width: '100%', 
                border: '1px solid #ddd',
                tableLayout: 'fixed'
            }}>
                {props.children}
            </table>
        </div>
    ),
    thead: (props: React.PropsWithChildren) => (
        <thead style={{ backgroundColor: '#f0f0f0' }}>
            {props.children}
        </thead>
    ),
    tr: (props: React.PropsWithChildren) => (
        <tr style={{ borderBottom: '1px solid #ddd' }}>
            {props.children}
        </tr>
    ),
    th: (props: React.PropsWithChildren) => (
        <th style={{ 
            padding: '10px 8px', 
            textAlign: 'left', 
            borderRight: '1px solid #ddd', 
            borderBottom: '2px solid #ddd',
            backgroundColor: '#f0f0f0',
            fontWeight: 'bold',
            wordWrap: 'break-word'
        }}>
            {props.children}
        </th>
    ),
    td: (props: React.PropsWithChildren) => (
        <td style={{ 
            padding: '8px', 
            textAlign: 'left', 
            borderRight: '1px solid #ddd',
            wordWrap: 'break-word'
        }}>
            {props.children}
        </td>
    ),
};

type messageProps = {
    message: IChatMessage;
    onFollowUp: (question: string) => void;
}

/**
 * Checks if a string is valid JSON
 */
const isJSON = (text: string): boolean => {
    if (!text) return false;
    text = text.trim();
    
    // First, do a quick check of starting/ending characters
    if (!(text.startsWith('{') && text.endsWith('}')) && 
        !(text.startsWith('[') && text.endsWith(']'))) {
        return false;
    }
    
    // Make sure the content looks like real JSON, not just some text in curly braces
    // Look for common JSON patterns like key-value pairs with quotes
    const hasKeyValuePattern = /"[^"]+"\s*:/i.test(text);
    
    if (!hasKeyValuePattern && text.startsWith('{')) {
        return false;
    }
    
    try {
        JSON.parse(text);
        
        // Additional check - if the parsed result is an array or object with at least one property
        // and has a reasonable size, it's likely a JSON that we want to format
        const parsed = JSON.parse(text);
        const isObject = typeof parsed === 'object' && parsed !== null;
        const hasProperties = isObject && Object.keys(parsed).length > 0;
        const isReasonableSize = text.length > 10 && text.length < 100000; // Avoid formatting tiny or huge JSON
        
        return isObject && hasProperties && isReasonableSize;
    } catch {
        return false;
    }
};

/**
 * Format JSON with syntax highlighting using HTML
 */
const syntaxHighlightJSON = (json: string): React.ReactNode => {
    if (!json) return null;
    
    try {
        // Parse and re-stringify for consistent formatting
        const obj = JSON.parse(json);
        const formattedJson = JSON.stringify(obj, null, 2);
        
        // Replace JSON syntax with styled spans
        const highlighted = formattedJson
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+-]?\d+)?)/g, 
                (match) => {
                    if (/^"/.test(match)) {
                        if (/:$/.test(match)) {
                            // It's a JSON key
                            // Remove the colon from the end for the key styling
                            match = match.replace(/:$/, '');
                            return `<span style="color: #0078d4; font-weight: 500;">${match}</span>:`;
                        }
                        // It's a string
                        return `<span style="color: #107c10;">${match}</span>`;
                    } else if (/true|false/.test(match)) {
                        // Boolean value
                        return `<span style="color: #8661c5;">${match}</span>`;
                    } else if (/null/.test(match)) {
                        // Null value
                        return `<span style="color: #5c5c5c; font-style: italic;">${match}</span>`;
                    } else {
                        // It's a number
                        return `<span style="color: #d83b01;">${match}</span>`;
                    }
                }
            );

        return <div dangerouslySetInnerHTML={{ __html: highlighted }} />;
    } catch {
        // If there's an error, return the original JSON as plaintext
        return <pre>{json}</pre>;
    }
};

export function Message({ message, onFollowUp }: messageProps) {
    const classes = useClasses();
    const [copied, setCopied] = useState(false);
    
    const handleCopyJSON = () => {
        if (message.content) {
            try {
                const jsonToCopy = JSON.parse(message.content);
                navigator.clipboard.writeText(JSON.stringify(jsonToCopy, null, 2));
                setCopied(true);
                setTimeout(() => setCopied(false), 2000);
            } catch {
                // Fallback to copying raw content if parsing fails
                navigator.clipboard.writeText(message.content);
                setCopied(true);
                setTimeout(() => setCopied(false), 2000);
            }
        }
    };

    const formatCitationLabel = (index: number) => {
        return `[${index + 1}]`;
    };

    const formatMessageDate = (dateString: string | Date | undefined) => {
        if (!dateString) return '';
        
        const messageDate = new Date(dateString);
        const now = new Date();
        
        // Check if valid date
        if (isNaN(messageDate.getTime())) return '';
        
        const isToday = messageDate.toDateString() === now.toDateString();
        const isYesterday = new Date(now.setDate(now.getDate() - 1)).toDateString() === messageDate.toDateString();
        
        const timeOptions: Intl.DateTimeFormatOptions = { hour: '2-digit', minute: '2-digit' };
        const time = messageDate.toLocaleTimeString(undefined, timeOptions);
        
        if (isToday) {
            return ` • Today at ${time}`;
        } else if (isYesterday) {
            return ` • Yesterday at ${time}`;
        } else {
            return ` • ${messageDate.toLocaleDateString()} at ${time}`;
        }
    };

    const formatUsageMetrics = (usage?: { inputTokens: number, outputTokens: number, totalTokens: number }) => {
        if (!usage) return '';
        
        return `Tokens: ${usage.inputTokens} input, ${usage.outputTokens} output, ${usage.totalTokens} total`;
    };

    return (
        <>
            <div id={message.id} className={message.role == "user" ? classes.userContainer : classes.assistantContainer}>
                {message.content == "" ? (
                    <div className={classes.thinkingContainer}>
                        <Field validationMessage="Thinking..." validationState="none">
                            <ProgressBar />
                        </Field>
                    </div>
                ) : (
                    <div className={message.role == "user" ? classes.userTextContainer : classes.assistantTextContainer}>                        <div className={classes.roleLabel}>                            
                            {message.role === "user" ? (
                                <>You{formatMessageDate(message.created)}</>
                            ) : (
                                <><span>{message.agentName || "AI Assistant"}{formatMessageDate(message.created)}</span>
                                <Tooltip content={formatUsageMetrics(message.context?.usageMetrics)} relationship="description">
                                    <span>  •  <Info12Regular/></span>
                                </Tooltip></>
                            )}
                        </div>
                         <div className={classes.markdownContent}>                            {isJSON(message.content) ? (
                                <div className={classes.jsonContainer}>
                                    <div className={classes.jsonHeader}>
                                        <div style={{ display: 'flex', alignItems: 'center' }}>
                                            <Code20Regular style={{ marginRight: '8px', verticalAlign: 'middle' }} />
                                            <span>JSON Response</span>
                                        </div>
                                        <div style={{ marginLeft: 'auto' }}>
                                            <Button 
                                                appearance="subtle" 
                                                icon={copied ? <CheckmarkCircle16Regular /> : <Copy16Regular />}
                                                size="small"
                                                onClick={handleCopyJSON}
                                                title="Copy JSON to clipboard"
                                            >
                                                {copied ? "Copied!" : "Copy"}
                                            </Button>
                                        </div>
                                    </div>
                                    <div className={classes.jsonContent}>
                                        {syntaxHighlightJSON(message.content)}
                                    </div>
                                </div>
                            ) : (
                                <ReactMarkdown 
                                    remarkPlugins={[remarkGfm]} 
                                    components={MarkdownComponents}
                                >
                                    {message.content}
                                </ReactMarkdown>
                            )}
                        </div>
                        {!!message.context?.citations?.length && (
                            <div className={classes.citationsContainer}>
                                {message.context.citations.map((citation, index) => (
                                    <Badge 
                                        key={`citation-${index}`}
                                        className={classes.citationBadge}
                                        appearance="outline"
                                        title={citation.title || citation.filepath || 'Citation'}
                                    >
                                        {formatCitationLabel(index)}
                                    </Badge>
                                ))}
                            </div>
                        )}
                    </div>
                )}
            </div>
            
            {message.context?.followup_questions && message.context.followup_questions.length > 0 && (
                <div className={classes.followUpContainer}>
                    {message.context.followup_questions.map((question, index) => (
                        <Button 
                            key={index} 
                            className={classes.followUpButton} 
                            onClick={() => onFollowUp(question)}
                            appearance="secondary"
                            shape="rounded"
                        >
                            {question}
                        </Button>
                    ))}
                </div>
            )}
        </>
    );
};
