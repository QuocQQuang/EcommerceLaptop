import { cn } from '@/lib/utils';
import NextImage from 'next/image';
import { useState } from 'react';

interface ImageProps {
    src: string;
    alt: string;
    width?: number;
    height?: number;
    className?: string;
    priority?: boolean;
    fill?: boolean;
    placeholder?: 'blur' | 'empty';
    blurDataURL?: string;
    fallbackSrc?: string;
    sizes?: string;
    onLoad?: () => void;
    onError?: () => void;
}

export function Image({
    src,
    alt,
    width,
    height,
    className,
    priority = false,
    fill = false,
    placeholder = 'empty',
    blurDataURL,
    fallbackSrc = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjMwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjRjNGNEY1Ii8+PHRleHQgeD0iNTAlIiB5PSI1NSUiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIxOCIgZmlsbD0iI0I5QkNCQSIgZm9udC13ZWlnaHQ9IjUwMCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+Tm8gSW1hZ2UgQXZhaWxhYmxlPC90ZXh0Pjx0ZXh0IHg9IjUwJSIgeT0iNjUlIiBmb250LWZhbWlseT0iQXJpYWwiIGZvbnQtc2l6ZT0iMTIiIGZpbGw9IiM5OTk5OTkiIGZvbnQtd2VpZ2h0PSIyMDAiIHRleHQtYW5jaG9yPSJtaWRkbGUiPk5vIEltYWdlIEF2YWlsYWJsZTwvdGV4dD48L3N2Zz4=',
    sizes,
    onLoad,
    onError,
}: ImageProps) {
    const initialSrc = src || fallbackSrc;
    const [imgSrc, setImgSrc] = useState(initialSrc);
    const [isLoading, setIsLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    const handleLoad = () => {
        setIsLoading(false);
        onLoad?.();
    };

    const handleError = () => {
        setIsLoading(false);
        setHasError(true);
        setImgSrc(fallbackSrc);
        onError?.();
    };

    return (
        <div className={cn(
            'relative overflow-hidden',
            fill && 'h-full w-full',
            className
        )}>
            {isLoading && (
                <div className="absolute inset-0 bg-gray-200 animate-pulse rounded" />
            )}

            {imgSrc && (
                <NextImage
                    src={imgSrc}
                    alt={alt}
                    width={fill ? undefined : width}
                    height={fill ? undefined : height}
                    fill={fill}
                    priority={priority}
                    placeholder={placeholder}
                    blurDataURL={blurDataURL}
                    sizes={sizes}
                    // Bt unoptimized cho tt c nh theo yu cu (b ton b Next optimizer)
                    unoptimized={true}
                    className={cn(
                        'transition-opacity duration-300',
                        isLoading ? 'opacity-0' : 'opacity-100',
                        hasError && 'grayscale'
                    )}
                    onLoad={handleLoad}
                    onError={handleError}
                />
            )}

            {hasError && (
                <div className="absolute inset-0 flex items-center justify-center bg-gray-100 text-gray-400 text-xs">
                    Khng th ti nh
                </div>
            )}
        </div>
    );
}