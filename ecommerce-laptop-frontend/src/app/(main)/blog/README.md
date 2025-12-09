# Blog System vi Quill React

## Tng quan

H thng blog c xy dng vi React Quill editor, h tr chn nh t Unsplash v link sn phm t ng.

## Tnh nng chnh

### 1. Editor Blog vi Quill React
- **Rich text editing**: H tr y  cc tnh nng formatting
- **Custom toolbar**: Thanh cng c ty chnh vi cc nt chn nh v sn phm
- **Auto-save**: T ng lu nhp
- **Reading time**: Tnh ton thi gian c t ng

### 2. Chn nh t Unsplash
- **Image picker**: Giao din chn nh trc quan
- **Search functionality**: Tm kim nh theo t kha
- **Attribution**: T ng thm attribution cho nh
- **Alt text**: H tr alt text cho accessibility

### 3. Link sn phm thng minh
- **Product picker**: Chn sn phm t danh sch
- **Auto card generation**: T ng to card sn phm
- **Price formatting**: Hin th gi theo nh dng VND
- **Action buttons**: Nt "Xem sn phm" v "Thm vo gi"

## Cu trc file

```
src/app/(main)/blog/
 page.tsx                    # Trang danh sch blog
 [slug]/page.tsx            # Trang chi tit blog
 demo/page.tsx              # Trang demo
 README.md                  # Hng dn ny

src/components/blog/
 UnsplashImagePicker.tsx    # Component chn nh Unsplash
 ProductLinkPicker.tsx      # Component chn sn phm

src/hooks/
 useProductsQuery.ts        # Hook query sn phm
```

## Cch s dng

### 1. To blog mi
```typescript
// Truy cp: /admin/blog/posts/new
// S dng editor vi cc nt:
// - "nh t Unsplash": Chn nh min ph
// - "Link sn phm": Chn link sn phm
```

### 2. Format link sn phm
```html
<!-- Format trong editor -->
[product:ID:Tn:Gi:nh]

<!-- V d -->
[product:1:ASUS ROG Strix G15:25990000:https://example.com/image.jpg]
```

### 3. Hin th blog
```typescript
// Trang danh sch: /blog
// Trang chi tit: /blog/[slug]
// Trang demo: /blog/demo
```

## API Integration

### Blog Service
```typescript
import { blogService } from '@/services/blogService';

// Ly danh sch blog
const blogs = await blogService.getBlogs({
  page: 1,
  pageSize: 12,
  status: 'published'
});

// Ly blog theo slug
const blog = await blogService.getBlogBySlug('my-blog-slug');

// To blog mi
const newBlog = await blogService.createBlog({
  title: 'Tiu ',
  content: 'Ni dung...',
  isPublished: true
});
```

### Product Query Hook
```typescript
import { useProductsQuery } from '@/hooks/useProductsQuery';

const { data: products, isLoading } = useProductsQuery({
  page: 1,
  pageSize: 100,
  search: 'laptop'
});
```

## Customization

### 1. Thm format mi cho Quill
```typescript
const quillFormats = [
  'header', 'bold', 'italic', 'underline', 'strike',
  'color', 'background', 'list', 'indent',
  'align', 'blockquote', 'code-block', 'link', 'image', 'video',
  'custom-format' // Thm format mi
];
```

### 2. Custom toolbar
```typescript
const quillModules = {
  toolbar: {
    container: [
      // ... existing toolbar items
      ['custom-button'] // Thm nt mi
    ],
    handlers: {
      'custom-button': () => {
        // X l nt custom
      }
    }
  }
};
```

### 3. Thm loi link mi
```typescript
// Trong ProductLinkPicker hoc to component mi
const handleCustomLinkInsert = (data: any) => {
  const customLink = `[custom:${data.type}:${data.value}]`;
  // Insert vo editor
};
```

## Styling

### CSS Classes
```css
/* Blog content styling */
.prose {
  /* Tailwind prose styling */
}

/* Product link cards */
.product-link-card {
  @apply border-l-4 border-l-blue-500 bg-blue-50;
}

/* Image attribution */
.image-attribution {
  @apply text-sm text-gray-500 italic;
}
```

## Performance

### Optimization
- **Dynamic imports**: ReactQuill c load ng  trnh SSR issues
- **Image lazy loading**: nh c load lazy
- **Pagination**: Danh sch blog c phn trang
- **Caching**: S dng React Query  cache data

### Bundle Size
- Quill editor: ~200KB
- Unsplash picker: ~50KB
- Product picker: ~30KB

## Troubleshooting

### Li thng gp

1. **Quill khng load**
   ```typescript
   // m bo s dng dynamic import
   const ReactQuill = dynamic(
     () => import('react-quill-new').then(mod => mod.default),
     { ssr: false }
   );
   ```

2. **nh Unsplash khng hin th**
   ```typescript
   // Kim tra CORS v API key
   // S dng proxy nu cn
   ```

3. **Product link khng parse**
   ```typescript
   // Kim tra regex pattern
   const productLinkRegex = /\[product:(\d+):([^:]+):([^:]*):([^\]]*)\]/g;
   ```

## Roadmap

### Tnh nng sp ti
- [ ] Video embedding t YouTube/Vimeo
- [ ] Code syntax highlighting
- [ ] Table editor
- [ ] Drag & drop images
- [ ] Collaborative editing
- [ ] Version history
- [ ] SEO optimization tools
- [ ] Social media preview

### Ci tin
- [ ] Mobile responsive editor
- [ ] Keyboard shortcuts
- [ ] Undo/Redo improvements
- [ ] Auto-save vi conflict resolution
- [ ] Export to PDF/Word
- [ ] Multi-language support

## Contributing

1. Fork repository
2. To feature branch
3. Commit changes
4. Push to branch
5. To Pull Request

## License

MIT License - Xem file LICENSE  bit thm chi tit.
