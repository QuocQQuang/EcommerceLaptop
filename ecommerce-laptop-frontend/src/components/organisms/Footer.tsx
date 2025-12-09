import { Facebook, Instagram, Twitter, Youtube } from 'lucide-react';
import Link from 'next/link';

export function Footer() {
    return (
        <footer className="bg-black text-gray-300 border-t border-gray-800">
            <div className="container mx-auto px-4 py-8">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                    <div className="space-y-4">
                        <h3 className="text-lg font-semibold text-white">LaptopStore</h3>
                        <p className="text-gray-400 text-sm">Premium laptops for the future.</p>
                    </div>
                    <div className="space-y-4">
                        <h3 className="text-lg font-semibold text-white">Links</h3>
                        <ul className="space-y-2">
                            <li><Link href="/about" className="text-gray-400 hover:text-white transition-colors text-sm">About</Link></li>
                            <li><Link href="/support" className="text-gray-400 hover:text-white transition-colors text-sm">Support</Link></li>
                            <li><Link href="/privacy" className="text-gray-400 hover:text-white transition-colors text-sm">Privacy</Link></li>
                            <li><Link href="/terms" className="text-gray-400 hover:text-white transition-colors text-sm">Terms</Link></li>
                        </ul>
                    </div>
                    <div className="space-y-4">
                        <h3 className="text-lg font-semibold text-white">Follow Us</h3>
                        <div className="flex gap-4">
                            <Link href="https://facebook.com" className="text-gray-400 hover:text-white transition-colors"><Facebook className="w-5 h-5" /></Link>
                            <Link href="https://youtube.com" className="text-gray-400 hover:text-white transition-colors"><Youtube className="w-5 h-5" /></Link>
                            <Link href="https://instagram.com" className="text-gray-400 hover:text-white transition-colors"><Instagram className="w-5 h-5" /></Link>
                            <Link href="https://twitter.com" className="text-gray-400 hover:text-white transition-colors"><Twitter className="w-5 h-5" /></Link>
                        </div>
                    </div>
                </div>
                <div className="border-t border-gray-800 mt-8 pt-6 text-center text-sm text-gray-400">
                     2024 LaptopStore. All rights reserved.
                </div>
            </div>
        </footer>
    );
}