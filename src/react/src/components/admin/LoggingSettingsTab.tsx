import React, { useEffect, useState } from 'react';
import { 
  Card, 
  CardHeader,
  Text, 
  Body1, 
  Switch, 
  Label, 
  makeStyles,
  shorthands
} from '@fluentui/react-components';
import LoggingService from '../../services/LoggingService';

const useStyles = makeStyles({
  card: {
    ...shorthands.margin('16px', '0'),
    ...shorthands.padding('16px')
  },
  settingItem: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap('8px'),
    ...shorthands.margin('16px', '0')
  },
  switchRow: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap('8px')
  },
  description: {
    color: 'var(--colorNeutralForeground2)'
  }
});

export function LoggingSettingsTab(){
  const [loggingEnabled, setLoggingEnabled] = useState<boolean>(
    LoggingService.isLoggingEnabled()
  );

  // Handle toggle
  const handleLoggingToggle = (event: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = event.target.checked;
    LoggingService.setLoggingEnabled(newValue);
    setLoggingEnabled(newValue);
  };

  // Subscribe to changes
  useEffect(() => {
    const unsubscribe = LoggingService.subscribeToLoggingChanges(() => {
      setLoggingEnabled(LoggingService.isLoggingEnabled());
    });
    
    return unsubscribe;
  }, []);
  const styles = useStyles();

  return (
    <Card className={styles.card}>
      <CardHeader header={<Text weight="semibold" size={600}>Debugging Settings</Text>} />
      
      <div className={styles.settingItem}>
        <div className={styles.switchRow}>
          <Switch 
            checked={loggingEnabled}
            onChange={handleLoggingToggle}
            label={{
              children: "Enable Verbose Console Logging"
            }}
          />
        </div>
        
        <Body1 className={styles.description}>
          When enabled, detailed debug information will be logged to the browser console.
          This setting is useful for troubleshooting but may impact performance.
        </Body1>
      </div>
    </Card>
  );
};
