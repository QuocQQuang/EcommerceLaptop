import { CurrencySettings } from '@/components/admin/CurrencySettings';

export default function CurrencySettingsPage() {
    return (
        <div className="container mx-auto py-6">
            <div className="mb-6">
                <h1 className="text-3xl font-bold">Cài đặt tiền tệ</h1>
                <p className="text-muted-foreground mt-2">
                    Quản lý cài đặt tiền tệ và tỷ giá hối đoái cho toàn bộ hệ thống
                </p>
            </div>

            <CurrencySettings />
        </div>
    );
}
