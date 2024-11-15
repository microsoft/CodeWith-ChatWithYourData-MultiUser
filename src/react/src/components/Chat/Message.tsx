import { Field, makeStyles, ProgressBar, tokens, Button } from '@fluentui/react-components';
import { Stack, Text} from '@fluentui/react'
import { IChatMessage } from '../../models/ChatMessage';
import Markdown from 'react-markdown';
import { parseAnswer } from './AnswerParser';

const useClasses = makeStyles({
    userContainer: {
        display: 'flex',
        justifyContent: 'flex-end',
        marginTop: tokens.spacingVerticalL
    },
    assistantContainer: {
        display: 'flex',
        justifyContent: 'start',
        marginTop: tokens.spacingVerticalL
    },
    userTextContainer: {
        backgroundColor: tokens.colorNeutralBackground1Hover,
        borderRadius: tokens.borderRadiusXLarge,
        maxWidth: '80%',
        padding: tokens.spacingHorizontalM,
        paddingTop: 0,
        paddingBottom: 0,
        boxShadow: tokens.shadow2
    },
    assistantTextContainer: {
        backgroundColor: tokens.colorNeutralBackground1Pressed,
        borderRadius: tokens.borderRadiusXLarge,
        maxWidth: '80%',
        padding: tokens.spacingHorizontalM,
        paddingTop: 0,
        paddingBottom: 0,
        boxShadow: tokens.shadow2
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
        backgroundColor: tokens.colorNeutralBackground1Pressed,
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
        marginTop: tokens.spacingVerticalS,
        marginLeft: 'auto',
        marginRight: 'auto',
        maxWidth: '80%',
    },
    followUpButton: {
        marginTop: tokens.spacingVerticalXS
    }
});

type messageProps = {
    message: IChatMessage;
    onFollowUp: (question: string) => void;
}

export function Message({ message, onFollowUp }: messageProps) {

    const answer = parseAnswer(message);
    const classes = useClasses();

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
                    <div className={message.role == "user" ? classes.userTextContainer : classes.assistantTextContainer}>
                        <Markdown>{answer.markdownFormatText}</Markdown>
                        <Stack horizontal className="" verticalAlign="start">
                        {!!answer.citations.length  && (
                            <Stack.Item aria-label="References">
                                <Stack style={{ width: "100%" }} >
                                    <Stack horizontal horizontalAlign='start' verticalAlign='center'>
                                        <Text
                                            className=""
                                            
                                        >
                                        <span>{answer.citations.length > 1 ? answer.citations.length + " references" : "1 reference"}</span>
                                        </Text>
                                    </Stack>

                                </Stack>
                            </Stack.Item>
                        )}
                        </Stack>
                    </div>
                )}
            </div>
            {message.followupquestions && message.followupquestions.length > 0 && (
                <div className={classes.followUpContainer}>
                    {message.followupquestions.map((question, index) => (
                        <Button key={index} className={classes.followUpButton} onClick={() => onFollowUp(question)}>
                            {question}
                        </Button>
                    ))}
                </div>
            )}
        </>
    );
};
