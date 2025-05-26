import { Button, Input, Menu, MenuItem, MenuList, MenuPopover, MenuTrigger, Text, makeStyles, mergeClasses, tokens } from '@fluentui/react-components';
import { IChat } from '../../models/Chat';
import { Edit24Regular, MoreHorizontal16Regular } from '@fluentui/react-icons';
import { useState } from 'react';

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
    editInput: {
        maxWidth: '170px',
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
    updateChatName: ({chatId, name}: { chatId: string; name: string; }) => Promise<boolean>,
}

export function ListItem({ item, isSelected, selectChat, deleteChat, updateChatName }: listItemType) {
    const [isEditing, setIsEditing] = useState(false);
    const [editedName, setEditedName] = useState(item.threadName);
    const classes = useClasses();

    const handleEdit = (e: React.MouseEvent) => {
        e.stopPropagation();
        setEditedName(item.threadName);
        setIsEditing(true);
    };

    const handleSave = async (e: React.MouseEvent | React.KeyboardEvent) => {
        e.stopPropagation();
        if (editedName.trim() && editedName.trim() !== item.threadName) {
            await updateChatName({ chatId: item.id, name: editedName.trim() });
        }
        setIsEditing(false);
    };

    const handleCancel = (e: React.MouseEvent) => {
        e.stopPropagation();
        setEditedName(item.threadName);
        setIsEditing(false);
    };

    const handleKeyDown = async (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') {
            await handleSave(e);
        } else if (e.key === 'Escape') {
            handleCancel(e as any);
        }
    };

    const handleInputClick = (e: React.MouseEvent) => {
        e.stopPropagation();
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setEditedName(e.target.value);
    };

    return (
        <div key={item.id} onClick={() => selectChat(item.id)} className={mergeClasses(classes.root, isSelected && classes.selected)} title={`Chat: ${item.threadName}`} aria-label={`Chat list item: ${item.threadName}`}>
            {isEditing ? (
                <>
                    <Input 
                        className={classes.editInput}
                        value={editedName}
                        onChange={handleInputChange}
                        onClick={handleInputClick}
                        onKeyDown={handleKeyDown}
                        autoFocus
                    />
                    <Button appearance="subtle" onClick={handleSave}>Save</Button>
                </>
            ) : (
                <>
                    <Text className={classes.title} title={item.threadName}>
                        {item.threadName}
                    </Text>
                    <Menu>
                        <MenuTrigger disableButtonEnhancement>
                            <Button icon={<MoreHorizontal16Regular />} appearance="transparent" />
                        </MenuTrigger>

                        <MenuPopover>
                            <MenuList>
                                <MenuItem onClick={handleEdit}>Edit name</MenuItem>
                                <MenuItem onClick={() => deleteChat({chatId: item.id})}>Delete chat</MenuItem>
                            </MenuList>
                        </MenuPopover>
                    </Menu>
                </>
            )}
        </div>
    );
};