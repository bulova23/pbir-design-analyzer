import * as vscode from 'vscode';

/**
 * Interface for the Power BI authentication session
 */
export interface PowerBISession {
    accessToken: string;
    account: vscode.AuthenticationSession['account'];
}

/**
 * Handles authentication for Power BI using VS Code's built-in provider
 */
export class AuthManager {
    private static readonly PROVIDER_ID = 'microsoft';
    // Use the resource's default scope so VS Code can reuse a single session
    // This avoids repeated prompts caused by mismatched delegated scopes
    private static readonly SCOPES = [
        'https://analysis.windows.net/powerbi/api/.default'
    ];

    /**
     * Gets a token for Power BI Analysis Services
     * @param createIfNone If true, will prompt the user to sign in if no session exists
     */
    public static async getToken(createIfNone: boolean = false): Promise<string | undefined> {
        try {
            const session = await vscode.authentication.getSession(
                this.PROVIDER_ID,
                this.SCOPES,
                { createIfNone }
            );

            return session?.accessToken;
        } catch (error) {
            console.error('Error getting Power BI token:', error);
            return undefined;
        }
    }

    /**
     * Checks if the user is authenticated
     */
    public static async isAuthenticated(): Promise<boolean> {
        const session = await vscode.authentication.getSession(
            this.PROVIDER_ID,
            this.SCOPES,
            { createIfNone: false }
        );
        return !!session;
    }

    /**
     * Requests user to sign in
     */
    public static async signIn(): Promise<string | undefined> {
        return this.getToken(true);
    }
}
