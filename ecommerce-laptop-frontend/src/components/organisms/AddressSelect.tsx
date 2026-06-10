'use client';

import { Button } from '@/components/ui/button';
import {
    Command,
    CommandEmpty,
    CommandGroup,
    CommandInput,
    CommandItem,
    CommandList,
} from '@/components/ui/command';
import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from '@/components/ui/popover';
import { cn } from '@/lib/utils';
import type { District, Province, Ward } from '@/types/address';
import { vietnamAddressService } from '@/utils/vietnam-address';
import { Check, ChevronsUpDown } from 'lucide-react';
import { useEffect, useState } from 'react';

interface ProvinceSelectProps {
    value?: string;
    onValueChange: (value: string) => void;
    placeholder?: string;
    disabled?: boolean;
}

export function ProvinceSelect({ value, onValueChange, placeholder = "Chọn tỉnh/thành phố", disabled }: ProvinceSelectProps) {
    const [open, setOpen] = useState(false);
    const [provinces, setProvinces] = useState<Province[]>([]);
    const [filteredProvinces, setFilteredProvinces] = useState<Province[]>([]);
    const [search, setSearch] = useState('');

    useEffect(() => {
        const allProvinces = vietnamAddressService.getProvinces();
        setProvinces(allProvinces);
        setFilteredProvinces(allProvinces);
    }, []);

    useEffect(() => {
        if (search) {
            const filtered = provinces.filter(province =>
                province.name.toLowerCase().includes(search.toLowerCase()) ||
                province.name_with_type.toLowerCase().includes(search.toLowerCase())
            );
            setFilteredProvinces(filtered);
        } else {
            setFilteredProvinces(provinces);
        }
    }, [search, provinces]);

    const selectedProvince = provinces.find(province => province.code === value);

    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                <Button
                    type="button"
                    variant="outline"
                    role="combobox"
                    aria-expanded={open}
                    className="w-full justify-between"
                    disabled={disabled}
                >
                    {selectedProvince ? selectedProvince.name_with_type : placeholder}
                    <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                </Button>
            </PopoverTrigger>
            <PopoverContent className="w-full p-0">
                <Command>
                    <CommandInput
                        placeholder="Tìm kiếm tỉnh/thành phố..."
                        value={search}
                        onValueChange={setSearch}
                    />
                    <CommandList>
                        {filteredProvinces.length === 0 ? (
                            <CommandEmpty>Không tìm thấy tỉnh/thành phố nào.</CommandEmpty>
                        ) : (
                            <CommandGroup>
                                {filteredProvinces.map((province) => (
                                    <CommandItem
                                        key={province.code}
                                        value={province.name}
                                        onSelect={() => {
                                            onValueChange(province.code);
                                            setOpen(false);
                                            setSearch('');
                                        }}
                                    >
                                        <Check
                                            className={cn(
                                                "mr-2 h-4 w-4",
                                                value === province.code ? "opacity-100" : "opacity-0"
                                            )}
                                        />
                                        {province.name_with_type}
                                    </CommandItem>
                                ))}
                            </CommandGroup>
                        )}
                    </CommandList>
                </Command>
            </PopoverContent>
        </Popover>
    );
}

interface DistrictSelectProps {
    provinceCode?: string;
    value?: string;
    onValueChange: (value: string) => void;
    placeholder?: string;
    disabled?: boolean;
}

