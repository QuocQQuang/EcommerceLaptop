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
                                    <CardTitle>Thông tin cơ bản</CardTitle>
                                    <CardDescription>Series và model của laptop</CardDescription>
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
                                    <CardTitle>CPU - Bộ xử lý</CardTitle>
                                    <CardDescription>Thông số chi tiết về bộ xử lý</CardDescription>
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
                                    <Label htmlFor="cpuBrand">Thương hiệu CPU</Label>
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
                                        placeholder="Chọn thương hiệu CPU..."
                                        searchPlaceholder="Tìm kiếm thương hiệu..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuModel">Model CPU</Label>
                                    <Combobox
                                        options={createOptions(cpuModelOptions)}
                                        value={formData.cpuModel || ''}
                                        onValueChange={(value) => onInputChange('cpuModel', value)}
                                        placeholder="Chọn model CPU..."
                                        searchPlaceholder="Tìm kiếm model..."
                                        disabled={!selectedCpuBrand || selectedCpuBrand === 'Khác'}
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuGeneration">Thế hệ CPU</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.cpuGenerations)}
                                        value={formData.cpuGeneration || ''}
                                        onValueChange={(value) => onInputChange('cpuGeneration', value)}
                                        placeholder="Chọn thế hệ CPU..."
                                        searchPlaceholder="Tìm kiếm thế hệ..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuCores">Số nhân</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.cpuCores)}
                                        value={formData.cpuCores || ''}
                                        onValueChange={(value) => onInputChange('cpuCores', value)}
                                        placeholder="Chọn số nhân..."
                                        searchPlaceholder="Tìm kiếm số nhân..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="cpuBaseClockGHz">Tần số cơ bản (GHz)</Label>
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
                                    <Label htmlFor="cpuBoostClockGHz">Tần số tăng cường (GHz)</Label>
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
                                    <CardTitle>RAM - Bộ nhớ</CardTitle>
                                    <CardDescription>Thông số chi tiết về bộ nhớ RAM</CardDescription>
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
                                    <Label htmlFor="ramType">Loại RAM</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.ramTypes)}
                                        value={formData.ramType || ''}
                                        onValueChange={(value) => onInputChange('ramType', value)}
                                        placeholder="Chọn loại RAM..."
                                        searchPlaceholder="Tìm kiếm loại RAM..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ramCapacityGB">Dung lượng RAM</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.ramCapacities)}
                                        value={formData.ramCapacityGB || ''}
                                        onValueChange={(value) => onInputChange('ramCapacityGB', value)}
                                        placeholder="Chọn dung lượng RAM..."
                                        searchPlaceholder="Tìm kiếm dung lượng..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ramSlots">Số khe RAM</Label>
                                    <Input
                                        id="ramSlots"
                                        type="number"
                                        value={formData.ramSlots || ''}
                                        onChange={(e) => onInputChange('ramSlots', e.target.value)}
                                        placeholder="VD: 2, 4"
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ramSpeed">Tốc độ RAM</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.ramSpeeds)}
                                        value={formData.ramSpeed || ''}
                                        onValueChange={(value) => onInputChange('ramSpeed', value)}
                                        placeholder="Chọn tốc độ RAM..."
                                        searchPlaceholder="Tìm kiếm tốc độ..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ramUpgradeable">Có thể nâng cấp</Label>
                                    <select
                                        value={formData.ramUpgradeable ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('ramUpgradeable', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Không</option>
                                        <option value="true">Có</option>
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
                                    <CardTitle>Storage - Ổ cứng</CardTitle>
                                    <CardDescription>Thông số chi tiết về ổ cứng</CardDescription>
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
                                    <Label htmlFor="storageType">Loại ổ cứng</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.storageTypes)}
                                        value={formData.storageType || ''}
                                        onValueChange={(value) => onInputChange('storageType', value)}
                                        placeholder="Chọn loại ổ cứng..."
                                        searchPlaceholder="Tìm kiếm loại ổ cứng..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="storageCapacityGB">Dung lượng</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.storageCapacities)}
                                        value={formData.storageCapacityGB || ''}
                                        onValueChange={(value) => onInputChange('storageCapacityGB', value)}
                                        placeholder="Chọn dung lượng..."
                                        searchPlaceholder="Tìm kiếm dung lượng..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="storageInterface">Giao tiếp</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.storageInterfaces)}
                                        value={formData.storageInterface || ''}
                                        onValueChange={(value) => onInputChange('storageInterface', value)}
                                        placeholder="Chọn giao tiếp..."
                                        searchPlaceholder="Tìm kiếm giao tiếp..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="nvMeSupport">Hỗ trợ NVMe</Label>
                                    <select
                                        value={formData.nvMeSupport ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('nvMeSupport', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Không</option>
                                        <option value="true">Có</option>
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
                                    <CardTitle>GPU - Card đồ họa</CardTitle>
                                    <CardDescription>Thông số chi tiết về card đồ họa</CardDescription>
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
                                    <Label htmlFor="gpuType">Loại GPU</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.gpuTypes)}
                                        value={formData.gpuType || ''}
                                        onValueChange={(value) => onInputChange('gpuType', value)}
                                        placeholder="Chọn loại GPU..."
                                        searchPlaceholder="Tìm kiếm loại GPU..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="gpuBrand">Thương hiệu GPU</Label>
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
                                        placeholder="Chọn thương hiệu GPU..."
                                        searchPlaceholder="Tìm kiếm thương hiệu..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="gpuModel">Model GPU</Label>
                                    <Combobox
                                        options={createOptions(gpuModelOptions)}
                                        value={formData.gpuModel || ''}
                                        onValueChange={(value) => onInputChange('gpuModel', value)}
                                        placeholder="Chọn model GPU..."
                                        searchPlaceholder="Tìm kiếm model..."
                                        disabled={!selectedGpuBrand || selectedGpuBrand === 'Khác'}
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="gpuVramGB">VRAM</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.gpuVramSizes)}
                                        value={formData.gpuVramGB || ''}
                                        onValueChange={(value) => onInputChange('gpuVramGB', value)}
                                        placeholder="Chọn VRAM..."
                                        searchPlaceholder="Tìm kiếm VRAM..."
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
                                    <CardTitle>Display - Màn hình</CardTitle>
                                    <CardDescription>Thông số chi tiết về màn hình</CardDescription>
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
                                    <Label htmlFor="displaySizeInches">Kích thước màn hình</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.displaySizes)}
                                        value={formData.displaySizeInches || ''}
                                        onValueChange={(value) => onInputChange('displaySizeInches', value)}
                                        placeholder="Chọn kích thước màn hình..."
                                        searchPlaceholder="Tìm kiếm kích thước..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="displayResolution">Độ phân giải</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.displayResolutions)}
                                        value={formData.displayResolution || ''}
                                        onValueChange={(value) => onInputChange('displayResolution', value)}
                                        placeholder="Chọn độ phân giải..."
                                        searchPlaceholder="Tìm kiếm độ phân giải..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="displayPanelType">Loại panel</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.displayPanels)}
                                        value={formData.displayPanelType || ''}
                                        onValueChange={(value) => onInputChange('displayPanelType', value)}
                                        placeholder="Chọn loại panel..."
                                        searchPlaceholder="Tìm kiếm loại panel..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="displayRefreshRateHz">Tần số quét</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.refreshRates)}
                                        value={formData.displayRefreshRateHz || ''}
                                        onValueChange={(value) => onInputChange('displayRefreshRateHz', value)}
                                        placeholder="Chọn tần số quét..."
                                        searchPlaceholder="Tìm kiếm tần số quét..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="displayTouchscreen">Cảm ứng</Label>
                                    <select
                                        value={formData.displayTouchscreen ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('displayTouchscreen', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Không</option>
                                        <option value="true">Có</option>
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
                                    <CardTitle>Physical - Thông số vật lý</CardTitle>
                                    <CardDescription>Kích thước, trọng lượng và các thông số vật lý</CardDescription>
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
                                    <Label htmlFor="batteryCapacityWh">Dung lượng pin</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.batteryCapacities)}
                                        value={formData.batteryCapacityWh || ''}
                                        onValueChange={(value) => onInputChange('batteryCapacityWh', value)}
                                        placeholder="Chọn dung lượng pin..."
                                        searchPlaceholder="Tìm kiếm dung lượng pin..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="weightKg">Trọng lượng</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.weightRanges)}
                                        value={formData.weightKg || ''}
                                        onValueChange={(value) => onInputChange('weightKg', value)}
                                        placeholder="Chọn trọng lượng..."
                                        searchPlaceholder="Tìm kiếm trọng lượng..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="color">Màu sắc</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.colors)}
                                        value={formData.color || ''}
                                        onValueChange={(value) => onInputChange('color', value)}
                                        placeholder="Chọn màu sắc..."
                                        searchPlaceholder="Tìm kiếm màu sắc..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="ports">Cổng kết nối</Label>
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
                                    <CardTitle>Connectivity - Kết nối</CardTitle>
                                    <CardDescription>WiFi, Bluetooth và các kết nối khác</CardDescription>
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
                                    <Label htmlFor="wiFi6Support">Hỗ trợ WiFi 6</Label>
                                    <select
                                        value={formData.wiFi6Support ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('wiFi6Support', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Không</option>
                                        <option value="true">Có</option>
                                    </select>
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="bluetoothSupport">Hỗ trợ Bluetooth</Label>
                                    <select
                                        value={formData.bluetoothSupport ? 'true' : 'false'}
                                        onChange={(e) => onInputChange('bluetoothSupport', e.target.value === 'true')}
                                        className="w-full p-2 border rounded-md"
                                    >
                                        <option value="false">Không</option>
                                        <option value="true">Có</option>
                                    </select>
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="bluetoothVersion">Phiên bản Bluetooth</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.bluetoothVersions)}
                                        value={formData.bluetoothVersion || ''}
                                        onValueChange={(value) => onInputChange('bluetoothVersion', value)}
                                        placeholder="Chọn phiên bản Bluetooth..."
                                        searchPlaceholder="Tìm kiếm phiên bản..."
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
                                    <CardTitle>Business - Thông tin kinh doanh</CardTitle>
                                    <CardDescription>Bảo hành và đối tượng khách hàng</CardDescription>
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
                                    <Label htmlFor="warrantyPeriod">Thời gian bảo hành</Label>
                                    <Combobox
                                        options={createOptions(PRODUCT_SPECS_OPTIONS.warrantyPeriods)}
                                        value={formData.warrantyPeriod || ''}
                                        onValueChange={(value) => onInputChange('warrantyPeriod', value)}
                                        placeholder="Chọn thời gian bảo hành..."
                                        searchPlaceholder="Tìm kiếm thời gian bảo hành..."
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="targetAudience">Đối tượng khách hàng</Label>
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
                                    <CardTitle>Kết nối & Cổng</CardTitle>
                                    <CardDescription>Thông tin về kết nối và cổng kết nối</CardDescription>
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
                                    <Label htmlFor="ports">Cổng kết nối</Label>
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
                                        <Label htmlFor="wiFi6Support">Hỗ trợ WiFi 6</Label>
                                    </div>
                                    <div className="flex items-center space-x-2">
                                        <input
                                            type="checkbox"
                                            id="bluetoothSupport"
                                            checked={formData.bluetoothSupport || false}
                                            onChange={(e) => onInputChange('bluetoothSupport', e.target.checked)}
                                        />
                                        <Label htmlFor="bluetoothSupport">Hỗ trợ Bluetooth</Label>
                                    </div>
                                </div>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="bluetoothVersion">Phiên bản Bluetooth</Label>
                                <Combobox
                                    options={createOptions(PRODUCT_SPECS_OPTIONS.bluetoothVersions)}
                                    value={formData.bluetoothVersion || ''}
                                    onValueChange={(value) => onInputChange('bluetoothVersion', value)}
                                    placeholder="Chọn phiên bản Bluetooth..."
                                    searchPlaceholder="Tìm kiếm phiên bản..."
                                />
                            </div>
                        </CardContent>
                    </CollapsibleContent>
                </Collapsible>
            </Card>
        </div>
    );
}
