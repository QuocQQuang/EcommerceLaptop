import type { Address } from '@/types/address';

/**
 * Safely format address string for display
 * Handles cases where address might be a string or Address object
 */
export const formatAddressForDisplay = (address: string | Address | undefined | null): string => {
    if (!address) {
        return 'Cha c a ch';
    }

    // If it's already a string, return it as is (but cleaned)
    if (typeof address === 'string') {
        const cleaned = address.replace(/,\s*,/g, ',').replace(/^,|,$/, '').trim();
        return cleaned || 'Cha c a ch';
    }

    // If it's an Address object, format it properly
    const parts = [
        address.addressLine1,
        address.ward,
        address.district, 
        address.city
    ].filter(part => part && part.trim() !== '');

    return parts.length > 0 ? parts.join(', ') : 'Cha c a ch';
};

/**
 * Parse address string into components for display
 * Attempts to extract meaningful parts from a comma-separated address string
 */
export const parseAddressString = (addressString: string | undefined | null): Partial<Address> => {
    if (!addressString || typeof addressString !== 'string') {
        return {
            recipientName: '',
            phoneNumber: '',
            addressLine1: '',
            addressLine2: '',
            city: '',
            district: '',
            ward: '',
        };
    }

    // Clean up the string - remove extra commas and whitespace
    const cleaned = addressString.replace(/,\s*,/g, ',').replace(/^,|,$/, '').trim();
    
    if (!cleaned) {
        return {
            recipientName: '',
            phoneNumber: '',
            addressLine1: '',
            addressLine2: '',
            city: '',
            district: '',
            ward: '',
        };
    }

    // Split by comma and filter out empty parts
    const parts = cleaned.split(',').map(part => part.trim()).filter(part => part !== '');
    
    // If we have parts, try to assign them based on common Vietnamese address format
    // Usually: [Name], [Phone], [Street Address], [Ward], [District], [City/Province]
    const result: Partial<Address> = {
        recipientName: '',
        phoneNumber: '',
        addressLine1: '',
        addressLine2: '',
        city: '',
        district: '',
        ward: '',
    };

    if (parts.length >= 1) {
        // First part could be name or address, check if it looks like a phone number
        if (/^\+?[\d\s-()]+$/.test(parts[0])) {
            result.phoneNumber = parts[0];
        } else {
            result.addressLine1 = parts[0];
        }
    }

    if (parts.length >= 2) {
        // Second part 
        if (!result.phoneNumber && /^\+?[\d\s-()]+$/.test(parts[1])) {
            result.phoneNumber = parts[1];
        } else if (!result.addressLine1) {
            result.addressLine1 = parts[1];
        } else {
            result.ward = parts[1];
        }
    }

    if (parts.length >= 3) {
        // Third part is usually ward if we already have address
        if (result.addressLine1 && !result.ward) {
            result.ward = parts[2];
        } else {
            result.district = parts[2];
        }
    }

    if (parts.length >= 4) {
        // Fourth part is usually district
        if (!result.district) {
            result.district = parts[3];
        } else {
            result.city = parts[3];
        }
    }

    if (parts.length >= 5) {
        // Fifth part is usually city/province
        result.city = parts[4];
    }

    // If we only have a few parts, assume the last one is city
    if (parts.length >= 2 && !result.city) {
        result.city = parts[parts.length - 1];
        if (parts.length >= 3 && !result.district) {
            result.district = parts[parts.length - 2];
        }
    }

    return result;
};

/**
 * Format address parts for different display scenarios
 */
export const formatAddressParts = (address: string | Address | undefined | null): {
    full: string;
    short: string;
    line1: string;
    location: string; // ward, district, city
} => {
    const defaultResult = {
        full: 'Cha c a ch',
        short: 'Cha c a ch', 
        line1: '',
        location: ''
    };

    if (!address) {
        return defaultResult;
    }

    let parsedAddress: Partial<Address>;

    if (typeof address === 'string') {
        parsedAddress = parseAddressString(address);
    } else {
        parsedAddress = address;
    }

    const line1 = parsedAddress.addressLine1 || '';
    const location = [parsedAddress.ward, parsedAddress.district, parsedAddress.city]
        .filter(part => part && part.trim() !== '')
        .join(', ');

    const full = [line1, location].filter(part => part).join(', ') || 'Cha c a ch';
    const short = location || line1 || 'Cha c a ch';

    return {
        full,
        short,
        line1,
        location
    };
};