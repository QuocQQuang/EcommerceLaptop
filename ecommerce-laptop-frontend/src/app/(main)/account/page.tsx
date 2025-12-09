'use client';

import { ChangePasswordForm } from '@/components/account/ChangePasswordForm';
import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { orderService } from '@/services/orderService';
import { userService } from '@/services/userService';
import {
    AlertCircle,
    Calendar,
    CheckCircle,
    Edit,
    Heart,
    Lock,
    Mail,
    MapPin,
    Package,
    Phone,
    Save,
    Settings,
    User,
    X
} from 'lucide-react';
import { useSession } from 'next-auth/react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface UserProfile {
    id: number; // Changed from string to number to match backend
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber?: string;
    dateOfBirth?: string;
    gender?: string;
    address?: {
        street: string;
        city: string;
        district: string;
        ward: string;
        zipCode: string;
    };
    createdAt: string;
}

interface Order {
    id: string;
    orderNumber: string;
    status: string;
    totalAmount: number;
    createdAt: string;
    items: Array<{
        productName: string;
        quantity: number;
        price: number;
    }>;
}

export default function AccountPage() {
    const { selectedCurrency } = useCurrencyContext();
    const { data: session, status } = useSession();
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [profile, setProfile] = useState<UserProfile | null>(null);
    const [orders, setOrders] = useState<Order[]>([]);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [editMode, setEditMode] = useState(false);
    const [formData, setFormData] = useState<Partial<UserProfile>>({});
    const [showChangePassword, setShowChangePassword] = useState(false);

    useEffect(() => {
        if (session?.user) {
            loadUserData();
        }
    }, [session]);

    const loadUserData = async () => {
        try {
            setLoading(true);
            const profileResponse = await userService.getProfile();
            setProfile(profileResponse.data);
            setFormData(profileResponse.data);

            // Get orders with customer ID from profile
            if (profileResponse.data?.id) {
                const ordersResponse = await orderService.getUserOrders(
                    profileResponse.data.id,
                    1,
                    5
                );
                setOrders(ordersResponse.items || []); // Handle PaginatedResponse structure
            }
        } catch (err: any) {
            setError('Khng th ti thng tin ti khon');
        } finally {
            setLoading(false);
        }
    };

    const handleSaveProfile = async () => {
        try {
            setSaving(true);
            setError('');

            const response = await userService.updateProfile(formData);
            setProfile(response.data);
            setSuccess('Cp nht thng tin thnh cng');
            setEditMode(false);

            setTimeout(() => setSuccess(''), 3000);
        } catch (err: any) {
            setError('Khng th cp nht thng tin');
        } finally {
            setSaving(false);
        }
    };

    const getStatusColor = (status: string) => {
        const key = (status || '').toLowerCase();
        switch (key) {
            case 'pending': return 'text-yellow-600 bg-yellow-100';
            case 'confirmed': return 'text-blue-600 bg-blue-100';
            case 'processing': return 'text-purple-600 bg-purple-100';
            case 'shipped': return 'text-indigo-600 bg-indigo-100';
            case 'delivered': return 'text-green-600 bg-green-100';
            case 'cancelled': return 'text-red-600 bg-red-100';
            default: return 'text-gray-600 bg-gray-100';
        }
    };

    const getStatusText = (status: string) => {
        const statusMap: { [key: string]: string } = {
            pending: 'Ch xc nhn',
            confirmed: ' xc nhn',
            processing: 'ang x l',
            shipped: 'ang giao hng',
            delivered: ' nhn hng',
            cancelled: ' hy'
        };
        const key = (status || '').toLowerCase();
        return statusMap[key] || status;
    };

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50 py-8">
            <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
                {/* Header */}
                <div className="mb-8">
                    <h1 className="text-3xl font-bold text-gray-900 mb-2">
                        Ti khon ca ti
                    </h1>
                    <p className="text-gray-600">
                        Qun l thng tin c nhn v n hng ca bn
                    </p>
                </div>

                {/* Alerts */}
                {error && (
                    <Alert variant="destructive" className="mb-6">
                        <AlertCircle className="h-4 w-4" />
                        <AlertDescription>{error}</AlertDescription>
                    </Alert>
                )}

                {success && (
                    <Alert variant="default" className="mb-6 border-green-200 bg-green-50">
                        <CheckCircle className="h-4 w-4 text-green-600" />
                        <AlertDescription className="text-green-800">{success}</AlertDescription>
                    </Alert>
                )}

                <Tabs defaultValue="profile" className="space-y-6">
                    <TabsList className="grid w-full grid-cols-4">
                        <TabsTrigger value="profile" className="flex items-center">
                            <User className="h-4 w-4 mr-2" />
                            Thng tin c nhn
                        </TabsTrigger>
                        <TabsTrigger value="orders" className="flex items-center">
                            <Package className="h-4 w-4 mr-2" />
                            n hng
                        </TabsTrigger>
                        <TabsTrigger value="wishlist" className="flex items-center">
                            <Heart className="h-4 w-4 mr-2" />
                            Yu thch
                        </TabsTrigger>
                        <TabsTrigger value="settings" className="flex items-center">
                            <Settings className="h-4 w-4 mr-2" />
                            Ci t
                        </TabsTrigger>
                    </TabsList>

                    {/* Profile Tab */}
                    <TabsContent value="profile">
                        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                            {/* Basic Information */}
                            <Card>
                                <CardHeader className="flex flex-row items-center justify-between">
                                    <div>
                                        <CardTitle>Thng tin c bn</CardTitle>
                                        <CardDescription>
                                            Cp nht thng tin c nhn ca bn
                                        </CardDescription>
                                    </div>
                                    {!editMode ? (
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => setEditMode(true)}
                                        >
                                            <Edit className="h-4 w-4 mr-2" />
                                            Chnh sa
                                        </Button>
                                    ) : (
                                        <div className="flex space-x-2">
                                            <Button
                                                variant="outline"
                                                size="sm"
                                                onClick={() => {
                                                    setEditMode(false);
                                                    setFormData(profile || {});
                                                }}
                                            >
                                                <X className="h-4 w-4" />
                                            </Button>
                                            <Button
                                                size="sm"
                                                onClick={handleSaveProfile}
                                                disabled={saving}
                                            >
                                                {saving ? (
                                                    <LoadingSpinner size="sm" className="mr-2" />
                                                ) : (
                                                    <Save className="h-4 w-4 mr-2" />
                                                )}
                                                Lu
                                            </Button>
                                        </div>
                                    )}
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    <div className="grid grid-cols-2 gap-4">
                                        <div className="space-y-2">
                                            <Label htmlFor="firstName">H</Label>
                                            <Input
                                                id="firstName"
                                                value={formData.firstName || ''}
                                                onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                                                disabled={!editMode}
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label htmlFor="lastName">Tn</Label>
                                            <Input
                                                id="lastName"
                                                value={formData.lastName || ''}
                                                onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                                                disabled={!editMode}
                                            />
                                        </div>
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="email">Email</Label>
                                        <div className="relative">
                                            <Mail className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
                                            <Input
                                                id="email"
                                                value={formData.email || ''}
                                                disabled
                                                className="pl-10"
                                            />
                                        </div>
                                        {/* Resend Confirmation */}
                                        <div className="flex items-center gap-3">
                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                onClick={async () => {
                                                    try {
                                                        const email = formData.email || profile?.email;
                                                        if (!email) {
                                                            toast.error('Khng tm thy email ti khon');
                                                            return;
                                                        }
                                                        const res = await userService.resendEmailConfirmation(email);
                                                        if (res.success) {
                                                            toast.success(res.message || ' gi li email xc thc');
                                                        } else {
                                                            toast.error(res.message || 'Gi li email xc thc tht bi');
                                                        }
                                                    } catch (e: any) {
                                                        toast.error(e?.response?.data?.message || 'Khng th gi li email xc thc');
                                                    }
                                                }}
                                            >
                                                Gi li email xc thc
                                            </Button>
                                        </div>
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="phoneNumber">S in thoi</Label>
                                        <div className="relative">
                                            <Phone className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
                                            <Input
                                                id="phoneNumber"
                                                value={formData.phoneNumber || ''}
                                                onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                                                disabled={!editMode}
                                                className="pl-10"
                                                placeholder="Nhp s in thoi"
                                            />
                                        </div>
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="dateOfBirth">Ngy sinh</Label>
                                        <div className="relative">
                                            <Calendar className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
                                            <Input
                                                id="dateOfBirth"
                                                type="date"
                                                value={formData.dateOfBirth || ''}
                                                onChange={(e) => setFormData({ ...formData, dateOfBirth: e.target.value })}
                                                disabled={!editMode}
                                                className="pl-10"
                                            />
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>

                            {/* Address Information */}
                            <Card>
                                <CardHeader>
                                    <CardTitle>a ch</CardTitle>
                                    <CardDescription>
                                        a ch giao hng mc nh
                                    </CardDescription>
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    <div className="space-y-2">
                                        <Label htmlFor="street">a ch chi tit</Label>
                                        <div className="relative">
                                            <MapPin className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
                                            <Input
                                                id="street"
                                                value={formData.address?.street || ''}
                                                onChange={(e) => setFormData({
                                                    ...formData,
                                                    address: { ...formData.address, street: e.target.value } as any
                                                })}
                                                disabled={!editMode}
                                                className="pl-10"
                                                placeholder="S nh, tn ng"
                                            />
                                        </div>
                                    </div>

                                    <div className="grid grid-cols-2 gap-4">
                                        <div className="space-y-2">
                                            <Label htmlFor="ward">Phng/X</Label>
                                            <Input
                                                id="ward"
                                                value={formData.address?.ward || ''}
                                                onChange={(e) => setFormData({
                                                    ...formData,
                                                    address: { ...formData.address, ward: e.target.value } as any
                                                })}
                                                disabled={!editMode}
                                                placeholder="Phng/X"
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label htmlFor="district">Qun/Huyn</Label>
                                            <Input
                                                id="district"
                                                value={formData.address?.district || ''}
                                                onChange={(e) => setFormData({
                                                    ...formData,
                                                    address: { ...formData.address, district: e.target.value } as any
                                                })}
                                                disabled={!editMode}
                                                placeholder="Qun/Huyn"
                                            />
                                        </div>
                                    </div>

                                    <div className="grid grid-cols-2 gap-4">
                                        <div className="space-y-2">
                                            <Label htmlFor="city">Tnh/Thnh ph</Label>
                                            <Input
                                                id="city"
                                                value={formData.address?.city || ''}
                                                onChange={(e) => setFormData({
                                                    ...formData,
                                                    address: { ...formData.address, city: e.target.value } as any
                                                })}
                                                disabled={!editMode}
                                                placeholder="Tnh/Thnh ph"
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label htmlFor="zipCode">M bu in</Label>
                                            <Input
                                                id="zipCode"
                                                value={formData.address?.zipCode || ''}
                                                onChange={(e) => setFormData({
                                                    ...formData,
                                                    address: { ...formData.address, zipCode: e.target.value } as any
                                                })}
                                                disabled={!editMode}
                                                placeholder="M bu in"
                                            />
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        </div>
                    </TabsContent>

                    {/* Orders Tab */}
                    <TabsContent value="orders">
                        <Card>
                            <CardHeader>
                                <CardTitle>n hng ca ti</CardTitle>
                                <CardDescription>
                                    Theo di trng thi n hng v lch s mua hng
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                {orders.length === 0 ? (
                                    <div className="text-center py-8">
                                        <Package className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                                        <h3 className="text-lg font-medium text-gray-900 mb-2">
                                            Cha c n hng no
                                        </h3>
                                        <p className="text-gray-500">
                                            Bn cha c n hng no. Hy bt u mua sm ngay!
                                        </p>
                                    </div>
                                ) : (
                                    <div className="space-y-4">
                                        {orders.map((order) => (
                                            <div key={order.id} className="border border-gray-200 rounded-lg p-4">
                                                <div className="flex justify-between items-start mb-3">
                                                    <div>
                                                        <h4 className="font-medium text-gray-900">
                                                            n hng #{order.orderNumber}
                                                        </h4>
                                                        <p className="text-sm text-gray-500">
                                                            {new Date(order.createdAt).toLocaleDateString('vi-VN')}
                                                        </p>
                                                    </div>
                                                    <span className={`px-2 py-1 text-xs font-medium rounded-full ${getStatusColor(order.status)}`}>
                                                        {getStatusText(order.status)}
                                                    </span>
                                                </div>

                                                <div className="space-y-2 mb-3">
                                                    {order.items.map((item, index) => (
                                                        <div key={index} className="flex justify-between text-sm">
                                                            <span>{item.productName} x{item.quantity}</span>
                                                            <span>{formatCurrencyPrice(item.price * item.quantity, selectedCurrency)}</span>
                                                        </div>
                                                    ))}
                                                </div>

                                                <div className="flex justify-between items-center pt-3 border-t border-gray-200">
                                                    <span className="font-medium">Tng cng:</span>
                                                    <span className="font-bold text-lg text-blue-600">
                                                        {formatCurrencyPrice(order.totalAmount, selectedCurrency)}
                                                    </span>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </CardContent>
                        </Card>
                    </TabsContent>

                    {/* Wishlist Tab */}
                    <TabsContent value="wishlist">
                        <Card>
                            <CardHeader>
                                <CardTitle>Danh sch yu thch</CardTitle>
                                <CardDescription>
                                    Cc sn phm bn  thm vo danh sch yu thch
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                <div className="text-center py-8">
                                    <Heart className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                                    <h3 className="text-lg font-medium text-gray-900 mb-2">
                                        Danh sch yu thch trng
                                    </h3>
                                    <p className="text-gray-500">
                                        Thm sn phm vo danh sch yu thch  theo di chng d dng hn
                                    </p>
                                </div>
                            </CardContent>
                        </Card>
                    </TabsContent>

                    {/* Settings Tab */}
                    <TabsContent value="settings">
                        {showChangePassword ? (
                            <div className="flex justify-center">
                                <ChangePasswordForm
                                    onSuccess={() => {
                                        setShowChangePassword(false);
                                        setSuccess('Mt khu  c thay i thnh cng');
                                        setTimeout(() => setSuccess(''), 3000);
                                    }}
                                    onCancel={() => setShowChangePassword(false)}
                                />
                            </div>
                        ) : (
                            <div className="flex justify-center">
                                <Card className="w-full max-w-md">
                                    <CardHeader>
                                        <CardTitle className="flex items-center">
                                            <Lock className="h-5 w-5 mr-2" />
                                            i mt khu
                                        </CardTitle>
                                        <CardDescription>
                                            Thay i mt khu ti khon ca bn
                                        </CardDescription>
                                    </CardHeader>
                                    <CardContent>
                                        <Button
                                            variant="outline"
                                            className="w-full justify-start"
                                            onClick={() => setShowChangePassword(true)}
                                        >
                                            <Lock className="h-4 w-4 mr-2" />
                                            i mt khu
                                        </Button>
                                    </CardContent>
                                </Card>
                            </div>
                        )}
                    </TabsContent>
                </Tabs>
            </div>
        </div>
    );
}