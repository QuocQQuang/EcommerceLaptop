'use client';

interface QuickActionChipsProps {
    onChipClick: (query: string) => void;
}

const QUICK_ACTIONS = [
    { label: 'Gaming Laptops', query: 'Show me gaming laptops' },
    { label: 'Thin & Light', query: 'I need a thin and light laptop' },
    { label: 'Student Deals', query: 'Best laptops for students' },
    { label: 'Under $1000', query: 'Show laptops under $1000' },
    { label: 'Business', query: 'Best business laptops' },
    { label: 'Creative Work', query: 'Laptops for video editing and design' },
];

export function QuickActionChips({ onChipClick }: QuickActionChipsProps) {
    return (
        <div className="flex flex-wrap gap-2 p-3 border-t border-neutral-100">
            {QUICK_ACTIONS.map((action) => (
                <button
                    key={action.label}
                    onClick={() => onChipClick(action.query)}
                    className="border border-neutral-200 rounded-lg px-3 py-1.5 text-xs font-medium text-neutral-700 hover:border-neutral-400 hover:bg-neutral-50 transition-colors"
                >
                    {action.label}
                </button>
            ))}
        </div>
    );
}
