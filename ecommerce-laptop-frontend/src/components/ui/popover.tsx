"use client"

import { cn } from "@/lib/utils";
import * as React from "react";

interface PopoverProps {
    children: React.ReactNode;
    open?: boolean;
    onOpenChange?: (open: boolean) => void;
}

interface PopoverContextType {
    open: boolean;
    onOpenChange: (open: boolean) => void;
}

const PopoverContext = React.createContext<PopoverContextType | null>(null);

const Popover = ({ children, open = false, onOpenChange }: PopoverProps) => {
    const [isOpen, setIsOpen] = React.useState(open);

    React.useEffect(() => {
        setIsOpen(open);
    }, [open]);

    const handleOpenChange = React.useCallback((newOpen: boolean) => {
        setIsOpen(newOpen);
        onOpenChange?.(newOpen);
    }, [onOpenChange]);

    return (
        <PopoverContext.Provider value={{ open: isOpen, onOpenChange: handleOpenChange }}>
            <div className="relative">
                {children}
            </div>
        </PopoverContext.Provider>
    );
};

interface PopoverTriggerProps extends React.HTMLAttributes<HTMLDivElement> {
    asChild?: boolean;
}

const PopoverTrigger = React.forwardRef<HTMLDivElement, PopoverTriggerProps>(
    ({ className, children, asChild, ...props }, ref) => {
        const context = React.useContext(PopoverContext);
        if (!context) {
            throw new Error("PopoverTrigger must be used within a Popover");
        }

        const { open, onOpenChange } = context;

        if (asChild && React.isValidElement(children)) {
            const childProps = children.props as any;
            return React.cloneElement(children, {
                ...childProps,
                onClick: (e: React.MouseEvent) => {
                    childProps.onClick?.(e);
                    onOpenChange(!open);
                },
            });
        }

        return (
            <div
                ref={ref}
                className={cn("cursor-pointer", className)}
                onClick={() => onOpenChange(!open)}
                {...props}
            >
                {children}
            </div>
        );
    }
);
PopoverTrigger.displayName = "PopoverTrigger";

interface PopoverContentProps extends React.HTMLAttributes<HTMLDivElement> {
    align?: "start" | "center" | "end";
    sideOffset?: number;
}

const PopoverContent = React.forwardRef<HTMLDivElement, PopoverContentProps>(
    ({ className, align = "center", sideOffset = 4, children, ...props }, ref) => {
        const context = React.useContext(PopoverContext);
        if (!context) {
            throw new Error("PopoverContent must be used within a Popover");
        }

        const { open, onOpenChange } = context;
        const contentRef = React.useRef<HTMLDivElement>(null);

        React.useEffect(() => {
            const handleClickOutside = (event: MouseEvent) => {
                if (contentRef.current && !contentRef.current.contains(event.target as Node)) {
                    onOpenChange(false);
                }
            };

            if (open) {
                document.addEventListener("mousedown", handleClickOutside);
                return () => document.removeEventListener("mousedown", handleClickOutside);
            }
        }, [open, onOpenChange]);

        if (!open) return null;

        const alignmentClasses = {
            start: "left-0",
            center: "left-1/2 -translate-x-1/2",
            end: "right-0",
        };

        return (
            <div
                ref={contentRef}
                className={cn(
                    "absolute z-50 w-72 rounded-md border bg-popover p-4 text-popover-foreground shadow-md outline-none",
                    "top-full mt-1",
                    alignmentClasses[align],
                    className
                )}
                style={{ marginTop: sideOffset }}
                {...props}
            >
                {children}
            </div>
        );
    }
);
PopoverContent.displayName = "PopoverContent";

export { Popover, PopoverContent, PopoverTrigger };
