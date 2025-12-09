'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { ProductFormData } from '@/lib/admin-api';
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
                                <CardTitle>Ci t nng cao</CardTitle>
                                <CardDescription>
                                    Qun l tags, thng s k thut v cc ci t khc
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
                                <Label>Thng tin b sung</Label>
                                <p className="text-sm text-muted-foreground mb-2">
                                    Cc trng thng tin b sung cho tng thch ngc
                                </p>
                            </div>


                            <div className="space-y-2">
                                <Label htmlFor="ports">Cng kt ni</Label>
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