export function DistrictSelect({ provinceCode, value, onValueChange, placeholder = "Chọn quận/huyện", disabled }: DistrictSelectProps) {
    const [open, setOpen] = useState(false);
    const [districts, setDistricts] = useState<District[]>([]);
    const [filteredDistricts, setFilteredDistricts] = useState<District[]>([]);
    const [search, setSearch] = useState('');

    useEffect(() => {
        if (provinceCode) {
            const allDistricts = vietnamAddressService.getDistrictsByProvince(provinceCode);
            setDistricts(allDistricts);
            setFilteredDistricts(allDistricts);
        } else {
            setDistricts([]);
            setFilteredDistricts([]);
        }
        setSearch('');
    }, [provinceCode]);

    useEffect(() => {
        if (search) {
            const filtered = districts.filter(district =>
                district.name.toLowerCase().includes(search.toLowerCase()) ||
                district.name_with_type.toLowerCase().includes(search.toLowerCase())
            );
            setFilteredDistricts(filtered);
        } else {
            setFilteredDistricts(districts);
        }
    }, [search, districts]);

    const selectedDistrict = districts.find(district => district.code === value);

    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                <Button
                    type="button"
                    variant="outline"
                    role="combobox"
                    aria-expanded={open}
                    className="w-full justify-between"
                    disabled={disabled || !provinceCode}
                >
                    {selectedDistrict ? selectedDistrict.name_with_type : placeholder}
                    <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                </Button>
            </PopoverTrigger>
            <PopoverContent className="w-full p-0">
                <Command>
                    <CommandInput
                        placeholder="Tìm kiếm quận/huyện..."
                        value={search}
                        onValueChange={setSearch}
                    />
                    <CommandList>
                        {filteredDistricts.length === 0 ? (
                            <CommandEmpty>Không tìm thấy quận/huyện nào.</CommandEmpty>
                        ) : (
                            <CommandGroup>
                                {filteredDistricts.map((district) => (
                                    <CommandItem
                                        key={district.code}
                                        value={district.name}
                                        onSelect={() => {
                                            onValueChange(district.code);
                                            setOpen(false);
                                            setSearch('');
                                        }}
                                    >
                                        <Check
                                            className={cn(
                                                "mr-2 h-4 w-4",
                                                value === district.code ? "opacity-100" : "opacity-0"
                                            )}
                                        />
                                        {district.name_with_type}
                                    </CommandItem>
                                ))}
                            </CommandGroup>
                        )}
                    </CommandList>
                </Command>
            </PopoverContent>
        </Popover>
    );
}

interface WardSelectProps {
    districtCode?: string;
    value?: string;
    onValueChange: (value: string) => void;
    placeholder?: string;
    disabled?: boolean;
}

export function WardSelect({ districtCode, value, onValueChange, placeholder = "Chọn phường/xã", disabled }: WardSelectProps) {
    const [open, setOpen] = useState(false);
    const [wards, setWards] = useState<Ward[]>([]);
    const [filteredWards, setFilteredWards] = useState<Ward[]>([]);
    const [search, setSearch] = useState('');

    useEffect(() => {
        if (districtCode) {
            const allWards = vietnamAddressService.getWardsByDistrict(districtCode);
            setWards(allWards);
            setFilteredWards(allWards);
        } else {
            setWards([]);
            setFilteredWards([]);
        }
        setSearch('');
    }, [districtCode]);

    useEffect(() => {
        if (search) {
            const filtered = wards.filter(ward =>
                ward.name.toLowerCase().includes(search.toLowerCase()) ||
                ward.name_with_type.toLowerCase().includes(search.toLowerCase())
            );
            setFilteredWards(filtered);
        } else {
            setFilteredWards(wards);
        }
    }, [search, wards]);

    const selectedWard = wards.find(ward => ward.code === value);

    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                <Button
                    type="button"
                    variant="outline"
                    role="combobox"
                    aria-expanded={open}
                    className="w-full justify-between"
                    disabled={disabled || !districtCode}
                >
                    {selectedWard ? selectedWard.name_with_type : placeholder}
                    <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                </Button>
            </PopoverTrigger>
            <PopoverContent className="w-full p-0">
                <Command>
                    <CommandInput
                        placeholder="Tìm kiếm phường/xã..."
                        value={search}
                        onValueChange={setSearch}
                    />
                    <CommandList>
                        {filteredWards.length === 0 ? (
                            <CommandEmpty>Không tìm thấy phường/xã nào.</CommandEmpty>
                        ) : (
                            <CommandGroup>
                                {filteredWards.map((ward) => (
                                    <CommandItem
                                        key={ward.code}
                                        value={ward.name}
                                        onSelect={() => {
                                            onValueChange(ward.code);
                                            setOpen(false);
                                            setSearch('');
                                        }}
                                    >
                                        <Check
                                            className={cn(
                                                "mr-2 h-4 w-4",
                                                value === ward.code ? "opacity-100" : "opacity-0"
                                            )}
                                        />
                                        {ward.name_with_type}
                                    </CommandItem>
                                ))}
                            </CommandGroup>
                        )}
                    </CommandList>
                </Command>
            </PopoverContent>
        </Popover>
    );
}