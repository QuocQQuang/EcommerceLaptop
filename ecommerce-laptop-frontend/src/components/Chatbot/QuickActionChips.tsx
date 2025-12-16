'use client';

interface QuickActionChipsProps {
    onChipClick: (query: string) => void;
}

const QUICK_ACTIONS = [
    { label: 'Laptop Gaming', query: 'Cho ti xem laptop gaming' },
    { label: 'Mng & Nh', query: 'Ti cn laptop mng nh' },
    { label: 'Sinh Vin', query: 'Laptop tt nht cho sinh vin' },
    { label: 'Di 20 triu', query: 'Laptop di 20 triu' },
    { label: 'Vn Phng', query: 'Laptop vn phng tt nht' },
    { label: 'Sng To', query: 'Laptop cho edit video v thit k' },
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
