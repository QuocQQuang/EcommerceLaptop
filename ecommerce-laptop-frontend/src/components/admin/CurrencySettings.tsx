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
        toast.success(` chuyn sang ${CURRENCY_CONFIGS[currency].code}`);
    };

    const handleRefreshRates = async () => {
        setIsRefreshing(true);
        try {
            const newRates = await fetchLatestExchangeRates();
            setExchangeRatesState(newRates);
            setCustomRates(newRates);
            toast.success(' cp nht t gi mi nht');
        } catch (error) {
            toast.error('Khng th cp nht t gi. S dng t gi mc nh.');
        } finally {
            setIsRefreshing(false);
        }
    };

    const handleSaveCustomRates = async () => {
        setIsSaving(true);
        try {
            setExchangeRates(customRates);
            setExchangeRatesState(customRates);
            toast.success(' lu t gi ty chnh');
        } catch (error) {
            toast.error('Khng th lu t gi ty chnh');
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
                        Ci t tin t
                    </CardTitle>
                    <CardDescription>
                        Chn loi tin t hin th mc nh cho ton b h thng
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="space-y-2">
                        <Label htmlFor="currency-select">Tin t mc nh</Label>
                        <Select value={selectedCurrency} onValueChange={handleCurrencyChange}>
                            <SelectTrigger>
                                <SelectValue placeholder="Chn tin t" />
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
                        <Label htmlFor="auto-refresh">T ng cp nht t gi</Label>
                    </div>
                </CardContent>
            </Card>

            {/* Exchange Rates */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center justify-between">
                        <span>T gi hi oi</span>
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={handleRefreshRates}
                            disabled={isRefreshing}
                        >
                            <RefreshCw className={`h-4 w-4 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
                            Cp nht
                        </Button>
                    </CardTitle>
                    <CardDescription>
                        T gi hin ti: 1 USD = {exchangeRates.VND.toLocaleString()} VND
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
                        {isSaving ? 'ang lu...' : 'Lu t gi ty chnh'}
                    </Button>
                </CardContent>
            </Card>

            {/* Current Settings Summary */}
            <Card>
                <CardHeader>
                    <CardTitle>Ci t hin ti</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="space-y-2 text-sm">
                        <div className="flex justify-between">
                            <span>Tin t mc nh:</span>
                            <span className="font-medium">
                                {CURRENCY_CONFIGS[selectedCurrency].code} ({CURRENCY_CONFIGS[selectedCurrency].symbol})
                            </span>
                        </div>
                        <div className="flex justify-between">
                            <span>T gi USD  VND:</span>
                            <span className="font-medium">1 : {exchangeRates.VND.toLocaleString()}</span>
                        </div>
                        <div className="flex justify-between">
                            <span>T ng cp nht:</span>
                            <span className="font-medium">{autoRefresh ? 'Bt' : 'Tt'}</span>
                        </div>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
