'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { AlertTriangle, RefreshCw, Trash2 } from 'lucide-react';
import { useState } from 'react';

export interface DeleteOption {
  id: string;
  name: string;
  productCount?: number;
}

export interface AdvancedDeleteDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  itemName: string;
  itemType: 'brand' | 'category';
  productCount: number;
  childCount?: number;
  availableOptions: DeleteOption[];
  onSimpleDelete: () => Promise<void>;
  onReassignAndDelete: (newItemId: string) => Promise<void>;
  onForceDelete: () => Promise<void>;
  isLoading?: boolean;
}

export default function AdvancedDeleteDialog({
  open,
  onOpenChange,
  itemName,
  itemType,
  productCount,
  childCount = 0,
  availableOptions,
  onSimpleDelete,
  onReassignAndDelete,
  onForceDelete,
  isLoading = false
}: AdvancedDeleteDialogProps) {
  const [deleteMethod, setDeleteMethod] = useState<'simple' | 'reassign' | 'force'>('simple');
  const [selectedTargetId, setSelectedTargetId] = useState<string>('');

  const canSimpleDelete = productCount === 0 && childCount === 0;
  const hasProducts = productCount > 0;
  const hasChildren = childCount > 0;

  const handleConfirm = async () => {
    try {
      switch (deleteMethod) {
        case 'simple':
          await onSimpleDelete();
          break;
        case 'reassign':
          if (selectedTargetId) {
            await onReassignAndDelete(selectedTargetId);
          }
          break;
        case 'force':
          await onForceDelete();
          break;
      }
      onOpenChange(false);
      setDeleteMethod('simple');
      setSelectedTargetId('');
    } catch (error) {
      console.error('Delete operation failed:', error);
    }
  };

  const getMethodDescription = () => {
    switch (deleteMethod) {
      case 'simple':
        return canSimpleDelete
          ? `Xóa ${itemType === 'brand' ? 'thương hiệu' : 'danh mục'} này vĩnh viễn.`
          : `Không thể xóa ${itemType === 'brand' ? 'thương hiệu' : 'danh mục'} này vì có ${hasProducts ? `${productCount} sản phẩm` : ''}${hasProducts && hasChildren ? ' và ' : ''}${hasChildren ? `${childCount} danh mục con` : ''} liên quan.`;

      case 'reassign':
        return `Chuyển tất cả ${productCount} sản phẩm sang ${itemType === 'brand' ? 'thương hiệu' : 'danh mục'} khác, sau đó xóa ${itemType === 'brand' ? 'thương hiệu' : 'danh mục'} này.`;

      case 'force':
        return itemType === 'brand'
          ? `Xóa thương hiệu và tự động đặt trạng thái của ${productCount} sản phẩm liên quan.`
          : `Xóa danh mục, đặt trạng thái của ${productCount} sản phẩm${hasChildren ? ` và chuyển ${childCount} danh mục con lên cấp cha` : ''}.`;

      default:
        return '';
    }
  };

  const isConfirmDisabled = () => {
    if (isLoading) return true;
    if (deleteMethod === 'simple' && !canSimpleDelete) return true;
    if (deleteMethod === 'reassign' && !selectedTargetId) return true;
    return false;
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-orange-500" />
            Xóa {itemType === 'brand' ? 'thương hiệu' : 'danh mục'}: {itemName}
          </DialogTitle>
          <DialogDescription>
            {itemType === 'brand' ? 'Thương hiệu' : 'Danh mục'} này có:
            {hasProducts && (
              <Badge variant="secondary" className="ml-2">
                {productCount} sản phẩm
              </Badge>
            )}
            {hasChildren && (
              <Badge variant="secondary" className="ml-2">
                {childCount} danh mục con
              </Badge>
            )}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {/* Simple Delete Option */}
          <div className="space-y-2">
            <div className="flex items-center space-x-2">
              <input
                type="radio"
                id="simple"
                name="deleteMethod"
                value="simple"
                checked={deleteMethod === 'simple'}
                onChange={(e) => setDeleteMethod(e.target.value as any)}
                disabled={!canSimpleDelete}
              />
              <Label htmlFor="simple" className={!canSimpleDelete ? 'text-muted-foreground' : ''}>
                <div className="flex items-center gap-2">
                  <Trash2 className="h-4 w-4" />
                  Xóa trực tiếp
                </div>
              </Label>
            </div>
          </div>

          {/* Reassign Option */}
          {hasProducts && availableOptions.length > 0 && (
            <div className="space-y-2">
              <div className="flex items-center space-x-2">
                <input
                  type="radio"
                  id="reassign"
                  name="deleteMethod"
                  value="reassign"
                  checked={deleteMethod === 'reassign'}
                  onChange={(e) => setDeleteMethod(e.target.value as any)}
                />
                <Label htmlFor="reassign">
                  <div className="flex items-center gap-2">
                    <RefreshCw className="h-4 w-4" />
                    Chuyển sản phẩm và xóa
                  </div>
                </Label>
              </div>

              {deleteMethod === 'reassign' && (
                <div className="ml-6 space-y-2">
                  <Label htmlFor="target-select">
                    Chọn {itemType === 'brand' ? 'thương hiệu' : 'danh mục'} đích:
                  </Label>
                  <Select value={selectedTargetId} onValueChange={setSelectedTargetId}>
                    <SelectTrigger>
                      <SelectValue placeholder={`Chọn ${itemType === 'brand' ? 'thương hiệu' : 'danh mục'} khác...`} />
                    </SelectTrigger>
                    <SelectContent>
                      {availableOptions.map((option) => (
                        <SelectItem key={option.id} value={option.id}>
                          <div className="flex items-center justify-between w-full">
                            <span>{option.name}</span>
                            {option.productCount !== undefined && (
                              <Badge variant="outline" className="ml-2">
                                {option.productCount} SP
                              </Badge>
                            )}
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}
            </div>
          )}

          {/* Force Delete Option */}
          <div className="space-y-2">
            <div className="flex items-center space-x-2">
              <input
                type="radio"
                id="force"
                name="deleteMethod"
                value="force"
                checked={deleteMethod === 'force'}
                onChange={(e) => setDeleteMethod(e.target.value as any)}
              />
              <Label htmlFor="force" className="text-red-600">
                <div className="flex items-center gap-2">
                  <AlertTriangle className="h-4 w-4" />
                  Xóa bắt buộc (Nguy hiểm)
                </div>
              </Label>
            </div>
          </div>

          {/* Method Description */}
          <div className="p-3 bg-muted rounded-lg">
            <p className="text-sm text-muted-foreground">
              <strong>Hành động:</strong> {getMethodDescription()}
            </p>
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isLoading}
          >
            Hủy
          </Button>
          <Button
            variant={deleteMethod === 'force' ? 'destructive' : 'default'}
            onClick={handleConfirm}
            disabled={isConfirmDisabled()}
            className={deleteMethod === 'force' ? 'bg-red-600 hover:bg-red-700' : ''}
          >
            {isLoading ? 'Đang xử lý...' : 'Xác nhận xóa'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
