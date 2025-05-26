import { makeStyles, tokens } from '@fluentui/react-components';

// Common layout styles used across components
export const useLayoutStyles = makeStyles({
  flexColumn: {
    display: 'flex',
    flexDirection: 'column',
  },
  flexRow: {
    display: 'flex',
    flexDirection: 'row',
  },
  centered: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
  },
  spaceBetween: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  fullWidth: {
    width: '100%',
  },
  fullHeight: {
    height: '100%',
  },
  scrollContainer: {
    overflowY: 'auto',
    '&::-webkit-scrollbar': {
      display: 'none'
    },
    scrollbarWidth: 'none', // Firefox
    msOverflowStyle: 'none', // IE/Edge
  },
});

// Common card and container styles
export const useContainerStyles = makeStyles({
  card: {
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalM,
    boxShadow: tokens.shadow2,
  },
  section: {
    marginBottom: tokens.spacingVerticalL,
  },
  dialog: {
    width: '90%',
    maxWidth: '600px',
  },
});

// Common text styles
export const useTextStyles = makeStyles({
  title: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
  },
  subtitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
  },
  body: {
    fontSize: tokens.fontSizeBase200,
  },
  caption: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
  truncate: {
    whiteSpace: 'nowrap',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
  },
});

// Common spacing values
export const spacing = {
  xs: tokens.spacingHorizontalXS,
  s: tokens.spacingHorizontalS,
  m: tokens.spacingHorizontalM,
  l: tokens.spacingHorizontalL,
  xl: tokens.spacingHorizontalXL,
};

// Common responsive breakpoints
export const breakpoints = {
  mobile: '@media (max-width: 599px)',
  tablet: '@media (min-width: 600px) and (max-width: 1023px)',
  desktop: '@media (min-width: 1024px)',
};

// Animation utilities following Fluent 2 motion guidelines
export const motion = {
  // Duration values
  duration: {
    fast: '100ms',
    normal: '200ms',
    moderate: '300ms',
    slow: '400ms',
    slower: '500ms',
  },
  // Easing functions
  easing: {
    accelerate: 'cubic-bezier(0.9, 0.1, 1, 0.2)',
    decelerate: 'cubic-bezier(0.1, 0.9, 0.2, 1)',
    standard: 'cubic-bezier(0.8, 0, 0.2, 1)',
    emphasized: 'cubic-bezier(0.1, 0.9, 0.2, 1)',
  },
  // Common transitions
  createTransition: (properties: string[], 
                     duration = '200ms', 
                     easing = 'cubic-bezier(0.8, 0, 0.2, 1)') => {
    return properties.map(prop => `${prop} ${duration} ${easing}`).join(', ');
  },
};

// Accessibility helper styles
export const useA11yStyles = makeStyles({
  focusVisible: {
    '&:focus-visible': {
      outline: `2px solid ${tokens.colorBrandStroke1}`,
      outlineOffset: '2px',
    },
  },
  srOnly: {
    position: 'absolute',
    width: '1px',
    height: '1px',
    padding: '0',
    margin: '-1px',
    overflow: 'hidden',
    clip: 'rect(0, 0, 0, 0)',
    whiteSpace: 'nowrap',
  },
});