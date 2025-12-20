'use client';

import { useRouter } from 'next/navigation';
import { useEffect } from 'react';

export default function CategoriesRedirect() {
  const router = useRouter();

  useEffect(() => {
    router.replace('/admin/products/categories');
  }, [router]);

  return (
    <div className="flex items-center justify-center h-64">
      <p>ang chuyn hng...</p>
    </div>
  );
}
