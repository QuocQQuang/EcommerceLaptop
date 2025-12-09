'use client';

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { CurrencySelector } from '@/components/ui/currency-selector';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { useAuth } from '@/hooks/useAuth';
import { useCartStore } from '@/store/cartStore';
import { useUIStore } from '@/store/uiStore';
import { useWishlistStore } from '@/store/wishlistStore';
import { Heart, LogIn, LogOut, Menu, ShoppingCart, User } from 'lucide-react';
import { signOut } from 'next-auth/react';
import Link from 'next/link';
import * as React from 'react';

export function Header() {
    const { setMobileMenuOpen } = useUIStore();
    const { user, session, isAuthenticated } = useAuth();
    const { itemCount } = useCartStore();
    const { items: wishlistItems } = useWishlistStore();

    // Hydration-safe rendering for client-only counts
    const [mounted, setMounted] = React.useState(false);
    React.useEffect(() => {
        setMounted(true);
    }, []);

    const handleLogout = async () => {
        await signOut({ redirect: false });
        window.location.href = '/';
    };

    return (
        <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur-xl supports-[backdrop-filter]:bg-background/60">
            <div className="container flex h-16 max-w-screen-2xl items-center">
                {/* Mobile Menu Button */}
                <Button
                    variant="ghost"
                    size="sm"
                    className="mr-4 md:hidden"
                    onClick={() => setMobileMenuOpen(true)}
                >
                    <Menu className="h-5 w-5" />
                    <span className="sr-only">Toggle menu</span>
                </Button>

                {/* Logo */}
                <Link href="/" className="mr-6 flex items-center group">
                    {/* Mobile: compact logo */}
                    <span className="inline-flex items-center select-none md:hidden font-extrabold text-base leading-none tracking-tight">
                        <span className="bg-gradient-to-r from-primary to-primary/70 bg-clip-text text-transparent">L</span>
                        <span className="text-foreground/95 italic">S</span>
                    </span>

                    {/* Desktop: full logo, larger size */}
                    <span className="hidden md:inline-flex select-none font-extrabold leading-none tracking-tight text-xl md:text-2xl">
                        <span className="bg-gradient-to-r from-primary to-primary/70 bg-clip-text text-transparent">Laptop</span>
                        <span className="text-foreground/95 italic ml-[2px]">Store</span>
                    </span>

                    <span className="sr-only">LaptopStore</span>
                </Link>

                {/* Navigation - Desktop */}
                <nav className="hidden items-center space-x-6 text-sm font-medium md:flex flex-1 justify-center">
                    <Link
                        href="/"
                        className="transition-colors text-muted-foreground hover:text-foreground"
                    >
                        Trang ch
                    </Link>
                    <Link
                        href="/products"
                        className="transition-colors text-muted-foreground hover:text-foreground"
                    >
                        Sn phm
                    </Link>
                    <Link
                        href="/blog"
                        className="transition-colors text-muted-foreground hover:text-foreground"
                    >
                        Blog
                    </Link>
                    <Link
                        href="/features"
                        className="transition-colors text-muted-foreground hover:text-foreground"
                    >
                        Tnh nng
                    </Link>
                    <Link
                        href="/reviews"
                        className="transition-colors text-muted-foreground hover:text-foreground"
                    >
                        nh gi
                    </Link>
                    <Link
                        href="/contact"
                        className="transition-colors text-muted-foreground hover:text-foreground"
                    >
                        Lin h
                    </Link>
                </nav>

                {/* User Actions - Desktop */}
                <div className="hidden md:flex items-center space-x-4">
                    {/* Currency Selector */}
                    <CurrencySelector variant="simple" showRefreshButton={false} />

                    {/* Wishlist */}
                    <Link href="/wishlist" className="relative p-2 text-muted-foreground hover:text-foreground transition-colors">
                        <Heart className="h-5 w-5" />
                        {mounted && wishlistItems.length > 0 && (
                            <span className="absolute -top-1 -right-1 bg-primary text-primary-foreground text-xs rounded-full h-5 w-5 flex items-center justify-center">
                                {wishlistItems.length > 99 ? '99+' : wishlistItems.length}
                            </span>
                        )}
                    </Link>

                    {/* Cart - Hidden on mobile, shown on desktop */}
                    <Link href="/cart" className="relative p-2 text-muted-foreground hover:text-foreground transition-colors hidden md:block">
                        <ShoppingCart className="h-5 w-5" />
                        {mounted && itemCount > 0 && (
                            <span className="absolute -top-1 -right-1 bg-primary text-primary-foreground text-xs rounded-full h-5 w-5 flex items-center justify-center">
                                {itemCount > 99 ? '99+' : itemCount}
                            </span>
                        )}
                    </Link>

                    {/* User Dropdown */}
                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Button
                                variant="ghost"
                                className="relative h-8 w-8 rounded-full"
                                suppressHydrationWarning
                            >
                                <Avatar className="h-8 w-8">
                                    <AvatarImage src={user?.profilePictureUrl || ''} alt={`${user?.firstName} ${user?.lastName}`} />
                                    <AvatarFallback>
                                        <User className="h-4 w-4" />
                                    </AvatarFallback>
                                </Avatar>
                            </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent className="w-56" align="end" forceMount>
                            <DropdownMenuLabel className="font-normal">
                                <div className="flex flex-col space-y-1">
                                    {isAuthenticated ? (
                                        <>
                                            <p className="text-sm font-medium leading-none">{`${user?.firstName || ''} ${user?.lastName || ''}`.trim() || 'User'}</p>
                                            <p className="text-xs leading-none text-muted-foreground">{user?.email || ''}</p>
                                        </>
                                    ) : (
                                        <p className="text-sm">Cha ng nhp</p>
                                    )}
                                </div>
                            </DropdownMenuLabel>
                            <DropdownMenuSeparator />
                            {isAuthenticated ? (
                                <>
                                    <DropdownMenuItem asChild>
                                        <Link href="/account">Ti khon</Link>
                                    </DropdownMenuItem>
                                    <DropdownMenuItem asChild>
                                        <Link href="/account/orders">n hng</Link>
                                    </DropdownMenuItem>
                                    <DropdownMenuSeparator />
                                    <DropdownMenuItem
                                        onSelect={handleLogout}
                                        className="text-danger focus:text-danger"
                                    >
                                        <LogOut className="mr-2 h-4 w-4" />
                                        <span>ng xut</span>
                                    </DropdownMenuItem>
                                </>
                            ) : (
                                <>
                                    <DropdownMenuItem asChild>
                                        <Link href="/auth/login">
                                            <LogIn className="mr-2 h-4 w-4" />
                                            <span>ng nhp</span>
                                        </Link>
                                    </DropdownMenuItem>
                                    <DropdownMenuItem asChild>
                                        <Link href="/auth/register">
                                            <User className="mr-2 h-4 w-4" />
                                            <span>ng k</span>
                                        </Link>
                                    </DropdownMenuItem>
                                </>
                            )}
                        </DropdownMenuContent>
                    </DropdownMenu>
                </div>
            </div>
        </header>
    );
}