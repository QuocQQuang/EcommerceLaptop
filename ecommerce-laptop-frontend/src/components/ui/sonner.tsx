"use client"

import { useTheme } from "next-themes"
import { Toaster as Sonner, ToasterProps } from "sonner"

const Toaster = ({ ...props }: ToasterProps) => {
  const { theme = "system" } = useTheme()

  return (
    <Sonner
      theme={theme as ToasterProps["theme"]}
      className="toaster group"
      toastOptions={{
        style: {
          background: theme === "dark" ? "#1a1a1a" : "#ffffff",
          color: theme === "dark" ? "#f8f9fa" : "#0a0a0a",
          border: theme === "dark" ? "1px solid #333" : "1px solid #e5e7eb",
        },
        classNames: {
          toast: "bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 border border-gray-200 dark:border-gray-700",
          description: "text-gray-600 dark:text-gray-400",
          actionButton: "bg-blue-600 hover:bg-blue-700 text-white",
          cancelButton: "bg-gray-500 hover:bg-gray-600 text-white",
          error: "bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 border-red-200 dark:border-red-800",
          success: "bg-green-50 dark:bg-green-900/20 text-green-800 dark:text-green-200 border-green-200 dark:border-green-800",
          warning: "bg-yellow-50 dark:bg-yellow-900/20 text-yellow-800 dark:text-yellow-200 border-yellow-200 dark:border-yellow-800",
          info: "bg-blue-50 dark:bg-blue-900/20 text-blue-800 dark:text-blue-200 border-blue-200 dark:border-blue-800",
        },
      }}
      style={
        {
          "--normal-bg": theme === "dark" ? "#1a1a1a" : "#ffffff",
          "--normal-text": theme === "dark" ? "#f8f9fa" : "#0a0a0a",
          "--normal-border": theme === "dark" ? "#333" : "#e5e7eb",
          "--success-bg": theme === "dark" ? "rgba(34, 197, 94, 0.1)" : "rgba(34, 197, 94, 0.05)",
          "--success-text": theme === "dark" ? "#86efac" : "#166534",
          "--success-border": theme === "dark" ? "#16a34a" : "#22c55e",
          "--error-bg": theme === "dark" ? "rgba(239, 68, 68, 0.1)" : "rgba(239, 68, 68, 0.05)",
          "--error-text": theme === "dark" ? "#fca5a5" : "#dc2626",
          "--error-border": theme === "dark" ? "#dc2626" : "#ef4444",
          "--warning-bg": theme === "dark" ? "rgba(245, 158, 11, 0.1)" : "rgba(245, 158, 11, 0.05)",
          "--warning-text": theme === "dark" ? "#fbbf24" : "#d97706",
          "--warning-border": theme === "dark" ? "#d97706" : "#f59e0b",
          "--info-bg": theme === "dark" ? "rgba(59, 130, 246, 0.1)" : "rgba(59, 130, 246, 0.05)",
          "--info-text": theme === "dark" ? "#93c5fd" : "#2563eb",
          "--info-border": theme === "dark" ? "#2563eb" : "#3b82f6",
        } as React.CSSProperties
      }
      {...props}
    />
  )
}

export { Toaster }
