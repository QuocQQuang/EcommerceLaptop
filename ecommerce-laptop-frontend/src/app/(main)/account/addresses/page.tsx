'use client';

import { DistrictSelect, ProvinceSelect, WardSelect } from '@/components/organisms/AddressSelect';
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { userService } from '@/services/userService';
import { vietnamAddressService } from '@/utils/vietnam-address';
import {
    Building,
    Edit3,
    Home,
    MapPin,
    Plus,
    Star,
    Trash2
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface Address {
    id: string;
    fullName: string;
    phoneNumber: string;
    address: string;
    wardCode: string;
    districtCode: string;
    provinceCode: string;
    postalCode: string;
    type: 'home' | 'office' | 'other';
    isDefault: boolean;
}

const mockAddresses: Address[] = [
    {
        id: '1',
        fullName: 'Nguyn Vn A',
        phoneNumber: '0123456789',
        address: '123 Nguyn Hu',
        wardCode: '00001',
        districtCode: '001',
        provinceCode: '79',
        postalCode: '700000',
        type: 'home',
        isDefault: true,
    },
    {
        id: '2',
        fullName: 'Nguyn Vn A',
        phoneNumber: '0123456789',
        address: '456 L Li',
        wardCode: '00002',
        districtCode: '001',
        provinceCode: '79',
        postalCode: '700000',
        type: 'office',
        isDefault: false,
    },
];

export default function AddressesPage() {
    const [addresses, setAddresses] = useState<Address[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [editingAddress, setEditingAddress] = useState<Address | null>(null);
    const [isDialogOpen, setIsDialogOpen] = useState(false);

    // Helper function to format address display
    const formatAddressDisplay = (address: Address) => {
        const province = vietnamAddressService.getProvinceByCode(address.provinceCode);
        const district = vietnamAddressService.getDistrictByCode(address.districtCode);
        const ward = vietnamAddressService.getWardByCode(address.wardCode);

        const parts = [
            address.address,
            ward?.name || address.wardCode,
            district?.name || address.districtCode,
            province?.name || address.provinceCode
        ].filter(Boolean);

        return parts.join(', ') + (address.postalCode ? ` ${address.postalCode}` : '');
    };

    // Form state
    const [addressForm, setAddressForm] = useState({
        fullName: '',
        phoneNumber: '',
        address: '',
        wardCode: '',
        districtCode: '',
        provinceCode: '',
        postalCode: '',
        type: 'home' as 'home' | 'office' | 'other',
        isDefault: false,
    });

    useEffect(() => {
        let mounted = true;
        const load = async () => {
            try {
                const res = await userService.getAddresses();
                if (!mounted) return;
                // map API address shape to local Address shape if needed
                const list = (res?.data || []).map((a: any) => {
                    // Try to find codes from names if not provided
                    let provinceCode = a.provinceCode || '';
                    let districtCode = a.districtCode || '';
                    let wardCode = a.wardCode || '';

                    // If we have names but no codes, try to find them
                    if (!provinceCode && a.province) {
                        const province = vietnamAddressService.getProvinceByName(a.province);
                        provinceCode = province?.code || '';
                    }
                    if (!districtCode && a.city) {
                        const district = vietnamAddressService.getDistrictByName(a.city, provinceCode);
                        districtCode = district?.code || '';
                    }
                    if (!wardCode && a.district) {
                        const ward = vietnamAddressService.getWardByName(a.district, districtCode);
                        wardCode = ward?.code || '';
                    }

                    return {
                        id: String(a.id || a._id || Date.now()),
                        fullName: a.fullName || a.recipientName || '',
                        phoneNumber: a.phoneNumber || a.phone || '',
                        address: a.street || a.address || '',
                        wardCode: wardCode,
                        districtCode: districtCode,
                        provinceCode: provinceCode,
                        postalCode: a.postalCode || a.zipCode || '',
                        type: a.type || 'home',
                        isDefault: !!a.isDefault
                    };
                });

                setAddresses(list);
            } catch (err) {
                // fallback to mock if API fails
                setAddresses(mockAddresses);
            } finally {
                if (mounted) setIsLoading(false);
            }
        };

        load();

        return () => { mounted = false; };
    }, []);

    const resetForm = () => {
        setAddressForm({
            fullName: '',
            phoneNumber: '',
            address: '',
            wardCode: '',
            districtCode: '',
            provinceCode: '',
            postalCode: '',
            type: 'home',
            isDefault: false,
        });
        setEditingAddress(null);
    };

    const handleEdit = (address: Address) => {
        setEditingAddress(address);
        setAddressForm({
            fullName: address.fullName,
            phoneNumber: address.phoneNumber,
            address: address.address,
            wardCode: address.wardCode,
            districtCode: address.districtCode,
            provinceCode: address.provinceCode,
            postalCode: address.postalCode,
            type: address.type,
            isDefault: address.isDefault,
        });
        setIsDialogOpen(true);
    };

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSaving(true);

        try {
            // Map codes to names for required fields
            const provinceObj = vietnamAddressService.getProvinceByCode(addressForm.provinceCode);
            const districtObj = vietnamAddressService.getDistrictByCode(addressForm.districtCode);
            const wardObj = vietnamAddressService.getWardByCode(addressForm.wardCode);

            const payload = {
                fullName: addressForm.fullName,
                phoneNumber: addressForm.phoneNumber,
                street: addressForm.address,
                city: districtObj?.name || '', // Huyn/Qun  City
                province: provinceObj?.name || '', // Tnh  Province  
                district: wardObj?.name || '', // X/Phng  District
                postalCode: addressForm.postalCode,
                country: 'Vit Nam',
                isDefault: addressForm.isDefault,
                // Keep original codes for frontend state management
                wardCode: addressForm.wardCode,
                districtCode: addressForm.districtCode,
                provinceCode: addressForm.provinceCode,
                type: addressForm.type,
            };

            if (editingAddress) {
                // Update existing address via API
                try {
                    await userService.updateAddress(editingAddress.id, payload as any);
                    setAddresses(prev => prev.map(addr =>
                        addr.id === editingAddress.id
                            ? { ...addr, ...addressForm }
                            : addressForm.isDefault ? { ...addr, isDefault: false } : addr
                    ));
                    toast.success('Cp nht a ch thnh cng');
                } catch (err) {
                    toast.error('Cp nht a ch tht bi');
                }
            } else {
                // Add new address via API
                try {
                    const res = await userService.addAddress(payload as any);
                    const created = res?.data as { id?: number | string };
                    const newAddress: Address = {
                        id: String(created?.id || Date.now()),
                        fullName: addressForm.fullName,
                        phoneNumber: addressForm.phoneNumber,
                        address: addressForm.address,
                        wardCode: addressForm.wardCode,
                        districtCode: addressForm.districtCode,
                        provinceCode: addressForm.provinceCode,
                        postalCode: addressForm.postalCode,
                        type: addressForm.type,
                        isDefault: addressForm.isDefault,
                    };
                    setAddresses(prev => addressForm.isDefault
                        ? [newAddress, ...prev.map(addr => ({ ...addr, isDefault: false }))]
                        : [newAddress, ...prev]
                    );
                    toast.success('Thm a ch mi thnh cng');
                } catch (err) {
                    toast.error('Thm a ch tht bi');
                }
            }

            setIsDialogOpen(false);
            resetForm();
        } catch (error) {
            toast.error('C li xy ra, vui lng th li');
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async (addressId: string) => {
        try {
            const res = await userService.deleteAddress(addressId);
            if (res) {
                setAddresses(prev => prev.filter(addr => addr.id !== addressId));
                toast.success('Xa a ch thnh cng');
            } else {
                toast.error('Xa a ch tht bi');
            }
        } catch (error) {
            toast.error('C li xy ra, vui lng th li');
        }
    };

    const handleSetDefault = async (addressId: string) => {
        try {
            const res = await userService.updateAddress(addressId, { isDefault: true } as any);
            if (res) {
                setAddresses(prev => prev.map(addr => ({
                    ...addr,
                    isDefault: addr.id === addressId
                })));
                toast.success(' t lm a ch mc nh');
            } else {
                toast.error('t mc nh tht bi');
            }
        } catch (error) {
            toast.error('C li xy ra, vui lng th li');
        }
    };

    const getAddressTypeIcon = (type: string) => {
        switch (type) {
            case 'home':
                return <Home className="h-4 w-4" />;
            case 'office':
                return <Building className="h-4 w-4" />;
            default:
                return <MapPin className="h-4 w-4" />;
        }
    };

    const getAddressTypeLabel = (type: string) => {
        switch (type) {
            case 'home':
                return 'Nh ring';
            case 'office':
                return 'Vn phng';
            default:
                return 'Khc';
        }
    };

    if (isLoading) {
        return (
            <div className="space-y-6">
                <Card>
                    <CardHeader>
                        <div className="h-6 bg-gray-200 rounded w-32 animate-pulse"></div>
                        <div className="h-4 bg-gray-200 rounded w-48 animate-pulse"></div>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-4">
                            {[...Array(3)].map((_, i) => (
                                <div key={i} className="h-32 bg-gray-200 rounded animate-pulse"></div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div>
                            <CardTitle className="flex items-center space-x-2">
                                <MapPin className="h-6 w-6" />
                                <span>S a ch</span>
                            </CardTitle>
                            <CardDescription>
                                Qun l a ch giao hng ca bn
                            </CardDescription>
                        </div>
                        <Dialog open={isDialogOpen} onOpenChange={(open) => {
                            setIsDialogOpen(open);
                            if (!open) resetForm();
                        }}>
                            <DialogTrigger asChild>
                                <Button>
                                    <Plus className="h-4 w-4 mr-2" />
                                    Thm a ch mi
                                </Button>
                            </DialogTrigger>
                            <DialogContent className="sm:max-w-[600px]">
                                <DialogHeader>
                                    <DialogTitle>
                                        {editingAddress ? 'Chnh sa a ch' : 'Thm a ch mi'}
                                    </DialogTitle>
                                    <DialogDescription>
                                        in thng tin a ch giao hng
                                    </DialogDescription>
                                </DialogHeader>
                                <form onSubmit={handleSave} className="space-y-4">
                                    <div className="grid gap-4 md:grid-cols-2">
                                        <div className="space-y-2">
                                            <Label htmlFor="fullName">H v tn *</Label>
                                            <Input
                                                id="fullName"
                                                value={addressForm.fullName}
                                                onChange={(e) => setAddressForm(prev => ({ ...prev, fullName: e.target.value }))}
                                                placeholder="Nhp h v tn"
                                                required
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label htmlFor="phoneNumber">S in thoi *</Label>
                                            <Input
                                                id="phoneNumber"
                                                value={addressForm.phoneNumber}
                                                onChange={(e) => setAddressForm(prev => ({ ...prev, phoneNumber: e.target.value }))}
                                                placeholder="Nhp s in thoi"
                                                required
                                            />
                                        </div>
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="address">a ch c th *</Label>
                                        <Input
                                            id="address"
                                            value={addressForm.address}
                                            onChange={(e) => setAddressForm(prev => ({ ...prev, address: e.target.value }))}
                                            placeholder="S nh, tn ng"
                                            required
                                        />
                                    </div>

                                    <div className="grid gap-4 md:grid-cols-3">
                                        <div className="space-y-2">
                                            <Label>Tnh/Thnh ph *</Label>
                                            <ProvinceSelect
                                                value={addressForm.provinceCode}
                                                onValueChange={(value) => setAddressForm(prev => ({
                                                    ...prev,
                                                    provinceCode: value,
                                                    districtCode: '',
                                                    wardCode: ''
                                                }))}
                                                placeholder="Chn tnh/thnh ph"
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label>Qun/Huyn *</Label>
                                            <DistrictSelect
                                                provinceCode={addressForm.provinceCode}
                                                value={addressForm.districtCode}
                                                onValueChange={(value) => setAddressForm(prev => ({
                                                    ...prev,
                                                    districtCode: value,
                                                    wardCode: ''
                                                }))}
                                                placeholder="Chn qun/huyn"
                                                disabled={!addressForm.provinceCode}
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label>Phng/X *</Label>
                                            <WardSelect
                                                districtCode={addressForm.districtCode}
                                                value={addressForm.wardCode}
                                                onValueChange={(value) => setAddressForm(prev => ({
                                                    ...prev,
                                                    wardCode: value
                                                }))}
                                                placeholder="Chn phng/x"
                                                disabled={!addressForm.districtCode}
                                            />
                                        </div>
                                    </div>

                                    <div className="grid gap-4 md:grid-cols-2">
                                        <div className="space-y-2">
                                            <Label htmlFor="postalCode">M bu in</Label>
                                            <Input
                                                id="postalCode"
                                                value={addressForm.postalCode}
                                                onChange={(e) => setAddressForm(prev => ({ ...prev, postalCode: e.target.value }))}
                                                placeholder="Nhp m bu in"
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label htmlFor="type">Loi a ch</Label>
                                            <Select value={addressForm.type} onValueChange={(value: 'home' | 'office' | 'other') => setAddressForm(prev => ({ ...prev, type: value }))}>
                                                <SelectTrigger>
                                                    <SelectValue placeholder="Chn loi a ch" />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    <SelectItem value="home">Nh ring</SelectItem>
                                                    <SelectItem value="office">Vn phng</SelectItem>
                                                    <SelectItem value="other">Khc</SelectItem>
                                                </SelectContent>
                                            </Select>
                                        </div>
                                    </div>

                                    <div className="flex items-center space-x-2">
                                        <Checkbox
                                            id="isDefault"
                                            checked={addressForm.isDefault}
                                            onCheckedChange={(checked) => setAddressForm(prev => ({ ...prev, isDefault: !!checked }))}
                                        />
                                        <Label htmlFor="isDefault" className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
                                            t lm a ch mc nh
                                        </Label>
                                    </div>

                                    <DialogFooter>
                                        <Button
                                            type="button"
                                            variant="outline"
                                            onClick={() => setIsDialogOpen(false)}
                                        >
                                            Hy
                                        </Button>
                                        <Button type="submit" disabled={isSaving}>
                                            {isSaving ? 'ang lu...' : 'Lu a ch'}
                                        </Button>
                                    </DialogFooter>
                                </form>
                            </DialogContent>
                        </Dialog>
                    </div>
                </CardHeader>
                <CardContent>
                    {addresses.length === 0 ? (
                        <div className="text-center py-12">
                            <MapPin className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-900 mb-2">
                                Cha c a ch no
                            </h3>
                            <p className="text-gray-500 mb-4">
                                Thm a ch giao hng  t hng nhanh hn
                            </p>
                            <Button onClick={() => setIsDialogOpen(true)}>
                                <Plus className="h-4 w-4 mr-2" />
                                Thm a ch u tin
                            </Button>
                        </div>
                    ) : (
                        <div className="space-y-4">
                            {addresses.map((address) => (
                                <Card key={address.id} className={`relative ${address.isDefault ? 'ring-2 ring-blue-500' : ''}`}>
                                    <CardContent className="pt-6">
                                        <div className="flex items-start justify-between">
                                            <div className="flex-1">
                                                <div className="flex items-center space-x-2 mb-2">
                                                    <div className="flex items-center space-x-1 text-gray-600">
                                                        {getAddressTypeIcon(address.type)}
                                                        <span className="text-sm font-medium">
                                                            {getAddressTypeLabel(address.type)}
                                                        </span>
                                                    </div>
                                                    {address.isDefault && (
                                                        <Badge variant="default" className="bg-blue-100 text-blue-800">
                                                            <Star className="h-3 w-3 mr-1" />
                                                            Mc nh
                                                        </Badge>
                                                    )}
                                                </div>

                                                <h3 className="font-semibold text-gray-900 mb-1">
                                                    {address.fullName}
                                                </h3>
                                                <p className="text-sm text-gray-600 mb-1">
                                                    {address.phoneNumber}
                                                </p>
                                                <p className="text-sm text-gray-700">
                                                    {formatAddressDisplay(address)}
                                                </p>
                                            </div>

                                            <div className="flex items-center space-x-2 ml-4">
                                                {!address.isDefault && (
                                                    <Button
                                                        variant="outline"
                                                        size="sm"
                                                        onClick={() => handleSetDefault(address.id)}
                                                    >
                                                        t mc nh
                                                    </Button>
                                                )}
                                                <Button
                                                    variant="outline"
                                                    size="sm"
                                                    onClick={() => handleEdit(address)}
                                                >
                                                    <Edit3 className="h-4 w-4" />
                                                </Button>
                                                <AlertDialog>
                                                    <AlertDialogTrigger asChild>
                                                        <Button variant="outline" size="sm">
                                                            <Trash2 className="h-4 w-4" />
                                                        </Button>
                                                    </AlertDialogTrigger>
                                                    <AlertDialogContent>
                                                        <AlertDialogHeader>
                                                            <AlertDialogTitle>Xa a ch</AlertDialogTitle>
                                                            <AlertDialogDescription>
                                                                Bn c chc chn mun xa a ch ny? Hnh ng ny khng th hon tc.
                                                            </AlertDialogDescription>
                                                        </AlertDialogHeader>
                                                        <AlertDialogFooter>
                                                            <AlertDialogCancel>Hy</AlertDialogCancel>
                                                            <AlertDialogAction
                                                                onClick={() => handleDelete(address.id)}
                                                                className="bg-red-600 hover:bg-red-700"
                                                            >
                                                                Xa
                                                            </AlertDialogAction>
                                                        </AlertDialogFooter>
                                                    </AlertDialogContent>
                                                </AlertDialog>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}