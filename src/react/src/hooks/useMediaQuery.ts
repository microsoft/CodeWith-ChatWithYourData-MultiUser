import { useEffect, useState } from 'react';

/**
 * Custom hook for responsive media queries
 * @param query - CSS media query string (e.g., '(max-width: 600px)')
 * @returns boolean indicating whether the query matches
 */
export const useMediaQuery = (query: string): boolean => {
  const [matches, setMatches] = useState<boolean>(() => {
    // Initialize with current window state if in browser environment
    if (typeof window !== 'undefined') {
      return window.matchMedia(query).matches;
    }
    return false;
  });

  useEffect(() => {
    // Return early if not in browser environment
    if (typeof window === 'undefined') {
      return;
    }

    // Create a media query list
    const mediaQueryList = window.matchMedia(query);

    // Update state when matches change
    const updateMatches = (event: MediaQueryListEvent) => {
      setMatches(event.matches);
    };

    // Add listener for changes
    // Modern browsers use addEventListener, older use addListener
    if (mediaQueryList.addEventListener) {
      mediaQueryList.addEventListener('change', updateMatches);
    } else {
      // @ts-ignore - For older browsers
      mediaQueryList.addListener(updateMatches);
    }

    // Initial check
    setMatches(mediaQueryList.matches);

    // Cleanup
    return () => {
      if (mediaQueryList.removeEventListener) {
        mediaQueryList.removeEventListener('change', updateMatches);
      } else {
        // @ts-ignore - For older browsers
        mediaQueryList.removeListener(updateMatches);
      }
    };
  }, [query]);

  return matches;
};
