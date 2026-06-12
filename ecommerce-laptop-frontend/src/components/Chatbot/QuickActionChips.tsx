'use client';

interface QuickActionChipsProps {
    onChipClick: (query: string) => void;
}

const QUICK_ACTIONS = [
    { label: 'Laptop Gaming', query: 'Cho tôi xem laptop gaming' },
    { label: 'Mỏng & Nhẹ', query: 'Tôi cần laptop mỏng nhẹ' },
    { label: 'Sinh Viên', query: 'Laptop tốt nhất cho sinh viên' },
    { label: 'Dưới 20 triệu', query: 'Laptop di 2000$' },
    { label: 'Văn Phòng', query: 'Laptop văn phòng tốt nhất' },
    { label: 'Sáng Tạo', query: 'Laptop cho edit video và thiết kế' },
    { label: 'Xem đơn hàng', query: 'Cho tôi xem đơn hàng của tôi' },
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
