import { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Email Confirmation | Ecommerce Laptop',
    description: 'Confirm your email address to activate your account and start shopping.',
    robots: 'noindex, nofollow', // Don't index confirmation pages
};

export default function ConfirmEmailLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    return children;
}