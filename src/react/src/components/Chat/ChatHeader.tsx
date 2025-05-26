import { makeStyles, tokens, TabList, Tab } from '@fluentui/react-components';

const useClasses = makeStyles({
    header: {
        display: "flex",
        flexDirection: 'column',
        paddingLeft: tokens.spacingHorizontalM, // use horizontal token explicitly
        paddingRight: tokens.spacingHorizontalM, // use horizontal token explicitly
        justifyContent: "center"
    }
});

type chatHeaderType = {
    selectedTab: string,
    setSelectedTab: (tab: string) => void
}

export function ChatHeader({ selectedTab, setSelectedTab }: chatHeaderType) {

    const classes = useClasses();

    return (
        <div className={classes.header}>
            <TabList selectedValue={selectedTab} onTabSelect={(_e, data) => { setSelectedTab(data.value as string) }}>
                <Tab value="chat">Chat</Tab>
                <Tab value="documents">Documents</Tab>
            </TabList>
        </div>
    );
};