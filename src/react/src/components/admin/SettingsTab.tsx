import { Button, Card, CardHeader, Field, Input, Spinner, Switch, makeStyles, tokens } from "@fluentui/react-components";
import { useState, useEffect } from "react";
import { ISetting } from "../../models/Settings";
import { useLayoutStyles } from "../../styles/sharedStyles";

const useStyles = makeStyles({
    container: {
        padding: tokens.spacingVerticalM,
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
    },
    settingsCard: {
        padding: tokens.spacingVerticalL,
        width: '100%',
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
    },
    settingsForm: {
        display: 'flex',
        flexDirection: 'column',
        gap: '15px',
        width: '100%',
        maxWidth: '400px',
        flex: '1 1 auto',
    },
    toggleSection: {
        marginTop: tokens.spacingVerticalM,
        display: 'flex',
        flexDirection: 'column',
        gap: tokens.spacingVerticalS,
    },
    sectionTitle: {
        fontWeight: 'bold',
        fontSize: tokens.fontSizeBase400,
        marginBottom: tokens.spacingVerticalS,
    }
});

export interface SettingsTabProps {
    isLoading: boolean;
    settings?: ISetting;
    onSaveSettings: (settings: {
        temperature: number;
        seed: number;
        allowFollowUpPrompts: boolean;
        allowInitialPromptRewrite: boolean;
        allowInitialPromptToHelpUser: boolean;
        useSemanticRanker: boolean;
    }) => void;
}

export function SettingsTab({ isLoading, settings, onSaveSettings }: SettingsTabProps) {
    const styles = useStyles();
    const layoutStyles = useLayoutStyles();
    const [localSettings, setLocalSettings] = useState({
        temperature: "0.7",
        seed: "42",
        allowFollowUpPrompts: true,
        allowInitialPromptRewrite: true,
        allowInitialPromptToHelpUser: true,
        useSemanticRanker: true
    });

    useEffect(() => {
        if (settings) {
            setLocalSettings({
                temperature: settings.temperature?.toString() || "0.7",
                seed: settings.seed?.toString() || "42",
                allowFollowUpPrompts: settings.allowFollowUpPrompts ?? true,
                allowInitialPromptRewrite: settings.allowInitialPromptRewrite ?? true,
                allowInitialPromptToHelpUser: settings.allowInitialPromptToHelpUser ?? true,
                useSemanticRanker: settings.useSemanticRanker ?? true
            });
        }
    }, [settings]);

    const handleSettingsUpdate = () => {
        onSaveSettings({
            temperature: parseFloat(localSettings.temperature),
            seed: parseInt(localSettings.seed),
            allowFollowUpPrompts: localSettings.allowFollowUpPrompts,
            allowInitialPromptRewrite: localSettings.allowInitialPromptRewrite,
            allowInitialPromptToHelpUser: localSettings.allowInitialPromptToHelpUser,
            useSemanticRanker: localSettings.useSemanticRanker
        });
    };

    const handleToggleChange = (settingName: string) => {
        setLocalSettings(prev => ({
            ...prev,
            [settingName]: !prev[settingName as keyof typeof prev]
        }));
    };

    if (isLoading) {
        return <Spinner />;
    }

    return (
        <div className={styles.container}>
            <Card className={styles.settingsCard}>
                <div className={`${styles.settingsForm} ${layoutStyles.scrollContainer}`}>
                    <div className={styles.toggleSection}>
                        <div className={styles.sectionTitle}>LLM Features</div>
                            <Field label="Temperature">
                            <Input
                                type="number"
                                value={localSettings.temperature}
                                onChange={(_e, data) => 
                                    setLocalSettings(prev => ({ ...prev, temperature: data.value }))} 
                                min={0}
                                max={1}
                                step={0.1}
                            />
                        </Field>
                        <Field label="Seed">
                            <Input
                                type="number"
                                value={localSettings.seed}
                                onChange={(_e, data) => 
                                    setLocalSettings(prev => ({ ...prev, seed: data.value }))} 
                            />
                        </Field>
                    </div>
                    

                    <div className={styles.toggleSection}>
                        <div className={styles.sectionTitle}>Chat Features</div>
                        
                        <Field label="Allow Follow-up Prompts">
                            <Switch 
                                checked={localSettings.allowFollowUpPrompts}
                                onChange={() => handleToggleChange('allowFollowUpPrompts')}
                                label={localSettings.allowFollowUpPrompts ? "Enabled" : "Disabled"}
                            />
                        </Field>
                        
                        <Field label="Allow Initial Prompt Rewrite">
                            <Switch 
                                checked={localSettings.allowInitialPromptRewrite}
                                onChange={() => handleToggleChange('allowInitialPromptRewrite')}
                                label={localSettings.allowInitialPromptRewrite ? "Enabled" : "Disabled"}
                            />
                        </Field>
                        
                        <Field label="Allow Initial Prompt to Help User">
                            <Switch 
                                checked={localSettings.allowInitialPromptToHelpUser}
                                onChange={() => handleToggleChange('allowInitialPromptToHelpUser')}
                                label={localSettings.allowInitialPromptToHelpUser ? "Enabled" : "Disabled"}
                            />
                        </Field>
                    </div>

                    <div className={styles.toggleSection}>
                        <div className={styles.sectionTitle}>Search Features</div>
                        
                        <Field label="Use Semantic Ranker">
                            <Switch 
                                checked={localSettings.useSemanticRanker}
                                onChange={() => handleToggleChange('useSemanticRanker')}
                                label={localSettings.useSemanticRanker ? "Enabled" : "Disabled"}
                            />
                        </Field>
                    </div>
                    
                    <Button onClick={handleSettingsUpdate}>Save Settings</Button>
                </div>
            </Card>
        </div>
    );
}