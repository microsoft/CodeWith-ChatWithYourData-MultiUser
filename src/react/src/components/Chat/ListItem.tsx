import { Button, Menu, MenuItem, MenuList, MenuPopover, MenuTrigger, Text, makeStyles, mergeClasses, tokens } from '@fluentui/react-components';
import { IChat } from '../../models/Chat';
import { MoreHorizontal16Regular } from '@fluentui/react-icons';

const useClasses = makeStyles({
    root: {
        boxSizing: 'border-box',
        display: 'flex',
        flexDirection: 'row',
        width: '100%',
        cursor: 'pointer',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginTop: tokens.spacingVerticalS,
        padding: tokens.spacingHorizontalXS,
        borderRadius: tokens.borderRadiusLarge,
        ":hover": {
            backgroundColor: tokens.colorNeutralBackground2Hover
        }
    },
    title: {
        fontSize: tokens.fontSizeBase300,
        color: tokens.colorNeutralForeground1,
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        width: '170px',
        textOverflow: 'ellipsis',
    },
    selected: {
        backgroundColor: tokens.colorNeutralBackground2Pressed

    }
});

type listItemType = {
    item: IChat,
    isSelected: boolean,
    selectChat: (chatId?: string) => void,
    deleteChat: ({chatId }: { chatId: string; }) => Promise<boolean>,
}

export function ListItem({ item, isSelected, selectChat, deleteChat }: listItemType) {

    const classes = useClasses();

    return (
        <div key={item.id} onClick={() => selectChat(item.id)} className={mergeClasses(classes.root, isSelected && classes.selected)} title={`Chat: ${item.threadName}`} aria-label={`Chat list item: ${item.threadName}`}>

            <Text className={classes.title} title={item.threadName}>
                {item.threadName}
            </Text>
            <Menu>
                <MenuTrigger disableButtonEnhancement>
                    <Button icon={<MoreHorizontal16Regular />} appearance="transparent" />
                </MenuTrigger>

                <MenuPopover>
                    <MenuList>
                        <MenuItem onClick={() => deleteChat({chatId: item.id})}>Delete chat</MenuItem>
                    </MenuList>
                </MenuPopover>
            </Menu>
        </div>
    );
};