/**
 * LoggingService.ts
 * Service to manage application-wide logging settings
 */

// Event name for logging state changes
const LOGGING_CHANGE_EVENT = 'loggingStateChanged';

// Local storage key for persisting logging preference
const LOGGING_ENABLED_KEY = 'app_verbose_logging_enabled';

class LoggingService {
  private static instance: LoggingService;
  private loggingEnabled: boolean;
  private eventTarget: EventTarget;

  private constructor() {
    this.eventTarget = new EventTarget();
    
    // Initialize from localStorage or default to false
    const storedValue = localStorage.getItem(LOGGING_ENABLED_KEY);
    this.loggingEnabled = storedValue ? JSON.parse(storedValue) : false;
  }

  public static getInstance(): LoggingService {
    if (!LoggingService.instance) {
      LoggingService.instance = new LoggingService();
    }
    return LoggingService.instance;
  }

  /**
   * Get current logging state
   */
  public isLoggingEnabled(): boolean {
    return this.loggingEnabled;
  }

  /**
   * Toggle logging state
   */
  public setLoggingEnabled(enabled: boolean): void {
    if (this.loggingEnabled !== enabled) {
      this.loggingEnabled = enabled;
      
      // Persist setting to localStorage
      localStorage.setItem(LOGGING_ENABLED_KEY, JSON.stringify(enabled));
      
      // Dispatch event to notify subscribers
      this.eventTarget.dispatchEvent(new CustomEvent(LOGGING_CHANGE_EVENT));
    }
  }

  /**
   * Subscribe to logging state changes
   */
  public subscribeToLoggingChanges(callback: () => void): () => void {
    const handler = () => callback();
    this.eventTarget.addEventListener(LOGGING_CHANGE_EVENT, handler);
    
    // Return unsubscribe function
    return () => {
      this.eventTarget.removeEventListener(LOGGING_CHANGE_EVENT, handler);
    };
  }

  /**
   * Conditionally log messages based on current logging state
   */
  public log(...args: any[]): void {
    if (this.loggingEnabled) {
      console.log(...args);
    }
  }

  /**
   * Conditionally log warnings based on current logging state
   */
  public warn(...args: any[]): void {
    if (this.loggingEnabled) {
      console.warn(...args);
    }
  }

  /**
   * Always log errors, regardless of logging state
   */
  public error(...args: any[]): void {
    // Errors are always logged
    console.error(...args);
  }
}

export default LoggingService.getInstance();
