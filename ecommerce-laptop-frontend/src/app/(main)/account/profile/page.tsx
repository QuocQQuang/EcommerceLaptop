'use client';

import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';
import { getApiUrl } from '@/lib/utils';
import { authService } from '@/services/authService';
import { userService } from '@/services/userService';
import { zodResolver } from '@hookform/resolvers/zod';
import { Camera, Eye, EyeOff, Lock, User } from 'lucide-react';
import { useSession } from 'next-auth/react';
import Image from 'next/image';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

const profileSchema = z.object({
    firstName: z.string().min(1, 'H khng c  trng'),
    lastName: z.string().min(1, 'Tn khng c  trng'),
    phoneNumber: z.string().optional(),
});

const passwordSchema = z
    .object({
        currentPassword: z.string().min(1, 'Mt khu hin ti l bt buc'),
        newPassword: z
            .string()
            .min(8, 'Mt khu mi phi c t nht 8 k t'),
        confirmPassword: z.string(),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
        message: 'Mt khu xc nhn khng khp',
        path: ['confirmPassword'],
    });

type ProfileFormValues = z.infer<typeof profileSchema>;
type PasswordFormValues = z.infer<typeof passwordSchema>;

export default function ProfilePage() {
    const { data: session, update } = useSession();
    const [isLoading, setIsLoading] = useState(true);
    const [isSavingProfile, setIsSavingProfile] = useState(false);
    const [isSavingPassword, setIsSavingPassword] = useState(false);
    const [showCurrentPassword, setShowCurrentPassword] = useState(false);
    const [showNewPassword, setShowNewPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);
    const [avatarUrl, setAvatarUrl] = useState<string | null>(null);
    const [isUploading, setIsUploading] = useState(false);

    const profileForm = useForm<ProfileFormValues>({
        resolver: zodResolver(profileSchema),
        defaultValues: {
            firstName: '',
            lastName: '',
            phoneNumber: '',
        },
    });

    const passwordForm = useForm<PasswordFormValues>({
        resolver: zodResolver(passwordSchema),
        defaultValues: {
            currentPassword: '',
            newPassword: '',
            confirmPassword: '',
        },
    });

    useEffect(() => {
        const loadProfile = async () => {
            setIsLoading(true);
            if (session?.user?.id) {
                try {
                    const { data: profile } = await userService.getProfile();
                    profileForm.reset({
                        firstName: profile.firstName,
                        lastName: profile.lastName,
                        phoneNumber: profile.phoneNumber || '',
                    });
                    setAvatarUrl(profile.profilePictureUrl || null);
                } catch (error) {
                    console.error('Failed to load profile:', error);
                    toast.error('Khng th ti thng tin c nhn.');
                }
            }
            setIsLoading(false);
        };
        loadProfile();
    }, [session, profileForm]);

    const handleProfileUpdate = async (values: ProfileFormValues) => {
        setIsSavingProfile(true);
        try {
            await userService.updateProfile(values);
            await update({
                ...session,
                user: {
                    ...session?.user,
                    name: `${values.firstName} ${values.lastName}`,
                    firstName: values.firstName,
                    lastName: values.lastName,
                },
            });
            toast.success('Cp nht thng tin thnh cng!');
        } catch (error: any) {
            const errorMessage =
                error.response?.data?.message ||
                'C li xy ra, vui lng th li.';
            toast.error(errorMessage);
        } finally {
            setIsSavingProfile(false);
        }
    };

    const handlePasswordChange = async (values: PasswordFormValues) => {
        setIsSavingPassword(true);
        try {
            await authService.changePassword(
                values.currentPassword,
                values.newPassword,
            );
            toast.success('i mt khu thnh cng!');
            passwordForm.reset();
        } catch (error: any) {
            const errorMessage =
                error.response?.data?.message ||
                'C li xy ra, vui lng th li.';
            toast.error(errorMessage);
        } finally {
            setIsSavingPassword(false);
        }
    };

    const handleAvatarUpload = async (
        event: React.ChangeEvent<HTMLInputElement>,
    ) => {
        const file = event.target.files?.[0];
        if (!file) return;

        if (file.size > 10 * 1024 * 1024) {
            toast.error('Kch thc file khng c vt qu 10MB.');
            return;
        }

        const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
        if (!allowedTypes.includes(file.type)) {
            toast.error('nh dng file khng hp l. Ch chp nhn JPG, PNG, WebP.');
            return;
        }

        setIsUploading(true);
        const toastId = toast.loading('ang ti nh ln...');

        try {
            const response = await userService.uploadAvatar(file);
            const newAvatarUrl = response.data.avatarUrl;

            setAvatarUrl(newAvatarUrl);
            await update({
                ...session,
                user: {
                    ...session?.user,
                    profilePictureUrl: newAvatarUrl,
                },
            });

            toast.success('Cp nht nh i din thnh cng!', { id: toastId });
        } catch (error: any) {
            console.error('Avatar upload error:', error);
            const errorMessage =
                error.response?.data?.message ||
                'C li xy ra khi ti nh ln.';
            toast.error(errorMessage, { id: toastId });
        } finally {
            setIsUploading(false);
            // Reset file input
            event.target.value = '';
        }
    };

    if (isLoading) {
        return (
            <div className="space-y-6">
                <Card>
                    <CardHeader>
                        <div className="h-8 bg-gray-200 rounded w-48 animate-pulse"></div>
                        <div className="h-4 bg-gray-200 rounded w-64 animate-pulse mt-2"></div>
                    </CardHeader>
                    <CardContent className="space-y-8">
                        <div className="flex items-center space-x-6">
                            <div className="w-24 h-24 bg-gray-200 rounded-full animate-pulse"></div>
                            <div className="space-y-2">
                                <div className="h-6 bg-gray-200 rounded w-32 animate-pulse"></div>
                                <div className="h-4 bg-gray-200 rounded w-48 animate-pulse"></div>
                            </div>
                        </div>
                        <div className="h-px bg-gray-200 w-full animate-pulse"></div>
                        <div className="space-y-4">
                            {[...Array(3)].map((_, i) => (
                                <div
                                    key={i}
                                    className="h-10 bg-gray-200 rounded animate-pulse"
                                ></div>
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
                    <CardTitle className="flex items-center">
                        <User className="mr-2" />
                        H s ca ti
                    </CardTitle>
                    <CardDescription>
                        Qun l thng tin c nhn v bo mt ti khon ca bn.
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {/* Avatar Section */}
                    <div className="flex flex-col sm:flex-row items-center space-y-4 sm:space-y-0 sm:space-x-6 mb-8">
                        <div className="relative">
                            <Image
                                src={getApiUrl(
                                    avatarUrl || '/uploads/default_avatar.png',
                                )}
                                alt="Avatar"
                                width={96}
                                height={96}
                                className="w-24 h-24 rounded-full object-cover border-2 border-gray-200"
                            />
                            <label
                                htmlFor="avatar-upload"
                                className="absolute bottom-0 right-0 bg-primary text-primary-foreground rounded-full p-2 cursor-pointer hover:bg-primary/90 transition-colors"
                            >
                                <Camera className="h-4 w-4" />
                                <input
                                    id="avatar-upload"
                                    type="file"
                                    accept="image/jpeg,image/png,image/webp"
                                    className="hidden"
                                    onChange={handleAvatarUpload}
                                    disabled={isUploading}
                                />
                            </label>
                        </div>
                        <div>
                            <h3 className="text-xl font-semibold">
                                {profileForm.getValues('firstName')}{' '}
                                {profileForm.getValues('lastName')}
                            </h3>
                            <p className="text-sm text-muted-foreground">
                                {session?.user?.email}
                            </p>
                        </div>
                    </div>

                    <Separator />

                    {/* Profile Form */}
                    <Form {...profileForm}>
                        <form
                            onSubmit={profileForm.handleSubmit(
                                handleProfileUpdate,
                            )}
                            className="space-y-6 mt-6"
                        >
                            <h3 className="text-lg font-medium">
                                Thng tin c nhn
                            </h3>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <FormField
                                    control={profileForm.control}
                                    name="firstName"
                                    render={({ field }: { field: any }) => (
                                        <FormItem>
                                            <FormLabel>H</FormLabel>
                                            <FormControl>
                                                <Input
                                                    placeholder="Nguyn"
                                                    {...field}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                                <FormField
                                    control={profileForm.control}
                                    name="lastName"
                                    render={({ field }: { field: any }) => (
                                        <FormItem>
                                            <FormLabel>Tn</FormLabel>
                                            <FormControl>
                                                <Input
                                                    placeholder="Vn A"
                                                    {...field}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            </div>
                            <FormField
                                control={profileForm.control}
                                name="phoneNumber"
                                render={({ field }: { field: any }) => (
                                    <FormItem>
                                        <FormLabel>S in thoi</FormLabel>
                                        <FormControl>
                                            <Input
                                                placeholder="09xxxxxxxx"
                                                {...field}
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <Button
                                type="submit"
                                disabled={isSavingProfile || !profileForm.formState.isDirty}
                            >
                                {isSavingProfile
                                    ? 'ang lu...'
                                    : 'Lu thay i'}
                            </Button>
                        </form>
                    </Form>

                    <Separator className="my-8" />

                    {/* Password Form */}
                    <Form {...passwordForm}>
                        <form
                            onSubmit={passwordForm.handleSubmit(
                                handlePasswordChange,
                            )}
                            className="space-y-6"
                        >
                            <h3 className="text-lg font-medium flex items-center">
                                <Lock className="mr-2 h-5 w-5" />
                                i mt khu
                            </h3>
                            <FormField
                                control={passwordForm.control}
                                name="currentPassword"
                                render={({ field }: { field: any }) => (
                                    <FormItem>
                                        <FormLabel>Mt khu hin ti</FormLabel>
                                        <FormControl>
                                            <div className="relative">
                                                <Input
                                                    type={
                                                        showCurrentPassword
                                                            ? 'text'
                                                            : 'password'
                                                    }
                                                    {...field}
                                                />
                                                <Button
                                                    type="button"
                                                    variant="ghost"
                                                    size="sm"
                                                    className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                                                    onClick={() =>
                                                        setShowCurrentPassword(
                                                            (prev) => !prev,
                                                        )
                                                    }
                                                >
                                                    {showCurrentPassword ? (
                                                        <EyeOff className="h-4 w-4" />
                                                    ) : (
                                                        <Eye className="h-4 w-4" />
                                                    )}
                                                </Button>
                                            </div>
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <FormField
                                    control={passwordForm.control}
                                    name="newPassword"
                                    render={({ field }: { field: any }) => (
                                        <FormItem>
                                            <FormLabel>Mt khu mi</FormLabel>
                                            <FormControl>
                                                <div className="relative">
                                                    <Input
                                                        type={
                                                            showNewPassword
                                                                ? 'text'
                                                                : 'password'
                                                        }
                                                        {...field}
                                                    />
                                                    <Button
                                                        type="button"
                                                        variant="ghost"
                                                        size="sm"
                                                        className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                                                        onClick={() =>
                                                            setShowNewPassword(
                                                                (prev) => !prev,
                                                            )
                                                        }
                                                    >
                                                        {showNewPassword ? (
                                                            <EyeOff className="h-4 w-4" />
                                                        ) : (
                                                            <Eye className="h-4 w-4" />
                                                        )}
                                                    </Button>
                                                </div>
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                                <FormField
                                    control={passwordForm.control}
                                    name="confirmPassword"
                                    render={({ field }: { field: any }) => (
                                        <FormItem>
                                            <FormLabel>
                                                Xc nhn mt khu mi
                                            </FormLabel>
                                            <FormControl>
                                                <div className="relative">
                                                    <Input
                                                        type={
                                                            showConfirmPassword
                                                                ? 'text'
                                                                : 'password'
                                                        }
                                                        {...field}
                                                    />
                                                    <Button
                                                        type="button"
                                                        variant="ghost"
                                                        size="sm"
                                                        className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                                                        onClick={() =>
                                                            setShowConfirmPassword(
                                                                (prev) => !prev,
                                                            )
                                                        }
                                                    >
                                                        {showConfirmPassword ? (
                                                            <EyeOff className="h-4 w-4" />
                                                        ) : (
                                                            <Eye className="h-4 w-4" />
                                                        )}
                                                    </Button>
                                                </div>
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            </div>
                            <Button
                                type="submit"
                                disabled={isSavingPassword || !passwordForm.formState.isDirty}
                            >
                                {isSavingPassword
                                    ? 'ang cp nht...'
                                    : 'Cp nht mt khu'}
                            </Button>
                        </form>
                    </Form>
                </CardContent>
            </Card>
        </div>
    );
}