'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Combobox, createOptions } from '@/components/ui/combobox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { ProductFormData } from '@/lib/admin-api';
import { PRODUCT_SPECS_OPTIONS, getCpuModels, getGpuModels } from '@/lib/product-specs-options';
import { ChevronDown, ChevronRight } from 'lucide-react';
import { useState } from 'react';

interface LaptopSpecificationsSectionProps {
    formData: ProductFormData;
    onInputChange: (field: keyof ProductFormData, value: any) => void;
    validationErrors: Record<string, string>;
    expandedSections: Record<string, boolean>;
    onToggleSection: (section: string) => void;
}

export default function LaptopSpecificationsSection({
    formData,
    onInputChange,
    validationErrors,
    expandedSections,
    onToggleSection
}: LaptopSpecificationsSectionProps) {
    const isExpanded = (section: string) => expandedSections[section] ?? true;

    // State for dependent dropdowns
    const [selectedCpuBrand, setSelectedCpuBrand] = useState(formData.cpuBrand || '');
    const [selectedGpuBrand, setSelectedGpuBrand] = useState(formData.gpuBrand || '');

    // Get available options based on selections
    const cpuModelOptions = selectedCpuBrand ? getCpuModels(selectedCpuBrand) : [];
    const gpuModelOptions = selectedGpuBrand ? getGpuModels(selectedGpuBrand) : [];

    return (
        <div className="space-y-6">
            {/* Basic Info */}
            <Card>
                <Collapsible open={isExpanded('laptop-basic')} onOpenChange={() => onToggleSection('laptop-basic')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>Thng tin c bn</CardTitle>
                                    <CardDescription>Series v model ca laptop</CardDescription>
                                </div>
                                {isExpanded('laptop-basic') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="series">Series</Label>
                                    <Input
                                        id="series"
                                        value={formData.series || ''}
                                        onChange={(e) => onInputChange('series', e.target.value)}
                                        placeholder="VD: MacBook Pro, ThinkPad X1"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="model">Model</Label>
                                    <Input
                                        id="model"
                                        value={formData.model || ''}
                                        onChange={(e) => onInputChange('model', e.target.value)}
                                        placeholder="VD: M3 Pro, Carbon Gen 11"
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>

            {/* CPU Specifications */}
            <Card>
                <Collapsible open={isExpanded('laptop-cpu')} onOpenChange={() => onToggleSection('laptop-cpu')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>CPU - B x l</CardTitle>
                                    <CardDescription>Thng s chi tit v b x l</CardDescription>
                                </div>
                                {isExpanded('laptop-cpu') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="cpuBrand">Thng hiu CPU</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.cpuBrands)}
                                        value={formData.cpuBrand || ''}
                                        onValueChange={(value) => {
                                            onInputChange('cpuBrand', value);
                                            setSelectedCpuBrand(value);
                                            // Reset CPU model when brand changes
                                            if (value !== formData.cpuBrand) {
                                                onInputChange('cpuModel', '');
                                            }
                                        }}
                                        placeholder="Chn thng hiu CPU..."
                                        searchPlaceholder="Tm kim thng hiu..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuModel">Model CPU</Label>
                                    <Combobox
                                        options={createOptions(cpuModelOptions)}
                                        value={formData.cpuModel || ''}
                                        onValueChange={(value) => onInputChange('cpuModel', value)}
                                        placeholder="Chn model CPU..."
                                        searchPlaceholder="Tm kim model..."
                                        disabled={!selectedCpuBrand || selectedCpuBrand === 'Khc'}
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuGeneration">Th h CPU</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.cpuGenerations)}
                                        value={formData.cpuGeneration || ''}
                                        onValueChange={(value) => onInputChange('cpuGeneration', value)}
                                        placeholder="Chn th h CPU..."
                                        searchPlaceholder="Tm kim th h..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuCores">S nhn</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.cpuCores)}
                                        value={formData.cpuCores || ''}
                                        onValueChange={(value) => onInputChange('cpuCores', value)}
                                        placeholder="Chn s nhn..."
                                        searchPlaceholder="Tm kim s nhn..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuBaseClockGHz">Tn s c bn (GHz)</Label>
                                    <Input
                                        id="cpuBaseClockGHz"
                                        type="number"
                                        step="0.1"
                                        value={formData.cpuBaseClockGHz || ''}
                                        onChange={(e) => onInputChange('cpuBaseClockGHz', e.target.value)}
                                        placeholder="VD: 2.4, 3.2"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuBoostClockGHz">Tn s tng cng (GHz)</Label>
                                    <Input
                                        id="cpuBoostClockGHz"
                                        type="number"
                                        step="0.1"
                                        value={formData.cpuBoostClockGHz || ''}
                                        onChange={(e) => onInputChange('cpuBoostClockGHz', e.target.value)}
                                        placeholder="VD: 4.8, 5.0"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuCache">Cache</Label>
                                    <Input
                                        id="cpuCache"
                                        value={formData.cpuCache || ''}
                                        onChange={(e) => onInputChange('cpuCache', e.target.value)}
                                        placeholder="VD: 24MB L3, 16MB L3"
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>

            {/* RAM Specifications */}
            <Card>
                <Collapsible open={isExpanded('laptop-ram')} onOpenChange={() => onToggleSection('laptop-ram')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>RAM - B nh</CardTitle>
                                    <CardDescription>Thng s chi tit v b nh RAM</CardDescription>
                                </div>
                                {isExpanded('laptop-ram') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="ramType">Loi RAM</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.ramTypes)}
                                        value={formData.ramType || ''}
                                        onValueChange={(value) => onInputChange('ramType', value)}
                                        placeholder="Chn loi RAM..."
                                        searchPlaceholder="Tm kim loi RAM..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ramCapacityGB">Dung lng RAM</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.ramCapacities)}
                                        value={formData.ramCapacityGB || ''}
                                        onValueChange={(value) => onInputChange('ramCapacityGB', value)}
                                        placeholder="Chn dung lng RAM..."
                                        searchPlaceholder="Tm kim dung lng..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ramSlots">S khe RAM</Label>
                                    <Input
                                        id="ramSlots"
                                        type="number"
                                        value={formData.ramSlots || ''}
                                        onChange={(e) => onInputChange('ramSlots', e.target.value)}
                                        placeholder="VD: 2, 4"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ramSpeed">Tc  RAM</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.ramSpeeds)}
                                        value={formData.ramSpeed || ''}
                                        onValueChange={(value) => onInputChange('ramSpeed', value)}
                                        placeholder="Chn tc  RAM..."
                                        searchPlaceholder="Tm kim tc ..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ramUpgradeable">C th nng cp</Label>
                                    <select
                                        value={formData.ramUpgradeable ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('ramUpgradeable', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Khng</option>
                                        <option value="true">C</option>
                                    </select>
                                </div>
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>

            {/* Storage Specifications */}
            <Card>
                <Collapsible open={isExpanded('laptop-storage')} onOpenChange={() => onToggleSection('laptop-storage')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>Storage -  cng</CardTitle>
                                    <CardDescription>Thng s chi tit v  cng</CardDescription>
                                </div>
                                {isExpanded('laptop-storage') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="storageType">Loi  cng</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.storageTypes)}
                                        value={formData.storageType || ''}
                                        onValueChange={(value) => onInputChange('storageType', value)}
                                        placeholder="Chn loi  cng..."
                                        searchPlaceholder="Tm kim loi  cng..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="storageCapacityGB">Dung lng</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.storageCapacities)}
                                        value={formData.storageCapacityGB || ''}
                                        onValueChange={(value) => onInputChange('storageCapacityGB', value)}
                                        placeholder="Chn dung lng..."
                                        searchPlaceholder="Tm kim dung lng..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="storageInterface">Giao tip</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.storageInterfaces)}
                                        value={formData.storageInterface || ''}
                                        onValueChange={(value) => onInputChange('storageInterface', value)}
                                        placeholder="Chn giao tip..."
                                        searchPlaceholder="Tm kim giao tip..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="nvMeSupport">H tr NVMe</Label>
                                    <select
                                        value={formData.nvMeSupport ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('nvMeSupport', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Khng</option>
                                        <option value="true">C</option>
                                    </select>
                                </div>
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>

            {/* GPU Specifications */}
            <Card>
                <Collapsible open={isExpanded('laptop-gpu')} onOpenChange={() => onToggleSection('laptop-gpu')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>GPU - Card  ha</CardTitle>
                                    <CardDescription>Thng s chi tit v card  ha</CardDescription>
                                </div>
                                {isExpanded('laptop-gpu') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="gpuType">Loi GPU</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.gpuTypes)}
                                        value={formData.gpuType || ''}
                                        onValueChange={(value) => onInputChange('gpuType', value)}
                                        placeholder="Chn loi GPU..."
                                        searchPlaceholder="Tm kim loi GPU..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="gpuBrand">Thng hiu GPU</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.gpuBrands)}
                                        value={formData.gpuBrand || ''}
                                        onValueChange={(value) => {
                                            onInputChange('gpuBrand', value);
                                            setSelectedGpuBrand(value);
                                            // Reset GPU model when brand changes
                                            if (value !== formData.gpuBrand) {
                                                onInputChange('gpuModel', '');
                                            }
                                        }}
                                        placeholder="Chn thng hiu GPU..."
                                        searchPlaceholder="Tm kim thng hiu..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="gpuModel">Model GPU</Label>
                                    <Combobox
                                        options={createOptions(gpuModelOptions)}
                                        value={formData.gpuModel || ''}
                                        onValueChange={(value) => onInputChange('gpuModel', value)}
                                        placeholder="Chn model GPU..."
                                        searchPlaceholder="Tm kim model..."
                                        disabled={!selectedGpuBrand || selectedGpuBrand === 'Khc'}
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="gpuVramGB">VRAM</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.gpuVramSizes)}
                                        value={formData.gpuVramGB || ''}
                                        onValueChange={(value) => onInputChange('gpuVramGB', value)}
                                        placeholder="Chn VRAM..."
                                        searchPlaceholder="Tm kim VRAM..."
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>

            {/* Display Specifications */}
            <Card>
                <Collapsible open={isExpanded('laptop-display')} onOpenChange={() => onToggleSection('laptop-display')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>Display - Mn hnh</CardTitle>
                                    <CardDescription>Thng s chi tit v mn hnh</CardDescription>
                                </div>
                                {isExpanded('laptop-display') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="displaySizeInches">Kch thc mn hnh</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.displaySizes)}
                                        value={formData.displaySizeInches || ''}
                                        onValueChange={(value) => onInputChange('displaySizeInches', value)}
                                        placeholder="Chn kch thc mn hnh..."
                                        searchPlaceholder="Tm kim kch thc..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="displayResolution"> phn gii</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.displayResolutions)}
                                        value={formData.displayResolution || ''}
                                        onValueChange={(value) => onInputChange('displayResolution', value)}
                                        placeholder="Chn  phn gii..."
                                        searchPlaceholder="Tm kim  phn gii..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="displayPanelType">Loi panel</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.displayPanels)}
                                        value={formData.displayPanelType || ''}
                                        onValueChange={(value) => onInputChange('displayPanelType', value)}
                                        placeholder="Chn loi panel..."
                                        searchPlaceholder="Tm kim loi panel..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="displayRefreshRateHz">Tn s qut</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.refreshRates)}
                                        value={formData.displayRefreshRateHz || ''}
                                        onValueChange={(value) => onInputChange('displayRefreshRateHz', value)}
                                        placeholder="Chn tn s qut..."
                                        searchPlaceholder="Tm kim tn s qut..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="displayTouchscreen">Cm ng</Label>
                                    <select
                                        value={formData.displayTouchscreen ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('displayTouchscreen', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Khng</option>
                                        <option value="true">C</option>
                                    </select>
                                </div>
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>

            {/* Physical Specifications */}
            <Card>
                <Collapsible open={isExpanded('laptop-physical')} onOpenChange={() => onToggleSection('laptop-physical')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>Physical - Thng s vt l</CardTitle>
                                    <CardDescription>Kch thc, trng lng v cc thng s vt l</CardDescription>
                                </div>
                                {isExpanded('laptop-physical') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="batteryCapacityWh">Dung lng pin</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.batteryCapacities)}
                                        value={formData.batteryCapacityWh || ''}
                                        onValueChange={(value) => onInputChange('batteryCapacityWh', value)}
                                        placeholder="Chn dung lng pin..."
                                        searchPlaceholder="Tm kim dung lng pin..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="weightKg">Trng lng</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.weightRanges)}
                                        value={formData.weightKg || ''}
                                        onValueChange={(value) => onInputChange('weightKg', value)}
                                        placeholder="Chn trng lng..."
                                        searchPlaceholder="Tm kim trng lng..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="color">Mu sc</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.colors)}
                                        value={formData.color || ''}
                                        onValueChange={(value) => onInputChange('color', value)}
                                        placeholder="Chn mu sc..."
                                        searchPlaceholder="Tm kim mu sc..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ports">Cng kt ni</Label>
                                    <Input
                                        id="ports"
                                        value={formData.ports || ''}
                                        onChange={(e) => onInputChange('ports', e.target.value)}
                                        placeholder="VD: 2x USB-C, 1x HDMI, 3.5mm Audio"
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>

            {/* Connectivity */}
            <Card>
                <Collapsible open={isExpanded('laptop-connectivity')} onOpenChange={() => onToggleSection('laptop-connectivity')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>Connectivity - Kt ni</CardTitle>
                                    <CardDescription>WiFi, Bluetooth v cc kt ni khc</CardDescription>
                                </div>
                                {isExpanded('laptop-connectivity') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="wiFi6Support">H tr WiFi 6</Label>
                                    <select
                                        value={formData.wiFi6Support ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('wiFi6Support', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Khng</option>
                                        <option value="true">C</option>
                                    </select>
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="bluetoothSupport">H tr Bluetooth</Label>
                                    <select
                                        value={formData.bluetoothSupport ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('bluetoothSupport', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Khng</option>
                                        <option value="true">C</option>
                                    </select>
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="bluetoothVersion">Phin bn Bluetooth</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.bluetoothVersions)}
                                        value={formData.bluetoothVersion || ''}
                                        onValueChange={(value) => onInputChange('bluetoothVersion', value)}
                                        placeholder="Chn phin bn Bluetooth..."
                                        searchPlaceholder="Tm kim phin bn..."
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>

            {/* Business Information */}
            <Card>
                <Collapsible open={isExpanded('laptop-business')} onOpenChange={() => onToggleSection('laptop-business')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>Business - Thng tin kinh doanh</CardTitle>
                                    <CardDescription>Bo hnh v i tng khch hng</CardDescription>
                                </div>
                                {isExpanded('laptop-business') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="warrantyPeriod">Thi gian bo hnh</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.warrantyPeriods)}
                                        value={formData.warrantyPeriod || ''}
                                        onValueChange={(value) => onInputChange('warrantyPeriod', value)}
                                        placeholder="Chn thi gian bo hnh..."
                                        searchPlaceholder="Tm kim thi gian bo hnh..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="targetAudience">i tng khch hng</Label>
                                    <Input
                                        id="targetAudience"
                                        value={formData.targetAudience || ''}
                                        onChange={(e) => onInputChange('targetAudience', e.target.value)}
                                        placeholder="VD: Gaming, Business, Student, Creative"
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>

            {/* Connectivity & Ports */}
            <Card>
                <Collapsible open={isExpanded('laptop-connectivity')} onOpenChange={() => onToggleSection('laptop-connectivity')}>
                    <CollapsibleTrigger asChild>
                        <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                            <div className="flex items-center justify-between">
                                <div>
                                    <CardTitle>Kt ni & Cng</CardTitle>
                                    <CardDescription>Thng tin v kt ni v cng kt ni</CardDescription>
                                </div>
                                {isExpanded('laptop-connectivity') ? (
                                    <ChevronDown className="h-5 w-5 text-muted-foreground" />
                                ) : (
                                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                                )}
                            </div>
                        </CardHeader>
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="ports">Cng kt ni</Label>
                                    <Textarea
                                        id="ports"
                                        value={formData.ports || ''}
                                        onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => onInputChange('ports', e.target.value)}
                                        placeholder="VD: 2x USB 3.2, 1x USB-C Thunderbolt 4, HDMI 2.1, 3.5mm Audio"
                                        rows={3}
                                    />
                                </div>
                                <div className="space-y-4">
                                    <div className="flex items-center space-x-2">
                                        <input
                                            type="checkbox"
                                            id="wiFi6Support"
                                            checked={formData.wiFi6Support || false}
                                            onChange={(e) => onInputChange('wiFi6Support', e.target.checked)}
                                        />
                                        <Label htmlFor="wiFi6Support">H tr WiFi 6</Label>
                                    </div>
                                    <div className="flex items-center space-x-2">
                                        <input
                                            type="checkbox"
                                            id="bluetoothSupport"
                                            checked={formData.bluetoothSupport || false}
                                            onChange={(e) => onInputChange('bluetoothSupport', e.target.checked)}
                                        />
                                        <Label htmlFor="bluetoothSupport">H tr Bluetooth</Label>
                                    </div>
                                </div>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="bluetoothVersion">Phin bn Bluetooth</Label>
                                <Combobox
                                    options={createOptions(PRODUCT_SPECS_OPTIONS.bluetoothVersions)}
                                    value={formData.bluetoothVersion || ''}
                                    onValueChange={(value) => onInputChange('bluetoothVersion', value)}
                                    placeholder="Chn phin bn Bluetooth..."
                                    searchPlaceholder="Tm kim phin bn..."
                                />
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>
        </div>
    );
}