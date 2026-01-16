import axios from 'axios';
import NextAuth, { AuthOptions } from 'next-auth';
import CredentialsProvider from 'next-auth/providers/credentials';

// Validate required environment variables
if (!process.env.NEXTAUTH_SECRET) {
    throw new Error('NEXTAUTH_SECRET is required for NextAuth.js to function properly. Add it to your .env.local file.');
}
if (!process.env.NEXT_PUBLIC_API_URL) {
    throw new Error('NEXT_PUBLIC_API_URL is required for authentication against the backend. Add it to your .env.local file.');
}

export const authOptions: AuthOptions = {
    // Let NextAuth handle cookie naming automatically
    // In production (HTTPS), it will use __Secure-next-auth.session-token
    // In development (HTTP), it will use next-auth.session-token
    providers: [
        CredentialsProvider({
            name: 'Credentials',
            credentials: {
                email: { label: "Email", type: "text", placeholder: "test@example.com" },
                password: { label: "Password", type: "password" }
            },
            async authorize(credentials) {
                if (!credentials?.email || !credentials?.password) {
                    return null;
                }

                try {
                    // Send login request to the .NET backend
                    const apiUrl = process.env.NEXT_PUBLIC_API_URL;
                    if (!apiUrl) {
                        console.error(' [Authorize] NEXT_PUBLIC_API_URL is not configured. Cannot authenticate.');
                        return null;
                    }
                    const response = await axios.post(`${apiUrl}/auth/login`, {
                        email: credentials.email,
                        password: credentials.password,
                        context: "customer" // Specify context for unified auth system
                    });

                    // Backend returns { success: true, data: {...} }, so we need response.data.data
                    const data = response.data.data;
                    if (data && data.accessToken) {
                        console.log(" [Authorize] Backend response OK:", {
                            userId: data.user.id,
                            email: data.user.email,
                            roles: data.user.roles, // Reading from data.user.roles
                            accessToken: !!data.accessToken,
                            refreshToken: !!data.refreshToken,
                            expiry: data.expiresAt, // Backend returns 'expiresAt'
                        });
                        return {
                            id: data.user.id,
                            email: data.user.email,
                            name: `${data.user.firstName} ${data.user.lastName}`,
                            firstName: data.user.firstName,
                            lastName: data.user.lastName,
                            profilePictureUrl: data.user.profilePictureUrl,
                            roles: data.user.roles, // Roles are in data.user.roles
                            accessToken: data.accessToken,
                            refreshToken: data.refreshToken,
                            accessTokenExpiry: data.expiresAt, // Backend returns 'expiresAt'
                        };
                    }

                    console.log(" [Authorize] Backend response invalid or missing token.");
                    return null;
                } catch (error: unknown) {
                    if (axios.isAxiosError(error)) {
                        console.error(" [Authorize] Axios error:", {
                            status: error.response?.status,
                            data: error.response?.data,
                            message: error.message,
                        });
                    } else {
                        console.error(" [Authorize] Unknown error:", error instanceof Error ? error.message : "Unknown error");
                    }
                    return null;
                }
            }
        })
    ],

    // Callbacks are used to control what happens to the JWT and session objects.
    callbacks: {
        async jwt({ token, user, trigger }) {
            const timestamp = new Date().toISOString();
            console.log(`=== JWT CALLBACK DETAILED DEBUG [${timestamp}] ===`);
            console.log("Trigger reason:", trigger);
            console.log("Has user (initial sign in):", !!user);
            console.log("Current token keys:", Object.keys(token));

            // If this is the initial sign in, save the access token and refresh token
            if (user) {
                console.log(`[${timestamp}] JWT callback - initial sign in, saving tokens`);
                token.accessToken = user.accessToken;
                token.refreshToken = user.refreshToken;
                token.accessTokenExpiry = user.accessTokenExpiry;
                token.roles = user.roles;
                token.firstName = user.firstName;
                token.lastName = user.lastName;
                token.profilePictureUrl = (user as { profilePictureUrl?: string }).profilePictureUrl;
                console.log(`[${timestamp}] JWT callback - initial sign in, tokens saved`);
                console.log("Token expiry set to:", token.accessTokenExpiry);
                return token;
            }

            // Don't attempt refresh if we don't have tokens or there's already an error
            if (!token.accessToken || !token.refreshToken || token.error) {
                console.log(`[${timestamp}] JWT callback - skipping refresh due to missing tokens or existing error`);
                console.log("Missing access token:", !token.accessToken);
                console.log("Missing refresh token:", !token.refreshToken);
                console.log("Existing error:", token.error);
                return token;
            }

            // Check if the access token has expired
            const now = Date.now();
            const expiryTime = new Date(token.accessTokenExpiry as string).getTime();
            const timeUntilExpiry = expiryTime - now;

            console.log(`[${timestamp}] JWT callback - checking token expiry`);
            console.log("Current time:", new Date(now).toISOString());
            console.log("Token expires at:", token.accessTokenExpiry);
            console.log("Time until expiry (minutes):", Math.round(timeUntilExpiry / (1000 * 60)));

            // Check if the access token is within the refresh threshold (e.g., 5 minutes before expiry)
            // Or if it has already expired.
            const refreshThreshold = 5 * 60 * 1000; // 5 minutes
            if (token.accessTokenExpiry && (timeUntilExpiry < refreshThreshold)) {
                console.log(`[${timestamp}] Access token expired or within refresh threshold, attempting refresh...`);
                try {
                    console.log(`[${timestamp}] Making refresh request to backend...`);
                    // Attempt to refresh the token
                    const apiUrl = process.env.NEXT_PUBLIC_API_URL;
                    if (!apiUrl) {
                        console.error(`[${timestamp}] NEXT_PUBLIC_API_URL is not configured. Skipping token refresh.`);
                        return token;
                    }
                    const response = await axios.post(`${apiUrl}/auth/refresh`, {
                        refreshToken: token.refreshToken,
                        context: "customer" // Specify context for unified auth system
                    });

                    // Backend returns { success: true, data: {...} }, so we need response.data.data
                    const data = response.data.data;
                    console.log(`[${timestamp}] Token refresh successful, received data:`, {
                        accessToken: !!data.accessToken,
                        refreshToken: !!data.refreshToken,
                        expiry: data.expiresAt, // Backend returns 'expiresAt', not 'accessTokenExpiry'
                    });
                    // Update token with new values
                    token.accessToken = data.accessToken;
                    token.refreshToken = data.refreshToken;
                    token.accessTokenExpiry = data.expiresAt; // Backend returns 'expiresAt'

                    // Clear any previous error
                    delete token.error;

                    console.log(`[${timestamp}] Token refreshed successfully, new expiry:`, data.accessTokenExpiry);

                } catch (error: any) {
                    console.error(`[${timestamp}] Token refresh failed:`, error?.response?.status, error?.response?.data || error.message);
                    console.log(`[${timestamp}] Full error response:`, {
                        status: error?.response?.status,
                        data: error?.response?.data,
                        message: error.message,
                        headers: error?.response?.headers
                    });

                    // If it's a 401, the refresh token is invalid - clear everything
                    if (error?.response?.status === 401) {
                        console.log(`[${timestamp}] Refresh token invalid (401), backend message:`, error.response.data?.error || 'No details');
                        console.log(`[${timestamp}] Clearing session due to invalid refresh token`);
                        return {
                            error: "RefreshAccessTokenError"
                        };
                    }

                    // For other errors, just mark as error but keep trying
                    console.log(`[${timestamp}] Non-401 error (${error?.response?.status}), marking token with error but keeping existing data`);
                    console.log(`[${timestamp}] Error details:`, error.response?.data);
                    return {
                        ...token,
                        error: "RefreshAccessTokenError"
                    };
                }
            } else {
                console.log(`[${timestamp}] Token still valid, no refresh needed`);
            }

            console.log(`[${timestamp}] JWT callback - returning token`);
            return token;
        },

        async session({ session, token, trigger }) {
            const timestamp = new Date().toISOString();
            const callStack = new Error().stack?.split('\n').slice(1, 6).join('\n') || 'No stack trace';

            // If there's a refresh error, return an expired session object
            if (token.error === "RefreshAccessTokenError" || !token.accessToken || !token.refreshToken) {
                console.log(` [${timestamp}] Session callback - refresh error or missing tokens, returning expired session`);
                console.log("Error details:", token.error);
                console.log("Missing tokens - Access:", !token.accessToken, "Refresh:", !token.refreshToken);
                return {
                    ...session,
                    user: undefined,
                    expires: new Date(0).toISOString(), // Expired session
                    accessToken: undefined,
                    refreshToken: undefined,
                    error: token.error,
                };
            }

            console.log(` [${timestamp}] Session callback - creating valid session`);

            // Send properties to the client
            session.accessToken = token.accessToken as string;
            session.refreshToken = token.refreshToken as string;
            session.error = token.error as string;

            if (session.user) {
                session.user.id = token.sub!;
                session.user.roles = token.roles as string[];
                session.user.firstName = token.firstName as string;
                session.user.lastName = token.lastName as string;
                session.user.profilePictureUrl = token.profilePictureUrl as string;
            }

            console.log(` [${timestamp}] Session returned successfully for user:`, session.user?.email);
            return session;
        },
    },

    pages: {
        signIn: '/auth/login',
        error: '/auth/error',
    },

    session: {
        strategy: 'jwt',
        maxAge: 24 * 60 * 60, // 1 day instead of 30 days to reduce persistence
    },

    events: {
        async signOut(message) {
            console.log("NextAuth signOut event triggered:", message);
        },
    },

    secret: process.env.NEXTAUTH_SECRET,
};

const handler = NextAuth(authOptions);
export { handler as GET, handler as POST };

