// filepath: d:\repos\codewith\codewith-philips\CodeWith-ChatWithYourData-MultiUser\src\react\src\components\admin\HealthTab.tsx
import {
    Text,
    Card,
    CardHeader,
    makeStyles,
    tokens,
    Spinner,
    Button,
    Badge,
    Tooltip,
    Table,
    TableHeader,
    TableRow,
    TableHeaderCell,
    TableBody,
    TableCell,
    TableCellLayout
} from "@fluentui/react-components";
import { Dismiss20Regular, Info20Regular, Status20Regular} from "@fluentui/react-icons";
import { HealthCheck } from "../../services/AdminService";
import { formatDate } from "../../utils/dateUtils";

interface HealthTabProps {
    healthData: HealthCheck | null;
    isLoading: boolean;
    error: Error | null;
    onRefresh: () => void;
}

const useStyles = makeStyles({
    container: {
        display: 'flex',
        flexDirection: 'column',
        gap: tokens.spacingVerticalM,
        height: '100%',
        overflow: 'auto',
    },
    header: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: tokens.spacingVerticalS,
    },
    healthCard: {
        marginBottom: tokens.spacingVerticalM,
    },
    statusContainer: {
        display: 'flex',
        alignItems: 'center',
        gap: tokens.spacingHorizontalS,
    },
    healthyStatus: {
        color: tokens.colorPaletteGreenForeground1,
        fontWeight: tokens.fontWeightSemibold,
    },
    unhealthyStatus: {
        color: tokens.colorPaletteRedForeground1,
        fontWeight: tokens.fontWeightSemibold,
    },
    timestamp: {
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground2,
    },
    message: {
        fontSize: tokens.fontSizeBase300,
        wordBreak: 'break-word',
    },
    healthyIcon: {
        color: tokens.colorPaletteGreenForeground1,
    },
    unhealthyIcon: {
        color: tokens.colorPaletteRedForeground1,
    },
    badgeHealthy: {
        backgroundColor: tokens.colorPaletteGreenBackground1,
        color: tokens.colorPaletteGreenForeground1,
    },
    badgeUnhealthy: {
        backgroundColor: tokens.colorPaletteRedBackground1,
        color: tokens.colorPaletteRedForeground1,
    },
    refreshButton: {
        marginLeft: tokens.spacingHorizontalS,
    },
    errorContainer: {
        color: tokens.colorPaletteRedForeground1,
        padding: tokens.spacingVerticalM,
    },
});

export const HealthTab = ({ healthData, isLoading, error, onRefresh }: HealthTabProps) => {
    const styles = useStyles();

    if (isLoading && !healthData) {
        return <Spinner label="Loading health data..." />;
    }

    if (error) {
        return (
            <div className={styles.errorContainer}>
                <Text weight="semibold">Error loading health data: {error.message}</Text>
                <Button appearance="primary" icon={<Status20Regular />} onClick={onRefresh}>
                    Retry
                </Button>
            </div>
        );
    }

    if (!healthData) {
        return (
            <div>
                <Text>No health data available.</Text>
                <Button appearance="primary" icon={<Status20Regular />} onClick={onRefresh}>
                    Refresh
                </Button>
            </div>
        );
    }

    const formattedTimestamp = formatDate(new Date(healthData.timestamp));

    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <div>
                    <Text size={500} weight="semibold">System Health</Text>
                    <Text className={styles.timestamp}>Last updated: {formattedTimestamp}</Text>
                </div>
                <Button 
                    icon={<Status20Regular />} 
                    onClick={onRefresh}
                    className={styles.refreshButton}
                >
                    Refresh
                </Button>
            </div>

            <Card className={styles.healthCard}>
                <CardHeader
                    header={
                        <div className={styles.statusContainer}>
                            <Text 
                                size={400} 
                                weight="semibold"
                                className={healthData.isHealthy ? styles.healthyStatus : styles.unhealthyStatus}
                            >
                                Overall Status:
                            </Text>
                            <Badge 
                                appearance="filled" 
                                className={healthData.isHealthy ? styles.badgeHealthy : styles.badgeUnhealthy}
                            >
                                {healthData.isHealthy ? 'Healthy' : 'Unhealthy'}
                            </Badge>
                        </div>
                    }
                />
            </Card>

            <Table aria-label="Service Health Table">
                <TableHeader>
                    <TableRow>
                        <TableHeaderCell>Service</TableHeaderCell>
                        <TableHeaderCell>Status</TableHeaderCell>
                        <TableHeaderCell>Message</TableHeaderCell>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {Object.entries(healthData.components).map(([serviceName, serviceHealth]) => (
                        <TableRow key={serviceName}>
                            <TableCell>
                                <TableCellLayout>
                                    <Text weight="semibold">{serviceName}</Text>
                                </TableCellLayout>
                            </TableCell>
                            <TableCell>
                                <TableCellLayout>
                                    <div className={styles.statusContainer}>
                                        {serviceHealth.isWorking ? (
                                            <Tooltip content="Service is working properly" relationship="label">
                                                <Info20Regular className={styles.healthyIcon} />
                                            </Tooltip>
                                        ) : (
                                            <Tooltip content="Service has issues" relationship="label">
                                                <Dismiss20Regular className={styles.unhealthyIcon} />
                                            </Tooltip>
                                        )}
                                        <Text className={serviceHealth.isWorking ? styles.healthyStatus : styles.unhealthyStatus}>
                                            {serviceHealth.isWorking ? 'Healthy' : 'Unhealthy'}
                                        </Text>
                                    </div>
                                </TableCellLayout>
                            </TableCell>
                            <TableCell>
                                <Text className={styles.message}>{serviceHealth.message}</Text>
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </div>
    );
};