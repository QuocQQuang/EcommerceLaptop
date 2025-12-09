export interface Province {
    name: string;
    slug: string;
    type: string;
    name_with_type: string;
    code: string;
}

export interface District {
    name: string;
    type: string;
    slug: string;
    name_with_type: string;
    path: string;
    path_with_type: string;
    code: string;
    parent_code: string;
}

export interface Ward {
    name: string;
    type: string;
    slug: string;
    name_with_type: string;
    path: string;
    path_with_type: string;
    code: string;
    parent_code: string;
}

export interface VietnamAddress {
    province?: Province;
    district?: District;
    ward?: Ward;
}

export interface Address {
    id?: number;
    recipientName: string;
    phoneNumber: string;
    addressLine1: string;
    addressLine2?: string;
    city: string;
    district: string;
    ward: string;
    postalCode?: string;
    isDefault?: boolean;
    createdAt?: string;
    updatedAt?: string;
    verified?: boolean; // Address verification status
    fullName?: string; // Alternative field name for compatibility
}

// Address Validation Types
export interface AddressValidationError {
    field: keyof Address;
    message: string;
}

export interface AddressValidationResult {
    isValid: boolean;
    errors: AddressValidationError[];
}

// Address creation/update DTOs
export interface CreateAddressDto {
    recipientName: string;
    phoneNumber: string;
    addressLine1: string;
    addressLine2?: string;
    city: string;
    district: string;
    ward: string;
    postalCode?: string;
    isDefault?: boolean;
}

export interface UpdateAddressDto extends Partial<CreateAddressDto> {
    id: number;
}