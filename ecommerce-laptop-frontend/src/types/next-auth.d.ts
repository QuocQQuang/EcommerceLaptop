import "next-auth";

declare module "next-auth" {
    interface Session {
        accessToken?: string;
        refreshToken?: string;
        error?: string;
        user: {
            id: string;
            email: string;
            name: string;
            firstName: string;
            lastName: string;
            profilePictureUrl?: string;
            roles: string[];
        };
    }

    interface User {
        id: string;
        email: string;
        name: string;
        firstName: string;
        lastName: string;
        profilePictureUrl?: string;
        roles: string[];
        accessToken: string;
        refreshToken: string;
        accessTokenExpiry: string;
    }
}

declare module "next-auth/jwt" {
    interface JWT {
        accessToken?: string;
        refreshToken?: string;
        accessTokenExpiry?: string;
        roles?: string[];
        firstName?: string;
        lastName?: string;
        profilePictureUrl?: string;
        error?: string;
    }
}