/**
 * Simple logger for the extension
 */

export const logger = {
    info: (msg: string, ...args: any[]) => console.log(`[PowerBI] ℹ️  ${msg}`, ...args),
    warn: (msg: string, ...args: any[]) => console.warn(`[PowerBI] ⚠️  ${msg}`, ...args),
    error: (msg: string, ...args: any[]) => console.error(`[PowerBI] ❌ ${msg}`, ...args),
    debug: (msg: string, ...args: any[]) => console.debug(`[PowerBI] 🐛 ${msg}`, ...args)
};
