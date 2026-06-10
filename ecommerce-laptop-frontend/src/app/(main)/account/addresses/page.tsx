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
        fullName: 'Nguyễn Văn A',
        phoneNumber: '0123456789',
        address: '123 Nguyễn Huệ',
        wardCode: '00001',
        districtCode: '001',
        provinceCode: '79',
        postalCode: '700000',
        type: 'home',
        isDefault: true,
    },
    {
        id: '2',
        fullName: 'Nguyễn Văn A',
        phoneNumber: '0123456789',
        address: '456 Lê Lợi',
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
                country: 'Việt Nam',
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
                    toast.success('Cập nhật địa chỉ thành công');
                } catch (err) {
                    toast.error('Cập nhật địa chỉ thất bại');
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
                    toast.success('Thêm địa chỉ mới thành công');
                } catch (err) {
                    toast.error('Thêm địa chỉ thất bại');
                }
            }

            setIsDialogOpen(false);
            resetForm();
        } catch (error) {
            toast.error('Có lỗi xảy ra, vui lòng thử lại');
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async (addressId: string) => {
        try {
            const res = await userService.deleteAddress(addressId);
            if (res) {
                setAddresses(prev => prev.filter(addr => addr.id !== addressId));
                toast.success('Xóa địa chỉ thành công');
            } else {
                toast.error('Xóa địa chỉ thất bại');
            }
        } catch (error) {
            toast.error('Có lỗi xảy ra, vui lòng thử lại');
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
                toast.success('Đã đặt làm địa chỉ mặc định');
            } else {
                toast.error('Đặt mặc định thất bại');
            }
        } catch (error) {
            toast.error('Có lỗi xảy ra, vui lòng thử lại');
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
                return 'Nhà riêng';
            case 'office':
                return 'Văn phòng';
            default:
                return 'Khác';
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
                                <span>Sổ địa chỉ</span>
                            </CardTitle>
                            <CardDescription>
                                Quản lý địa chỉ giao hàng của bạn
                            </CardDescription>
                        </div>
                        <Dialog open={isDialogOpen} onOpenChange={(open) => {
                            setIsDialogOpen(open);
                            if (!open) resetForm();
                        }}>
                            <DialogTrigger asChild>
                                <Button>
                                    <Plus className="h-4 w-4 mr-2" />
                                    Thêm địa chỉ mới
                                </Button>
                            </DialogTrigger>
                            <DialogContent className="sm:max-w-[600px]">
                                <DialogHeader>
                                    <DialogTitle>
                                        {editingAddress ? 'Chỉnh sửa địa chỉ' : 'Thêm địa chỉ mới'}
                                    </DialogTitle>
                                    <DialogDescription>
                                        Điền thông tin địa chỉ giao hàng
                                    </DialogDescription>
                                </DialogHeader>
                                <form onSubmit={handleSave} className="space-y-4">
                                    <div className="grid gap-4 md:grid-cols-2">
                                        <div className="space-y-2">
                                            <Label htmlFor="fullName">Họ và tên *</Label>
                                            <Input
                                                id="fullName"
                                                value={addressForm.fullName}
                                                onChange={(e) => setAddressForm(prev => ({ ...prev, fullName: e.target.value }))}
                                                placeholder="Nhập họ và tên"
                                                required
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label htmlFor="phoneNumber">Số điện thoại *</Label>
                                            <Input
                                                id="phoneNumber"
                                                value={addressForm.phoneNumber}
                                                onChange={(e) => setAddressForm(prev => ({ ...prev, phoneNumber: e.target.value }))}
                                                placeholder="Nhập số điện thoại"
                                                required
                                            />
                                        </div>
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="address">Địa chỉ cụ thể *</Label>
                                        <Input
                                            id="address"
                                            value={addressForm.address}
                                            onChange={(e) => setAddressForm(prev => ({ ...prev, address: e.target.value }))}
                                            placeholder="Số nhà, tên đường"
                                            required
                                        />
                                    </div>

                                    <div className="grid gap-4 md:grid-cols-3">
                                        <div className="space-y-2">
                                            <Label>Tỉnh/Thành phố *</Label>
                                            <ProvinceSelect
                                                value={addressForm.provinceCode}
                                                onValueChange={(value) => setAddressForm(prev => ({
                                                    ...prev,
                                                    provinceCode: value,
                                                    districtCode: '',
                                                    wardCode: ''
                                                }))}
                                                placeholder="Chọn tỉnh/thành phố"
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label>Quận/Huyện *</Label>
                                            <DistrictSelect
                                                provinceCode={addressForm.provinceCode}
                                                value={addressForm.districtCode}
                                                onValueChange={(value) => setAddressForm(prev => ({
                                                    ...prev,
                                                    districtCode: value,
                                                    wardCode: ''
                                                }))}
                                                placeholder="Chọn quận/huyện"
                                                disabled={!addressForm.provinceCode}
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label>Phường/Xã *</Label>
                                            <WardSelect
                                                districtCode={addressForm.districtCode}
                                                value={addressForm.wardCode}
                                                onValueChange={(value) => setAddressForm(prev => ({
                                                    ...prev,
                                                    wardCode: value
                                                }))}
                                                placeholder="Chọn phường/xã"
                                                disabled={!addressForm.districtCode}
                                            />
                                        </div>
                                    </div>

                                    <div className="grid gap-4 md:grid-cols-2">
                                        <div className="space-y-2">
                                            <Label htmlFor="postalCode">Mã bưu điện</Label>
                                            <Input
                                                id="postalCode"
                                                value={addressForm.postalCode}
                                                onChange={(e) => setAddressForm(prev => ({ ...prev, postalCode: e.target.value }))}
                                                placeholder="Nhập mã bưu điện"
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label htmlFor="type">Loại địa chỉ</Label>
                                            <Select value={addressForm.type} onValueChange={(value: 'home' | 'office' | 'other') => setAddressForm(prev => ({ ...prev, type: value }))}>
                                                <SelectTrigger>
                                                    <SelectValue placeholder="Chọn loại địa chỉ" />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    <SelectItem value="home">Nhà riêng</SelectItem>
                                                    <SelectItem value="office">Văn phòng</SelectItem>
                                                    <SelectItem value="other">Khác</SelectItem>
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
                                            Đặt làm địa chỉ mặc định
                                        </Label>
                                    </div>

                                    <DialogFooter>
                                        <Button
                                            type="button"
                                            variant="outline"
                                            onClick={() => setIsDialogOpen(false)}
                                        >
                                            Hủy
                                        </Button>
                                        <Button type="submit" disabled={isSaving}>
                                            {isSaving ? 'đang lưu...' : 'Lưu địa chỉ'}
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
                                Chưa có địa chỉ nào
                            </h3>
                            <p className="text-gray-500 mb-4">
                                Thêm địa chỉ giao hàng để đặt hàng nhanh hơn
                            </p>
                            <Button onClick={() => setIsDialogOpen(true)}>
                                <Plus className="h-4 w-4 mr-2" />
                                Thêm địa chỉ đầu tiên
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
                                                            Mặc định
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
                                                        Đặt mặc định
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
                                                            <AlertDialogTitle>Xóa địa chỉ</AlertDialogTitle>
                                                            <AlertDialogDescription>
                                                                Bạn có chắc chắn muốn xóa địa chỉ này? Hành động này không thể hoàn tác.
                                                            </AlertDialogDescription>
                                                        </AlertDialogHeader>
                                                        <AlertDialogFooter>
                                                            <AlertDialogCancel>Hủy</AlertDialogCancel>
                                                            <AlertDialogAction
                                                                onClick={() => handleDelete(address.id)}
                                                                className="bg-red-600 hover:bg-red-700"
                                                            >
                                                                Xóa
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