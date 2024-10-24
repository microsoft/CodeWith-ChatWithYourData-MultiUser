import { Field, makeStyles, ProgressBar, tokens } from '@fluentui/react-components';
import { IChatMessage } from '../../models/ChatMessage';
import Markdown from 'react-markdown';

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
    title: {
        flexGrow: 1,
        fontSize: tokens.fontSizeBase500,
    }
});

type messageProps = {
    message: IChatMessage
}

export function Message({ message }: messageProps) {

    const classes = useClasses();

    return (
        <div id={message.id} className={message.role == "user" ? classes.userContainer : classes.assistantContainer}>
            {message.content == "" ? (
                <Field validationMessage="Thinking..." validationState="none">
                    <ProgressBar />
                </Field>
            ) : (<div className={message.role == "user" ? classes.userTextContainer : classes.assistantTextContainer}>
                <Markdown>{message.content}</Markdown>
            </div>)}
        </div>
    )
};