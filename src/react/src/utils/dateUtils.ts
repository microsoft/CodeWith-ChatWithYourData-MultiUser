// filepath: d:\repos\codewith\codewith-philips\CodeWith-ChatWithYourData-MultiUser\src\react\src\utils\dateUtils.ts
/**
 * Format date to a readable format
 * @param date Date object to format
 * @returns Formatted date string
 */
export function formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    }).format(date);
}