import { 
  TabList, 
  Tab, 
  tokens,
  makeStyles,
  Switch,
  Tooltip,
  Divider
} from "@fluentui/react-components";
import { 
  WeatherMoon20Regular, 
  WeatherSunny20Regular
} from '@fluentui/react-icons';
import { ChatPage } from './pages/ChatPage';
import { AdminPage } from './pages/AdminPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MsalProvider, MsalAuthenticationTemplate } from "@azure/msal-react";
import { Configuration, PublicClientApplication, InteractionType } from "@azure/msal-browser";
import { ThemeProvider, useTheme } from "./components/ThemeContext";
import { env } from "./config/env";
import { BrowserRouter, Routes, Route, useNavigate, useLocation } from 'react-router-dom';
import { useState } from 'react';

const queryClient = new QueryClient()

const configuration: Configuration = {
  auth: {
    clientId: env.PUBLIC_APP_ID,
    authority: env.PUBLIC_AUTHORITY_URL,
    redirectUri: "/",
    postLogoutRedirectUri: "/",
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false, // Set this to "true" if you are having issues on IE11 or Edge
  },
};
const pca = new PublicClientApplication(configuration);

const authRequest = {
  scopes: ["openid", "profile", env.BACKEND_SCOPE]
};

// Navigation styles
const useNavStyles = makeStyles({
  appRoot: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    overflow: 'hidden',
    margin: 0,
    padding: 0,
  },
  navContainer: {
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalM}`, // further reduced vertical padding
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    backdropFilter: 'blur(10px)',
    position: 'sticky',
    top: 0,
    zIndex: 100,
    backgroundColor: tokens.colorNeutralBackground1,
    boxShadow: tokens.shadow4,
    flexShrink: 0, // prevent shrinking
  },
  contentContainer: {
    flex: 1,
    overflow: 'hidden', // let child components handle scrolling
    display: 'flex',
    flexDirection: 'column',
    minHeight: 0, // critical fix for flex overflow
  },
  appTitle: {
    margin: 0,
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
    display: 'flex',
    alignItems: 'center',
    paddingLeft: tokens.spacingHorizontalM,
  },
  controls: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  themeToggle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  settingsItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  versionToggle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  }
});

// Theme Toggle component
function ThemeToggle() {
  const { darkMode, setDarkMode } = useTheme();
  const navStyles = useNavStyles();
  
  return (
    <div className={navStyles.themeToggle}>
      <WeatherSunny20Regular />
      <Tooltip content={darkMode ? "Switch to light mode" : "Switch to dark mode"} relationship="label">
        <Switch 
          checked={darkMode}
          onChange={(_, data) => setDarkMode(data.checked)}
          aria-label="Theme toggle"
        />
      </Tooltip>
      <WeatherMoon20Regular />
    </div>
  );
}

// Commented out as unused, but kept for future reference
/*
function SettingsMenu() {
  const { highContrastMode, setHighContrastMode } = useTheme();
  const navStyles = useNavStyles();
  
  return (
    <Menu>
      <MenuTrigger disableButtonEnhancement>
        <Button 
          icon={<Settings24Regular />} 
          appearance="subtle"
          aria-label="Settings"
        />
      </MenuTrigger>
      <MenuPopover>
        <MenuList>
          <MenuItem>
            <div className={navStyles.settingsItem}>
              <AccessibilityCheckmark24Regular />
              <span>High contrast mode</span>
              <Switch 
                checked={highContrastMode}
                onChange={(_, data) => setHighContrastMode(data.checked)}
                aria-label="High contrast mode"
              />
            </div>
          </MenuItem>
        </MenuList>
      </MenuPopover>
    </Menu>
  );
}
*/

// Main navigation component
function AppNavigation() {
  const navStyles = useNavStyles();
  const location = useLocation();
  const navigate = useNavigate();
  const [isV2, setIsV2] = useState(false);

  const handleTabSelect = (_: React.SyntheticEvent, data: { value: unknown }) => {
    navigate(data.value as string);
  };

  const toggleVersion = () => {
    setIsV2((prev) => !prev);
  };

  return (
    <div className={navStyles.appRoot}>
      <div className={navStyles.navContainer}>
        <h1 className={navStyles.appTitle}>Chat with Your Data</h1>
        <div className={navStyles.controls}>
          <ThemeToggle />
          <Divider vertical />
          <TabList
            selectedValue={location.pathname}
            onTabSelect={handleTabSelect}
            appearance="subtle"
          >
            <Tab value="/">Chat</Tab>
            <Tab value="/admin">Admin</Tab>
          </TabList>
          
        </div>
      </div>
      <div className={navStyles.contentContainer}>
        <Routes>
          <Route path="/" element={<ChatPage />} />
          <Route path="/admin" element={<AdminPage />} />
        </Routes>
      </div>
    </div>
  );
}

function App() {
  return (
    <MsalProvider instance={pca}>
      <MsalAuthenticationTemplate
        interactionType={InteractionType.Redirect}
        authenticationRequest={authRequest}
      >
        <QueryClientProvider client={queryClient}>
          <ThemeProvider>
            <BrowserRouter>
              <AppNavigation />
            </BrowserRouter>
          </ThemeProvider>
        </QueryClientProvider>
      </MsalAuthenticationTemplate>
    </MsalProvider>
  );
}

export default App;
