'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { Download, ExternalLink, Image as ImageIcon, Loader2, Search } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface UnsplashImage {
    id: string;
    urls: {
        small: string;
        regular: string;
        full: string;
    };
    alt_description: string;
    description: string;
    user: {
        name: string;
        username: string;
        links: {
            html: string;
        };
    };
    links: {
        html: string;
        download: string;
    };
    width: number;
    height: number;
}

interface UnsplashImagePickerProps {
    onImageSelect: (imageUrl: string, altText: string, attribution: string) => void;
    trigger?: React.ReactNode;
}

export default function UnsplashImagePicker({ onImageSelect, trigger }: UnsplashImagePickerProps) {
    const [open, setOpen] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [images, setImages] = useState<UnsplashImage[]>([]);
    const [loading, setLoading] = useState(false);
    const [selectedImage, setSelectedImage] = useState<UnsplashImage | null>(null);
    const [customAltText, setCustomAltText] = useState('');
    const [isUsingMockData, setIsUsingMockData] = useState(false);

    // Popular search terms for tech/laptop related images
    const popularTerms = [
        'laptop', 'computer', 'technology', 'office', 'workspace',
        'coding', 'programming', 'business', 'modern', 'minimal'
    ];

    const searchImages = async (query: string) => {
        if (!query.trim()) return;

        setLoading(true);
        try {
            // Unsplash API configuration
            const UNSPLASH_ACCESS_KEY = process.env.NEXT_PUBLIC_UNSPLASH_ACCESS_KEY || 'YOUR_UNSPLASH_ACCESS_KEY';
            const UNSPLASH_API_URL = 'https://api.unsplash.com/search/photos';

            // If no access key is provided, fall back to mock data
            if (UNSPLASH_ACCESS_KEY === 'YOUR_UNSPLASH_ACCESS_KEY') {
                console.warn('Unsplash Access Key not configured. Using mock data.');
                setIsUsingMockData(true);
                const mockImages: UnsplashImage[] = Array.from({ length: 12 }, (_, i) => ({
                    id: `${query}-${i}`,
                    urls: {
                        small: `https://picsum.photos/400/300?random=${i}&query=${encodeURIComponent(query)}`,
                        regular: `https://picsum.photos/800/600?random=${i}&query=${encodeURIComponent(query)}`,
                        full: `https://picsum.photos/1920/1080?random=${i}&query=${encodeURIComponent(query)}`
                    },
                    alt_description: `${query} image ${i + 1}`,
                    description: `Beautiful ${query} photography`,
                    user: {
                        name: `Photographer ${i + 1}`,
                        username: `photographer${i + 1}`,
                        links: {
                            html: 'https://unsplash.com'
                        }
                    },
                    links: {
                        html: 'https://unsplash.com',
                        download: `https://picsum.photos/1920/1080?random=${i}&query=${encodeURIComponent(query)}`
                    },
                    width: 800,
                    height: 600
                }));

                await new Promise(resolve => setTimeout(resolve, 1000));
                setImages(mockImages);
                return;
            }

            // Real Unsplash API call
            const response = await fetch(
                `${UNSPLASH_API_URL}?query=${encodeURIComponent(query)}&per_page=20&orientation=landscape`,
                {
                    headers: {
                        'Authorization': `Client-ID ${UNSPLASH_ACCESS_KEY}`,
                        'Accept-Version': 'v1'
                    }
                }
            );

            if (!response.ok) {
                throw new Error(`Unsplash API error: ${response.status} ${response.statusText}`);
            }

            const data = await response.json();
            setImages(data.results || []);
            setIsUsingMockData(false);

        } catch (error) {
            console.error('Error searching images:', error);
            toast.error('Khng th ti nh t Unsplash. Vui lng kim tra cu hnh API key.');

            // Fallback to mock data on error
            setIsUsingMockData(true);
            const mockImages: UnsplashImage[] = Array.from({ length: 8 }, (_, i) => ({
                id: `fallback-${query}-${i}`,
                urls: {
                    small: `https://picsum.photos/400/300?random=${i}&query=${encodeURIComponent(query)}`,
                    regular: `https://picsum.photos/800/600?random=${i}&query=${encodeURIComponent(query)}`,
                    full: `https://picsum.photos/1920/1080?random=${i}&query=${encodeURIComponent(query)}`
                },
                alt_description: `${query} image ${i + 1}`,
                description: `Beautiful ${query} photography`,
                user: {
                    name: `Photographer ${i + 1}`,
                    username: `photographer${i + 1}`,
                    links: {
                        html: 'https://unsplash.com'
                    }
                },
                links: {
                    html: 'https://unsplash.com',
                    download: `https://picsum.photos/1920/1080?random=${i}&query=${encodeURIComponent(query)}`
                },
                width: 800,
                height: 600
            }));
            setImages(mockImages);
        } finally {
            setLoading(false);
        }
    };

    const handleImageSelect = (image: UnsplashImage) => {
        setSelectedImage(image);
        setCustomAltText(image.alt_description || image.description || '');
    };

    const handleConfirmSelection = () => {
        if (!selectedImage) return;

        const attribution = `Photo by ${selectedImage.user.name} on Unsplash`;
        onImageSelect(
            selectedImage.urls.regular,
            customAltText || selectedImage.alt_description || selectedImage.description || '',
            attribution
        );

        setOpen(false);
        setSelectedImage(null);
        setCustomAltText('');
        toast.success('nh  c chn vo bi vit');
    };

    const handleSearch = (e: React.FormEvent) => {
        e.preventDefault();
        if (searchTerm.trim()) {
            searchImages(searchTerm);
        }
    };

    // Load popular images on open
    useEffect(() => {
        if (open && images.length === 0) {
            searchImages('laptop technology');
        }
    }, [open]);

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                {trigger || (
                    <Button variant="outline" size="sm">
                        <ImageIcon className="w-4 h-4 mr-2" />
                        Chn nh t Unsplash
                    </Button>
                )}
            </DialogTrigger>
            <DialogContent className="max-w-6xl max-h-[90vh] w-[95vw] overflow-hidden flex flex-col">
                <DialogHeader>
                    <DialogTitle className="flex items-center gap-2">
                        <ImageIcon className="w-5 h-5" />
                        Chn nh t Unsplash
                    </DialogTitle>
                    <DialogDescription>
                        Tm kim v chn nh min ph t Unsplash  chn vo bi vit
                    </DialogDescription>
                    {isUsingMockData && (
                        <div className="mt-2 p-2 bg-amber-50 border border-amber-200 rounded-lg">
                            <p className="text-sm text-amber-800">
                                <strong>Demo Mode:</strong> ang s dng nh mu.  s dng Unsplash API thc t,
                            </p>
                        </div>
                    )}
                </DialogHeader>

                <div className="space-y-4 flex-1 overflow-hidden flex flex-col">
                    {/* Search */}
                    <form onSubmit={handleSearch} className="flex gap-2">
                        <div className="flex-1 relative">
                            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                            <Input
                                placeholder="Tm kim nh..."
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                                className="pl-10"
                            />
                        </div>
                        <Button type="submit" disabled={loading}>
                            {loading && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                            Tm kim
                        </Button>
                    </form>

                    {/* Popular Terms */}
                    <div className="flex flex-wrap gap-2">
                        <span className="text-sm text-gray-600">Tm kim ph bin:</span>
                        {popularTerms.map(term => (
                            <Badge
                                key={term}
                                variant="outline"
                                className="cursor-pointer hover:bg-gray-100"
                                onClick={() => {
                                    setSearchTerm(term);
                                    searchImages(term);
                                }}
                            >
                                {term}
                            </Badge>
                        ))}
                    </div>

                    {/* Image Grid */}
                    <div className="flex-1 overflow-y-auto min-h-0 p-1">
                        {loading ? (
                            <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 gap-2">
                                {Array.from({ length: 8 }).map((_, i) => (
                                    <Skeleton key={i} className="aspect-square rounded-lg" />
                                ))}
                            </div>
                        ) : images.length > 0 ? (
                            <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 gap-2">
                                {images.map((image) => (
                                    <Card
                                        key={image.id}
                                        className={`cursor-pointer transition-all duration-200 hover:shadow-md ${selectedImage?.id === image.id ? 'ring-2 ring-blue-500' : ''
                                            }`}
                                        onClick={() => handleImageSelect(image)}
                                    >
                                        <CardContent className="p-0">
                                            <div className="relative aspect-square overflow-hidden rounded-t-lg">
                                                <img
                                                    src={image.urls.small}
                                                    alt={image.alt_description}
                                                    className="w-full h-full object-cover"
                                                />
                                                {selectedImage?.id === image.id && (
                                                    <div className="absolute inset-0 bg-blue-500 bg-opacity-20 flex items-center justify-center">
                                                        <div className="bg-blue-500 text-white rounded-full p-2">
                                                            <Download className="w-4 h-4" />
                                                        </div>
                                                    </div>
                                                )}
                                            </div>
                                            <div className="p-2">
                                                <p className="text-xs text-gray-600 truncate">
                                                    by {image.user.name}
                                                </p>
                                            </div>
                                        </CardContent>
                                    </Card>
                                ))}
                            </div>
                        ) : (
                            <div className="text-center py-8 text-gray-500">
                                <ImageIcon className="w-12 h-12 mx-auto mb-4 opacity-50" />
                                <p>Cha c nh no. Hy th tm kim!</p>
                            </div>
                        )}
                    </div>

                    {/* Selected Image Details */}
                    {selectedImage && (
                        <Card className="border-blue-200 bg-blue-50">
                            <CardHeader>
                                <CardTitle className="text-sm">nh  chn</CardTitle>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="flex gap-4">
                                    <img
                                        src={selectedImage.urls.small}
                                        alt={selectedImage.alt_description}
                                        className="w-20 h-20 object-cover rounded-lg"
                                    />
                                    <div className="flex-1">
                                        <p className="text-sm font-medium text-gray-900">
                                            {selectedImage.alt_description || selectedImage.description}
                                        </p>
                                        <p className="text-xs text-gray-600">
                                            by {selectedImage.user.name}
                                        </p>
                                        <div className="flex items-center gap-2 mt-2">
                                            <a
                                                href={selectedImage.links.html}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                className="text-xs text-blue-600 hover:text-blue-800 flex items-center gap-1"
                                            >
                                                <ExternalLink className="w-3 h-3" />
                                                Xem trn Unsplash
                                            </a>
                                        </div>
                                    </div>
                                </div>

                                <div>
                                    <Label htmlFor="alt-text">Alt text (m t nh)</Label>
                                    <Input
                                        id="alt-text"
                                        value={customAltText}
                                        onChange={(e) => setCustomAltText(e.target.value)}
                                        placeholder="M t nh cho ngi khim th..."
                                        className="mt-1"
                                    />
                                </div>

                                <div className="flex justify-end gap-2">
                                    <Button
                                        variant="outline"
                                        onClick={() => setSelectedImage(null)}
                                    >
                                        Hy
                                    </Button>
                                    <Button onClick={handleConfirmSelection}>
                                        <Download className="w-4 h-4 mr-2" />
                                        Chn nh
                                    </Button>
                                </div>
                            </CardContent>
                        </Card>
                    )}
                </div>
            </DialogContent>
        </Dialog>
    );
}
