// Test script  kim tra address formatting functions
import { formatAddressForDisplay, formatAddressParts, parseAddressString } from './src/utils/addressUtils.js';

console.log('=== Testing Address Formatting Functions ===\n');

// Test case 1: Empty or null address
console.log('1. Testing empty/null addresses:');
console.log('formatAddressForDisplay(null):', formatAddressForDisplay(null));
console.log('formatAddressForDisplay(""):', formatAddressForDisplay(''));
console.log('formatAddressForDisplay(undefined):', formatAddressForDisplay(undefined));
console.log();

// Test case 2: Address string with empty fields (causing ", , /" issue)
console.log('2. Testing problematic address strings:');
const problematicAddress1 = ', , /';
const problematicAddress2 = ' ,, , ';
const problematicAddress3 = 'Nguyen Van A, 0123456789, , , ';
console.log(`"${problematicAddress1}" ->`, formatAddressForDisplay(problematicAddress1));
console.log(`"${problematicAddress2}" ->`, formatAddressForDisplay(problematicAddress2));
console.log(`"${problematicAddress3}" ->`, formatAddressForDisplay(problematicAddress3));
console.log();

// Test case 3: Normal address strings
console.log('3. Testing normal address strings:');
const normalAddress1 = 'Nguyen Van A, 0123456789, 123 Nguyen Hue, Phuong Ben Nghe, Quan 1, TP HCM';
const normalAddress2 = '123 Le Loi, Phuong 1, Quan 3, TP HCM';
console.log(`"${normalAddress1}" ->`, formatAddressForDisplay(normalAddress1));
console.log(`"${normalAddress2}" ->`, formatAddressForDisplay(normalAddress2));
console.log();

// Test case 4: Address object
console.log('4. Testing Address object:');
const addressObj = {
    recipientName: 'Nguyen Van A',
    phoneNumber: '0123456789',
    addressLine1: '123 Nguyen Hue',
    city: 'TP HCM',
    district: 'Quan 1',
    ward: 'Phuong Ben Nghe'
};
console.log('Address object ->', formatAddressForDisplay(addressObj));
console.log();

// Test case 5: formatAddressParts
console.log('5. Testing formatAddressParts:');
const parts1 = formatAddressParts(normalAddress1);
console.log('Normal address parts:', parts1);

const parts2 = formatAddressParts(problematicAddress1);
console.log('Problematic address parts:', parts2);

const parts3 = formatAddressParts(addressObj);
console.log('Address object parts:', parts3);
console.log();

// Test case 6: parseAddressString
console.log('6. Testing parseAddressString:');
const parsed1 = parseAddressString(normalAddress1);
console.log('Parsed normal address:', parsed1);

const parsed2 = parseAddressString(problematicAddress1);
console.log('Parsed problematic address:', parsed2);
console.log();

console.log('=== Test Complete ===');