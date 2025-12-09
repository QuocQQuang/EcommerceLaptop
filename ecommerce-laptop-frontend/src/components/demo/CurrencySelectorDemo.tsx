'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { CurrencySelector } from '@/components/ui/currency-selector';

export function CurrencySelectorDemo() {
    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle>CurrencySelector Variants</CardTitle>
                    <CardDescription>
                        Cc kiu hin th khc nhau ca CurrencySelector
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                    {/* Toggle Variant */}
                    <div>
                        <h4 className="font-medium mb-2">Toggle (mc nh)</h4>
                        <p className="text-sm text-muted-foreground mb-3">
                            Nt toggle lin nhau, ph hp cho header
                        </p>
                        <CurrencySelector variant="toggle" showRefreshButton={true} />
                    </div>

                    {/* Simple Variant */}
                    <div>
                        <h4 className="font-medium mb-2">Simple</h4>
                        <p className="text-sm text-muted-foreground mb-3">
                            Nt ring bit n gin, d s dng
                        </p>
                        <CurrencySelector variant="simple" showRefreshButton={true} />
                    </div>

                    {/* Buttons Variant */}
                    <div>
                        <h4 className="font-medium mb-2">Buttons</h4>
                        <p className="text-sm text-muted-foreground mb-3">
                            Nt vi icon v symbol y , thng tin chi tit
                        </p>
                        <CurrencySelector variant="buttons" showRefreshButton={true} />
                    </div>

                    {/* Without Refresh Button */}
                    <div>
                        <h4 className="font-medium mb-2">Khng c nt refresh</h4>
                        <p className="text-sm text-muted-foreground mb-3">
                            Ch c nt chuyn i tin t
                        </p>
                        <CurrencySelector variant="simple" showRefreshButton={false} />
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
