import React, { createContext, useContext, useState, ReactNode, useEffect } from 'react';
import { 
  FluentProvider, 
  webDarkTheme, 
  webLightTheme, 
  Theme, 
  createDarkTheme, 
  createLightTheme,
  BrandVariants
} from '@fluentui/react-components';

// Define your brand colors - example using blue as primary
const brandRamp: BrandVariants = {
  10: '#001122',
  20: '#002244',
  30: '#003366',
  40: '#004488',
  50: '#0055AA',
  60: '#0066CC', // Primary brand color
  70: '#2277DD',
  80: '#4488EE',
  90: '#66AAFF',
  100: '#88BBFF',
  110: '#AACCFF',
  120: '#CCDDFF',
  130: '#EEEEFF',
  140: '#F5F5FF',
  150: '#FAFAFF',
  160: '#FFFFFF',
};

// Create custom brand themes
const darkBrandTheme = createDarkTheme(brandRamp);
const lightBrandTheme = createLightTheme(brandRamp);

// Interface for theme context
type ThemeContextType = {
  darkMode: boolean;
  setDarkMode: (value: boolean) => void;
  currentTheme: Theme;
  highContrastMode: boolean;
  setHighContrastMode: (value: boolean) => void;
};

const ThemeContext = createContext<ThemeContextType>({
  darkMode: false,
  setDarkMode: () => {},
  currentTheme: webLightTheme,
  highContrastMode: false,
  setHighContrastMode: () => {}
});

export const useTheme = () => useContext(ThemeContext);

export const ThemeProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  // Try to get user preference from localStorage or system preference
  const prefersDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
  const [darkMode, setDarkMode] = useState(() => {
    const savedMode = localStorage.getItem('darkMode');
    return savedMode ? JSON.parse(savedMode) : prefersDarkMode;
  });
  const [highContrastMode, setHighContrastMode] = useState(() => {
    const savedMode = localStorage.getItem('highContrastMode');
    return savedMode ? JSON.parse(savedMode) : false;
  });

  // Select the appropriate theme based on user preferences
  const getTheme = (): Theme => {
    if (highContrastMode) {
      // Use high contrast themes when needed
      return darkMode 
        ? { ...webDarkTheme, colorNeutralForeground1: '#FFFFFF', colorNeutralBackground1: '#000000' }
        : { ...webLightTheme, colorNeutralForeground1: '#000000', colorNeutralBackground1: '#FFFFFF' };
    }
    
    // Use brand themes
    return darkMode ? darkBrandTheme : lightBrandTheme;
  };

  const currentTheme = getTheme();

  // Persist preferences to localStorage
  useEffect(() => {
    localStorage.setItem('darkMode', JSON.stringify(darkMode));
  }, [darkMode]);

  useEffect(() => {
    localStorage.setItem('highContrastMode', JSON.stringify(highContrastMode));
  }, [highContrastMode]);

  return (
    <ThemeContext.Provider value={{ 
      darkMode, 
      setDarkMode, 
      currentTheme,
      highContrastMode, 
      setHighContrastMode 
    }}>
      <FluentProvider theme={currentTheme} style={{ height: '100%' }}>
        {children}
      </FluentProvider>
    </ThemeContext.Provider>
  );
};