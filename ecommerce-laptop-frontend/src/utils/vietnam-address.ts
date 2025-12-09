import type { District, Province, Ward } from '@/types/address';

// Type definition for vietnamAddressService
export interface VietnamAddressService {
    getProvinces(): Province[];
    getDistrictsByProvince(provinceCode: string): District[];
    getWardsByDistrict(districtCode: string): Ward[];
    getProvinceByCode(code: string): Province | undefined;
    getDistrictByCode(code: string): District | undefined;
    getWardByCode(code: string): Ward | undefined;
    searchProvinces(query: string): Province[];
    searchDistricts(provinceCode: string, query: string): District[];
    searchWards(districtCode: string, query: string): Ward[];
    getProvinceByName(name: string): Province | undefined;
    getDistrictByName(name: string, provinceCode: string): District | undefined;
    getWardByName(name: string, districtCode: string): Ward | undefined;
}

// Import static data
import districtsData from '@/data/quan_huyen.json';
import provincesData from '@/data/tinh_tp.json';
import wardsData from '@/data/xa_phuong.json';

export const vietnamAddressService: VietnamAddressService = {
    /**
     * Get all provinces
     */
    getProvinces(): Province[] {
        return Object.values(provincesData as Record<string, Province>);
    },

    /**
     * Get districts by province code
     */
    getDistrictsByProvince(provinceCode: string): District[] {
        return Object.values(districtsData as Record<string, District>)
            .filter(district => district.parent_code === provinceCode);
    },

    /**
     * Get wards by district code
     */
    getWardsByDistrict(districtCode: string): Ward[] {
        return Object.values(wardsData as Record<string, Ward>)
            .filter(ward => ward.parent_code === districtCode);
    },

    /**
     * Find province by code
     */
    getProvinceByCode(code: string): Province | undefined {
        return (provincesData as Record<string, Province>)[code];
    },

    /**
     * Find district by code
     */
    getDistrictByCode(code: string): District | undefined {
        return (districtsData as Record<string, District>)[code];
    },

    /**
     * Find ward by code
     */
    getWardByCode(code: string): Ward | undefined {
        return (wardsData as Record<string, Ward>)[code];
    },

    /**
     * Search provinces by name
     */
    searchProvinces(query: string): Province[] {
        const normalizedQuery = query.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        return this.getProvinces().filter(province => {
            const normalizedName = province.name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
            return normalizedName.includes(normalizedQuery);
        });
    },

    /**
     * Search districts by name within a province
     */
    searchDistricts(provinceCode: string, query: string): District[] {
        const normalizedQuery = query.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        return this.getDistrictsByProvince(provinceCode).filter(district => {
            const normalizedName = district.name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
            return normalizedName.includes(normalizedQuery);
        });
    },

    /**
     * Search wards by name within a district
     */
    searchWards(districtCode: string, query: string): Ward[] {
        const normalizedQuery = query.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        return this.getWardsByDistrict(districtCode).filter(ward => {
            const normalizedName = ward.name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
            return normalizedName.includes(normalizedQuery);
        });
    },

    /**
     * Find province by name (exact match)
     */
    getProvinceByName(name: string): Province | undefined {
        const normalizedName = name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        return this.getProvinces().find(province => {
            const normalizedProvinceName = province.name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
            return normalizedProvinceName === normalizedName;
        });
    },

    /**
     * Find district by name within a province
     */
    getDistrictByName(name: string, provinceCode: string): District | undefined {
        const normalizedName = name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        return this.getDistrictsByProvince(provinceCode).find(district => {
            const normalizedDistrictName = district.name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
            return normalizedDistrictName === normalizedName;
        });
    },

    /**
     * Find ward by name within a district
     */
    getWardByName(name: string, districtCode: string): Ward | undefined {
        const normalizedName = name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        return this.getWardsByDistrict(districtCode).find(ward => {
            const normalizedWardName = ward.name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
            return normalizedWardName === normalizedName;
        });
    }
};