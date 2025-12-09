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
          ? `Xa ${itemType === 'brand' ? 'thng hiu' : 'danh mc'} ny vnh vin.`
          : `Khng th xa ${itemType === 'brand' ? 'thng hiu' : 'danh mc'} ny v c ${hasProducts ? `${productCount} sn phm` : ''}${hasProducts && hasChildren ? ' v ' : ''}${hasChildren ? `${childCount} danh mc con` : ''} lin quan.`;

      case 'reassign':
        return `Chuyn tt c ${productCount} sn phm sang ${itemType === 'brand' ? 'thng hiu' : 'danh mc'} khc, sau  xa ${itemType === 'brand' ? 'thng hiu' : 'danh mc'} ny.`;

      case 'force':
        return itemType === 'brand'
          ? `Xa thng hiu v t ng TT trng thi ca ${productCount} sn phm lin quan.`
          : `Xa danh mc, TT trng thi ca ${productCount} sn phm${hasChildren ? ` v chuyn ${childCount} danh mc con ln cp cha` : ''}.`;

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
            Xa {itemType === 'brand' ? 'thng hiu' : 'danh mc'}: {itemName}
          </DialogTitle>
          <DialogDescription>
            {itemType === 'brand' ? 'Thng hiu' : 'Danh mc'} ny c:
            {hasProducts && (
              <Badge variant="secondary" className="ml-2">
                {productCount} sn phm
              </Badge>
            )}
            {hasChildren && (
              <Badge variant="secondary" className="ml-2">
                {childCount} danh mc con
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
                  Xa trc tip
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
                    Chuyn sn phm v xa
                  </div>
                </Label>
              </div>

              {deleteMethod === 'reassign' && (
                <div className="ml-6 space-y-2">
                  <Label htmlFor="target-select">
                    Chn {itemType === 'brand' ? 'thng hiu' : 'danh mc'} ch:
                  </Label>
                  <Select value={selectedTargetId} onValueChange={setSelectedTargetId}>
                    <SelectTrigger>
                      <SelectValue placeholder={`Chn ${itemType === 'brand' ? 'thng hiu' : 'danh mc'} khc...`} />
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
                  Xa p buc (Nguy him)
                </div>
              </Label>
            </div>
          </div>

          {/* Method Description */}
          <div className="p-3 bg-muted rounded-lg">
            <p className="text-sm text-muted-foreground">
              <strong>Hnh ng:</strong> {getMethodDescription()}
            </p>
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isLoading}
          >
            Hy
          </Button>
          <Button
            variant={deleteMethod === 'force' ? 'destructive' : 'default'}
            onClick={handleConfirm}
            disabled={isConfirmDisabled()}
            className={deleteMethod === 'force' ? 'bg-red-600 hover:bg-red-700' : ''}
          >
            {isLoading ? 'ang x l...' : 'Xc nhn xa'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}