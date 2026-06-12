'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { ProductFormData } from '@/features/admin/products/types';
import { ChevronDown, ChevronRight } from 'lucide-react';

interface AdvancedSettingsSectionProps {
    formData: ProductFormData;
    onInputChange: (field: keyof ProductFormData, value: any) => void;
    validationErrors: Record<string, string>;
    isExpanded: boolean;
    onToggle: () => void;
}

export default function AdvancedSettingsSection({
    formData,
    onInputChange,
    validationErrors,
    isExpanded,
    onToggle
}: AdvancedSettingsSectionProps) {

    return (
        <Card>
            <Collapsible open={isExpanded} onOpenChange={onToggle}>
                <CollapsibleTrigger asChild>
                    <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                        <div className="flex items-center justify-between">
                            <div>
                                <CardTitle>Cài đặt nâng cao</CardTitle>
                                <CardDescription>
                                    Quản lý tags, thông số kỹ thuật và các cài đặt khác
                                </CardDescription>
                            </div>
                            {isExpanded ? (
                                <ChevronDown className="h-5 w-5 text-muted-foreground" />
                            ) : (
                                <ChevronRight className="h-5 w-5 text-muted-foreground" />
                            )}
                        </div>
                    </CardHeader>
                </CollapsibleTrigger>
                <CollapsibleContent>
                    <CardContent className="space-y-6">

                        {/* Legacy Fields for Backward Compatibility */}
                        <div className="space-y-4">
                            <div>
                                <Label>Thông tin bổ sung</Label>
                                <p className="text-sm text-muted-foreground mb-2">
                                    Các trường thông tin bổ sung cho tương thích ngược
                                </p>
                            </div>


                            <div className="space-y-2">
                                <Label htmlFor="ports">Cổng kết nối</Label>
                                <Textarea
                                    id="ports"
                                    value={formData.ports || ''}
                                    onChange={(e) => onInputChange('ports', e.target.value)}
                                    placeholder="VD: 2x USB 3.2, 1x USB-C Thunderbolt 4, HDMI 2.1, 3.5mm Audio"
                                    rows={2}
                                />
                            </div>
                        </div>
                    </CardContent>
                </CollapsibleContent>
            </Collapsible>
        </Card>
    );
}
