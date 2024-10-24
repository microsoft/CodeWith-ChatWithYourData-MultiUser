import { Text, Button, makeStyles, tokens } from '@fluentui/react-components';
import { ListItem } from './ListItem';
import { Add24Regular, Home24Regular, SignOut24Regular } from '@fluentui/react-icons';
import { IChat } from '../../models/Chat';
import { ListSkeleton } from '../Loading/ListSkeleton';
import { useMsal } from '@azure/msal-react';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        width: '280px',
        paddingTop: tokens.spacingHorizontalM,
        paddingRight: tokens.spacingVerticalM,
        paddingLeft: tokens.spacingVerticalM,
        flexDirection: 'column',
        backgroundColor: tokens.colorNeutralBackground2
    },
    headerContainer: {
        height: "48px",
        display: "flex",
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between'
    },
    scrollContainer: {
        flex: 1,
        overflowY: "auto"
    },
    signoutButtonContainer: {
        height: "48px",
    },
    signoutButton: {
       width: "100%",
    },
    listHeaderText: {
        marginTop: tokens.spacingVerticalM,
        marginBottom: tokens.spacingVerticalM,
        fontWeight: tokens.fontWeightSemibold,
        fontSize: tokens.fontSizeBase300
    }
});

type chatListType = {
    chats: IChat[] | undefined,
    selectedChatId: string | undefined,
    selectChat: (chatId?: string) => void,
    addChat: () => Promise<IChat>,
    deleteChat: ({ chatId }: { chatId: string; }) => Promise<boolean>,
    loading: boolean
}

export function Chatlist({ chats, selectedChatId, selectChat, addChat, deleteChat, loading }: chatListType) {
    const classes = useClasses();
    const { instance } = useMsal();
   

    return (
        <div className={classes.root}>
            <div className={classes.headerContainer}>
                <Button onClick={() => selectChat()} size="large" icon={<Home24Regular />} />
                <Button onClick={() => addChat()} size="large" icon={<Add24Regular />} />
            </div>
            <Text className={classes.listHeaderText}>My chats</Text>
            <div className={classes.scrollContainer}>
                {loading && (
                    <ListSkeleton />
                )}
                {/* Chat list */}
                {(chats && !loading) && chats.map((item) => {
                    return <ListItem item={item} isSelected={selectedChatId == item.id} selectChat={selectChat} deleteChat={deleteChat} />
                })}
            </div>
            <div className={classes.signoutButtonContainer}>
                <Button className={classes.signoutButton} onClick={() =>  instance.logout()} size="large" icon={<SignOut24Regular />}>Sign out</Button>
            </div>
        </div>
    );
};