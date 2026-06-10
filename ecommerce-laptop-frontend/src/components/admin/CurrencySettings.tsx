'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import {
    Currency,
    CURRENCY_CONFIGS,
    fetchLatestExchangeRates,
    getExchangeRates,
    setExchangeRates
} from '@/lib/currency';
import { RefreshCw, Save, Settings } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface CurrencySettingsProps {
    className?: string;
}

export function CurrencySettings({ className = '' }: CurrencySettingsProps) {
    const { selectedCurrency: contextCurrency, setSelectedCurrency: setContextCurrency } = useCurrencyContext();
    const [selectedCurrency, setSelectedCurrencyState] = useState<Currency>(contextCurrency);
    const [exchangeRates, setExchangeRatesState] = useState(getExchangeRates());
    const [isRefreshing, setIsRefreshing] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [customRates, setCustomRates] = useState<Record<Currency, number>>(exchangeRates);
    const [autoRefresh, setAutoRefresh] = useState(false);

    useEffect(() => {
        setSelectedCurrencyState(contextCurrency);
        setExchangeRatesState(getExchangeRates());
        setCustomRates(getExchangeRates());
    }, [contextCurrency]);

    const handleCurrencyChange = (currency: Currency) => {
        setContextCurrency(currency);
        setSelectedCurrencyState(currency);
        toast.success(`Đã chuyển sang ${CURRENCY_CONFIGS[currency].code}`);
    };

    const handleRefreshRates = async () => {
        setIsRefreshing(true);
        try {
            const newRates = await fetchLatestExchangeRates();
            setExchangeRatesState(newRates);
            setCustomRates(newRates);
            toast.success('Đã cập nhật tỷ giá mới nhất');
        } catch (error) {
            toast.error('Không thể cập nhật tỷ giá. Sử dụng tỷ giá mặc định.');
        } finally {
            setIsRefreshing(false);
        }
    };

    const handleSaveCustomRates = async () => {
        setIsSaving(true);
        try {
            setExchangeRates(customRates);
            setExchangeRatesState(customRates);
            toast.success('Đã lưu tỷ giá tùy chỉnh');
        } catch (error) {
            toast.error('Không thể lưu tỷ giá tùy chỉnh');
        } finally {
            setIsSaving(false);
        }
    };

    const handleRateChange = (currency: Currency, value: string) => {
        const numValue = parseFloat(value) || 0;
        setCustomRates(prev => ({
            ...prev,
            [currency]: numValue
        }));
    };

    return (
        <div className={`space-y-6 ${className}`}>
            {/* Currency Selection */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Settings className="h-5 w-5" />
                        Cài đặt tiền tệ
                    </CardTitle>
                    <CardDescription>
                        Chọn loại tiền tệ hiển thị mặc định cho toàn bộ hệ thống
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="space-y-2">
                        <Label htmlFor="currency-select">Tiền tệ mặc định</Label>
                        <Select value={selectedCurrency} onValueChange={handleCurrencyChange}>
                            <SelectTrigger>
                                <SelectValue placeholder="Chọn tiền tệ" />
                            </SelectTrigger>
                            <SelectContent>
                                {Object.entries(CURRENCY_CONFIGS).map(([code, config]) => (
                                    <SelectItem key={code} value={code}>
                                        <div className="flex items-center gap-2">
                                            <span className="font-medium">{config.code}</span>
                                            <span className="text-muted-foreground">{config.symbol}</span>
                                        </div>
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>

                    <div className="flex items-center space-x-2">
                        <Switch
                            id="auto-refresh"
                            checked={autoRefresh}
                            onCheckedChange={setAutoRefresh}
                        />
                        <Label htmlFor="auto-refresh">Tự động cập nhật tỷ giá</Label>
                    </div>
                </CardContent>
            </Card>

            {/* Exchange Rates */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center justify-between">
                        <span>Tỷ giá hối đoái</span>
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={handleRefreshRates}
                            disabled={isRefreshing}
                        >
                            <RefreshCw className={`h-4 w-4 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
                            Cập nhật
                        </Button>
                    </CardTitle>
                    <CardDescription>
                        Tỷ giá hiện tại: 1 USD = {exchangeRates.VND.toLocaleString()} VND
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {Object.entries(CURRENCY_CONFIGS).map(([code, config]) => (
                            <div key={code} className="space-y-2">
                                <Label htmlFor={`rate-${code}`}>
                                    {config.code} ({config.symbol})
                                </Label>
                                <div className="flex items-center gap-2">
                                    <Input
                                        id={`rate-${code}`}
                                        type="number"
                                        value={customRates[code as Currency]}
                                        onChange={(e) => handleRateChange(code as Currency, e.target.value)}
                                        placeholder="0"
                                        step="0.01"
                                        min="0"
                                    />
                                    <span className="text-sm text-muted-foreground">USD</span>
                                </div>
                            </div>
                        ))}
                    </div>

                    <Button
                        onClick={handleSaveCustomRates}
                        disabled={isSaving}
                        className="w-full"
                    >
                        <Save className="h-4 w-4 mr-2" />
                        {isSaving ? 'Đang lưu...' : 'Lưu tỷ giá tùy chỉnh'}
                    </Button>
                </CardContent>
            </Card>

            {/* Current Settings Summary */}
            <Card>
                <CardHeader>
                    <CardTitle>Cài đặt hiện tại</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="space-y-2 text-sm">
                        <div className="flex justify-between">
                            <span>Tiền tệ mặc định:</span>
                            <span className="font-medium">
                                {CURRENCY_CONFIGS[selectedCurrency].code} ({CURRENCY_CONFIGS[selectedCurrency].symbol})
                            </span>
                        </div>
                        <div className="flex justify-between">
                            <span>Tỷ giá USD sang VND:</span>
                            <span className="font-medium">1 : {exchangeRates.VND.toLocaleString()}</span>
                        </div>
                        <div className="flex justify-between">
                            <span>Tự động cập nhật:</span>
                            <span className="font-medium">{autoRefresh ? 'Bật' : 'Tắt'}</span>
                        </div>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
