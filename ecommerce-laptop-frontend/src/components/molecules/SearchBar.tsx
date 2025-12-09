'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { useDebounce } from '@/hooks/useUtilities';
import { cn } from '@/lib/utils';
import { Search, X } from 'lucide-react';
import { useState } from 'react';

interface SearchBarProps {
    placeholder?: string;
    value?: string;
    onChange?: (value: string) => void;
    onSearch?: (query: string) => void;
    onSuggestionClick?: (suggestion: any) => void;
    className?: string;
    autoFocus?: boolean;
}

export function SearchBar({
    placeholder = 'Bn mun tm g?',
    value: controlledValue,
    onChange: onControlledChange,
    onSearch,
    onSuggestionClick,
    className,
    autoFocus = false,
}: SearchBarProps) {
    const [internalQuery, setInternalQuery] = useState('');
    const [suggestions, setSuggestions] = useState<any[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [showSuggestions, setShowSuggestions] = useState(false);

    // Use controlled value if provided, otherwise use internal state
    const query = controlledValue !== undefined ? controlledValue : internalQuery;
    const setQuery = (value: string) => {
        if (controlledValue !== undefined && onControlledChange) {
            onControlledChange(value);
        } else {
            setInternalQuery(value);
        }
    };

    const debouncedQuery = useDebounce(query, 300);

    const handleSearch = (searchQuery: string = query) => {
        if (searchQuery.trim()) {
            onSearch?.(searchQuery.trim());
            setShowSuggestions(false);
        }
    };

    const handleInputChange = (value: string) => {
        setQuery(value);
        setShowSuggestions(value.length > 0);
    };

    const clearSearch = () => {
        setQuery('');
        setShowSuggestions(false);
        setSuggestions([]);
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') {
            handleSearch();
        } else if (e.key === 'Escape') {
            setShowSuggestions(false);
        }
    };

    return (
        <div className={cn('relative w-full max-w-md', className)}>
            <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                <Input
                    type="text"
                    placeholder={placeholder}
                    value={query}
                    onChange={(e) => handleInputChange(e.target.value)}
                    onKeyDown={handleKeyDown}
                    onFocus={() => setShowSuggestions(query.length > 0)}
                    autoFocus={autoFocus}
                    className="pl-10 pr-10"
                />
                {query && (
                    <Button
                        variant="ghost"
                        size="sm"
                        className="absolute right-1 top-1/2 transform -translate-y-1/2 h-6 w-6 p-0"
                        onClick={clearSearch}
                    >
                        <X className="w-3 h-3" />
                    </Button>
                )}
            </div>

            {showSuggestions && suggestions.length > 0 && (
                <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-200 rounded-md shadow-lg z-50 max-h-80 overflow-y-auto">
                    {suggestions.map((suggestion, index) => (
                        <button
                            key={index}
                            className="w-full px-4 py-2 text-left hover:bg-gray-50 flex items-center gap-3 border-b border-gray-100 last:border-b-0"
                            onClick={() => {
                                onSuggestionClick?.(suggestion);
                                setShowSuggestions(false);
                            }}
                        >
                            {suggestion.imageUrl && (
                                <img
                                    src={suggestion.imageUrl}
                                    alt={suggestion.name}
                                    className="w-8 h-8 object-cover rounded"
                                />
                            )}
                            <div className="flex-1">
                                <div className="font-medium text-sm">{suggestion.name}</div>
                                {suggestion.price && (
                                    <div className="text-xs text-gray-500">
                                        {new Intl.NumberFormat(undefined, { style: 'currency', currency: process.env.NEXT_PUBLIC_CURRENCY || 'USD' }).format(suggestion.price)}
                                    </div>
                                )}
                            </div>
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
}