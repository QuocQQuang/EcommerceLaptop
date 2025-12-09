"use client"

import { cn } from "@/lib/utils";
import * as React from "react";

interface RadioGroupProps {
    value?: string;
    onValueChange?: (value: string) => void;
    children: React.ReactNode;
    className?: string;
}

interface RadioGroupItemProps {
    value: string;
    id?: string;
    className?: string;
    disabled?: boolean;
}

const RadioGroupContext = React.createContext<{
    value?: string;
    onValueChange?: (value: string) => void;
}>({});

const RadioGroup = React.forwardRef<HTMLDivElement, RadioGroupProps>(
    ({ className, value, onValueChange, children, ...props }, ref) => {
        return (
            <RadioGroupContext.Provider value={{ value, onValueChange }}>
                <div
                    className={cn("grid gap-2", className)}
                    ref={ref}
                    role="radiogroup"
                    {...props}
                >
                    {children}
                </div>
            </RadioGroupContext.Provider>
        )
    }
)
RadioGroup.displayName = "RadioGroup"

const RadioGroupItem = React.forwardRef<HTMLInputElement, RadioGroupItemProps>(
    ({ className, value, id, disabled, ...props }, ref) => {
        const context = React.useContext(RadioGroupContext);
        const isChecked = context.value === value;

        return (
            <input
                ref={ref}
                type="radio"
                id={id}
                value={value}
                checked={isChecked}
                disabled={disabled}
                onChange={() => context.onValueChange?.(value)}
                className={cn(
                    "aspect-square h-4 w-4 rounded-full border border-primary text-primary ring-offset-background focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",
                    className
                )}
                {...props}
            />
        )
    }
)
RadioGroupItem.displayName = "RadioGroupItem"

export { RadioGroup, RadioGroupItem };

