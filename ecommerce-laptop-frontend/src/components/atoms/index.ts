import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Slider } from '@/components/ui/slider';

// Re-export all shadcn/ui components as atoms
export {
    Badge, Button, Card,
    CardContent,
    CardFooter,
    CardHeader,
    CardTitle, Checkbox, Input,
    Label, Slider
};

// Custom atomic components
// export * from './LoadingSpinner';
// export * from './Price';
// export * from './Rating';
// export * from './Image';