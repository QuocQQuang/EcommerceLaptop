import { toast } from "sonner";

export interface ToastProps {
  title?: string;
  description?: string;
  variant?: "default" | "destructive" | "success";
}

export function useToast() {
  return {
    toast: ({ title, description, variant = "default" }: ToastProps) => {
      switch (variant) {
        case "destructive":
          toast.error(title || "Error", {
            description: description,
          });
          break;
        case "success":
          toast.success(title || "Success", {
            description: description,
          });
          break;
        default:
          toast(title || "Notification", {
            description: description,
          });
      }
    },
  };
}