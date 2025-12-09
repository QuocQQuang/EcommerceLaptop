// Product specifications options for dropdowns/comboboxes
export const PRODUCT_SPECS_OPTIONS = {
    // CPU Options
    cpuBrands: [
        'Intel',
        'AMD',
        'Apple',
        'Qualcomm',
        'MediaTek',
        'Khc'
    ],

    cpuModels: {
        'Intel': [
            'Core i3-1215U',
            'Core i5-1235U',
            'Core i5-1240P',
            'Core i5-12500H',
            'Core i7-1260P',
            'Core i7-12700H',
            'Core i9-12900H',
            'Core i9-13900H',
            'Pentium Gold',
            'Celeron',
            'Khc'
        ],
        'AMD': [
            'Ryzen 3 7320U',
            'Ryzen 5 5625U',
            'Ryzen 5 6600H',
            'Ryzen 7 5825U',
            'Ryzen 7 6800H',
            'Ryzen 7 7840HS',
            'Ryzen 9 6900HX',
            'Ryzen 9 7940HS',
            'Athlon',
            'Khc'
        ],
        'Apple': [
            'M1',
            'M1 Pro',
            'M1 Max',
            'M1 Ultra',
            'M2',
            'M2 Pro',
            'M2 Max',
            'M2 Ultra',
            'M3',
            'M3 Pro',
            'M3 Max',
            'Khc'
        ],
        'Qualcomm': [
            'Snapdragon 8cx Gen 3',
            'Snapdragon 7c+',
            'Khc'
        ],
        'MediaTek': [
            'Kompanio 1300T',
            'Kompanio 1200',
            'Khc'
        ]
    },

    cpuGenerations: [
        '12th Gen Intel',
        '13th Gen Intel',
        '14th Gen Intel',
        'Zen 3 (5000 series)',
        'Zen 4 (7000 series)',
        'Apple Silicon M1',
        'Apple Silicon M2',
        'Apple Silicon M3',
        'Khc'
    ],

    cpuCores: [
        '2 cores',
        '4 cores',
        '6 cores',
        '8 cores',
        '10 cores',
        '12 cores',
        '14 cores',
        '16 cores',
        '18 cores',
        '20 cores',
        '24 cores',
        'Khc'
    ],

    // RAM Options
    ramTypes: [
        'DDR4',
        'DDR5',
        'LPDDR4',
        'LPDDR4X',
        'LPDDR5',
        'LPDDR5X',
        'Khc'
    ],

    ramCapacities: [
        '4GB',
        '8GB',
        '16GB',
        '32GB',
        '64GB',
        '128GB',
        'Khc'
    ],

    ramSpeeds: [
        '2133 MHz',
        '2400 MHz',
        '2666 MHz',
        '2933 MHz',
        '3200 MHz',
        '3600 MHz',
        '4000 MHz',
        '4800 MHz',
        '5200 MHz',
        '5600 MHz',
        '6000 MHz',
        '6400 MHz',
        'Khc'
    ],

    // Storage Options
    storageTypes: [
        'SSD',
        'HDD',
        'Hybrid (SSD + HDD)',
        'eMMC',
        'UFS',
        'Khc'
    ],

    storageCapacities: [
        '128GB',
        '256GB',
        '512GB',
        '1TB',
        '2TB',
        '4TB',
        '8TB',
        'Khc'
    ],

    storageInterfaces: [
        'SATA III',
        'NVMe PCIe Gen 3',
        'NVMe PCIe Gen 4',
        'NVMe PCIe Gen 5',
        'M.2 SATA',
        'M.2 NVMe',
        'Khc'
    ],

    // GPU Options
    gpuTypes: [
        'Integrated',
        'Dedicated',
        'Hybrid',
        'Khc'
    ],

    gpuBrands: [
        'Intel',
        'AMD',
        'NVIDIA',
        'Apple',
        'Khc'
    ],

    gpuModels: {
        'Intel': [
            'Intel UHD Graphics',
            'Intel Iris Xe Graphics',
            'Intel Arc A350M',
            'Intel Arc A370M',
            'Intel Arc A550M',
            'Intel Arc A730M',
            'Intel Arc A770M',
            'Khc'
        ],
        'AMD': [
            'Radeon RX 6500M',
            'Radeon RX 6600M',
            'Radeon RX 6700M',
            'Radeon RX 6800M',
            'Radeon RX 7600M',
            'Radeon RX 7600M XT',
            'Radeon RX 7700S',
            'Radeon 780M',
            'Khc'
        ],
        'NVIDIA': [
            'GeForce RTX 3050',
            'GeForce RTX 3050 Ti',
            'GeForce RTX 3060',
            'GeForce RTX 3070',
            'GeForce RTX 3070 Ti',
            'GeForce RTX 3080',
            'GeForce RTX 3080 Ti',
            'GeForce RTX 4050',
            'GeForce RTX 4060',
            'GeForce RTX 4070',
            'GeForce RTX 4080',
            'GeForce RTX 4090',
            'Khc'
        ],
        'Apple': [
            'Apple M1 GPU (7-core)',
            'Apple M1 GPU (8-core)',
            'Apple M1 Pro GPU (14-core)',
            'Apple M1 Pro GPU (16-core)',
            'Apple M1 Max GPU (24-core)',
            'Apple M1 Max GPU (32-core)',
            'Apple M2 GPU (8-core)',
            'Apple M2 GPU (10-core)',
            'Apple M2 Pro GPU (16-core)',
            'Apple M2 Pro GPU (19-core)',
            'Apple M2 Max GPU (30-core)',
            'Apple M2 Max GPU (38-core)',
            'Khc'
        ]
    },

    gpuVramSizes: [
        '2GB',
        '4GB',
        '6GB',
        '8GB',
        '12GB',
        '16GB',
        '24GB',
        'Khc'
    ],

    gpuVramTypes: [
        'GDDR6',
        'GDDR6X',
        'GDDR7',
        'HBM2',
        'HBM2e',
        'HBM3',
        'LPDDR5',
        'Khc'
    ],

    // Display Options
    displaySizes: [
        '11.6"',
        '12.5"',
        '13.3"',
        '13.4"',
        '14"',
        '15.6"',
        '16"',
        '17.3"',
        '18"',
        'Khc'
    ],

    displayResolutions: [
        '1366x768 (HD)',
        '1920x1080 (FHD)',
        '2560x1440 (QHD)',
        '2560x1600 (WQXGA)',
        '2880x1800 (Retina)',
        '3200x2000 (3.2K)',
        '3840x2160 (4K UHD)',
        '5120x2880 (5K)',
        '6016x3384 (6K)',
        'Khc'
    ],

    displayPanels: [
        'IPS',
        'OLED',
        'Mini-LED',
        'QLED',
        'TN',
        'VA',
        'PLS',
        'Khc'
    ],

    refreshRates: [
        '60Hz',
        '90Hz',
        '120Hz',
        '144Hz',
        '165Hz',
        '240Hz',
        '300Hz',
        '360Hz',
        'Khc'
    ],

    // Battery Options
    batteryCapacities: [
        '30Wh',
        '40Wh',
        '50Wh',
        '60Wh',
        '70Wh',
        '80Wh',
        '90Wh',
        '100Wh',
        'Khc'
    ],

    // Weight Options
    weightRanges: [
        'Under 1kg',
        '1.0-1.5kg',
        '1.5-2.0kg',
        '2.0-2.5kg',
        '2.5-3.0kg',
        '3.0-3.5kg',
        'Over 3.5kg',
        'Khc'
    ],

    // Connectivity Options
    wifiStandards: [
        'WiFi 5 (802.11ac)',
        'WiFi 6 (802.11ax)',
        'WiFi 6E (802.11ax)',
        'WiFi 7 (802.11be)',
        'Khc'
    ],

    bluetoothVersions: [
        'Bluetooth 4.2',
        'Bluetooth 5.0',
        'Bluetooth 5.1',
        'Bluetooth 5.2',
        'Bluetooth 5.3',
        'Khc'
    ],

    // Port Options
    portTypes: [
        'USB-A 2.0',
        'USB-A 3.0',
        'USB-A 3.1',
        'USB-A 3.2',
        'USB-C 3.1',
        'USB-C 3.2',
        'USB-C 4.0',
        'Thunderbolt 3',
        'Thunderbolt 4',
        'HDMI 1.4',
        'HDMI 2.0',
        'HDMI 2.1',
        'DisplayPort 1.2',
        'DisplayPort 1.4',
        'DisplayPort 2.0',
        'Audio Jack 3.5mm',
        'Ethernet RJ45',
        'SD Card Reader',
        'MicroSD Card Reader',
        'Khc'
    ],

    // Color Options
    colors: [
        'Silver',
        'Space Gray',
        'Black',
        'White',
        'Blue',
        'Red',
        'Green',
        'Pink',
        'Gold',
        'Rose Gold',
        'Khc'
    ],

    // OS Options
    operatingSystems: [
        'Windows 11 Home',
        'Windows 11 Pro',
        'Windows 10 Home',
        'Windows 10 Pro',
        'macOS Monterey',
        'macOS Ventura',
        'macOS Sonoma',
        'macOS Sequoia',
        'Ubuntu',
        'Linux Mint',
        'Chrome OS',
        'No OS',
        'Khc'
    ],

    // Keyboard Options
    keyboardTypes: [
        'Standard',
        'Backlit',
        'RGB Backlit',
        'Mechanical',
        'Membrane',
        'Scissor Switch',
        'Butterfly Switch',
        'Khc'
    ],

    // Webcam Options
    webcamTypes: [
        'HD (720p)',
        'FHD (1080p)',
        '4K (2160p)',
        'HD + IR Camera',
        'FHD + IR Camera',
        'No Webcam',
        'Khc'
    ],

    // Security Options
    securityFeatures: [
        'Fingerprint Reader',
        'Face Recognition',
        'Windows Hello',
        'TPM 2.0',
        'Kensington Lock Slot',
        'Smart Card Reader',
        'Khc'
    ],

    // Warranty Options
    warrantyPeriods: [
        '1 year',
        '2 years',
        '3 years',
        '4 years',
        '5 years',
        'On-site warranty',
        'International warranty',
        'Extended warranty available',
        'Khc'
    ]
};

// Helper function to get CPU models based on selected brand
export const getCpuModels = (brand: string): string[] => {
    return PRODUCT_SPECS_OPTIONS.cpuModels[brand as keyof typeof PRODUCT_SPECS_OPTIONS.cpuModels] || [];
};

// Helper function to get GPU models based on selected brand
export const getGpuModels = (brand: string): string[] => {
    return PRODUCT_SPECS_OPTIONS.gpuModels[brand as keyof typeof PRODUCT_SPECS_OPTIONS.gpuModels] || [];
};

// Helper function to check if "Khc" (Other) is selected
export const isOtherSelected = (value: string): boolean => {
    return value === 'Khc';
};
