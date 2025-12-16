"use client"

import { Check, ChevronsUpDown, X } from "lucide-react"
import * as React from "react"

import { Button } from "@/components/ui/button"
import {
    Command,
    CommandEmpty,
    CommandGroup,
    CommandInput,
    CommandItem,
    CommandList,
} from "@/components/ui/command"
import { Input } from "@/components/ui/input"
import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from "@/components/ui/popover"
import { cn } from "@/lib/utils"

export interface ComboboxOption {
    value: string
    label: string
}

interface ComboboxProps {
    options: ComboboxOption[]
    value: string
    onValueChange: (value: string) => void
    placeholder?: string
    searchPlaceholder?: string
    emptyText?: string
    allowCustom?: boolean
    customPlaceholder?: string
    className?: string
    disabled?: boolean
}

export function Combobox({
    options,
    value,
    onValueChange,
    placeholder = "Select option...",
    searchPlaceholder = "Search...",
    emptyText = "No option found.",
    allowCustom = true,
    customPlaceholder = "Enter custom value...",
    className,
    disabled = false
}: ComboboxProps) {
    const [open, setOpen] = React.useState(false)
    const [customValue, setCustomValue] = React.useState("")
    const [showCustomInput, setShowCustomInput] = React.useState(false)
    const [searchValue, setSearchValue] = React.useState("")

    const selectedOption = options.find((option) => option.value === value)

    // Filter options based on search
    const filteredOptions = React.useMemo(() => {
        if (!searchValue) return options
        return options.filter(option =>
            option.label.toLowerCase().includes(searchValue.toLowerCase())
        )
    }, [options, searchValue])

    const handleSelect = (selectedValue: string) => {
        if (selectedValue === "other") {
            setShowCustomInput(true)
            setOpen(false)
        } else {
            onValueChange(selectedValue)
            setOpen(false)
            setShowCustomInput(false)
        }
    }

    const handleCustomSubmit = () => {
        if (customValue.trim()) {
            onValueChange(customValue.trim())
            setShowCustomInput(false)
            setCustomValue("")
        }
    }

    const handleCustomCancel = () => {
        setShowCustomInput(false)
        setCustomValue("")
    }

    const handleClear = () => {
        onValueChange("")
        setShowCustomInput(false)
        setCustomValue("")
    }

    const handleOpenChange = (newOpen: boolean) => {
        setOpen(newOpen)
        if (newOpen) {
            setSearchValue("")
        }
    }

    return (
        <div className={cn("space-y-2", className)}>
            <Popover open={open} onOpenChange={handleOpenChange}>
                <PopoverTrigger asChild>
                    <Button
                        variant="outline"
                        role="combobox"
                        aria-expanded={open}
                        className="w-full justify-between"
                        disabled={disabled}
                    >
                        {selectedOption ? selectedOption.label : placeholder}
                        <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="w-full p-0">
                    <Command>
                        <CommandInput
                            placeholder={searchPlaceholder}
                            value={searchValue}
                            onValueChange={setSearchValue}
                        />
                        <CommandList>
                            {filteredOptions.length === 0 ? (
                                <CommandEmpty>
                                    {emptyText}
                                </CommandEmpty>
                            ) : (
                                <CommandGroup>
                                    {filteredOptions.map((option) => (
                                        <CommandItem
                                            key={option.value}
                                            value={option.value}
                                            onSelect={handleSelect}
                                        >
                                            <Check
                                                className={cn(
                                                    "mr-2 h-4 w-4",
                                                    value === option.value ? "opacity-100" : "opacity-0"
                                                )}
                                            />
                                            {option.label}
                                        </CommandItem>
                                    ))}
                                    {allowCustom && (
                                        <CommandItem
                                            value="other"
                                            onSelect={handleSelect}
                                            className="text-blue-600"
                                        >
                                            <Check className="mr-2 h-4 w-4 opacity-0" />
                                            Khc...
                                        </CommandItem>
                                    )}
                                </CommandGroup>
                            )}
                        </CommandList>
                    </Command>
                </PopoverContent>
            </Popover>

            {showCustomInput && (
                <div className="space-y-2">
                    <Input
                        placeholder={customPlaceholder}
                        value={customValue}
                        onChange={(e) => setCustomValue(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === "Enter") {
                                handleCustomSubmit()
                            } else if (e.key === "Escape") {
                                handleCustomCancel()
                            }
                        }}
                    />
                    <div className="flex gap-2">
                        <Button size="sm" onClick={handleCustomSubmit}>
                            Xc nhn
                        </Button>
                        <Button size="sm" variant="outline" onClick={handleCustomCancel}>
                            Hy
                        </Button>
                    </div>
                </div>
            )}

            {value && (
                <div className="flex items-center gap-2">
                    <span className="text-sm text-muted-foreground">
                         chn: {selectedOption?.label || value}
                    </span>
                    <Button
                        size="sm"
                        variant="ghost"
                        onClick={handleClear}
                        className="h-6 w-6 p-0"
                    >
                        <X className="h-3 w-3" />
                    </Button>
                </div>
            )}
        </div>
    )
}

// Helper function to convert array of strings to ComboboxOption[]
export const createOptions = (items: string[]): ComboboxOption[] => {
    return items.map((item) => ({
        value: item,
        label: item
    }))
}
