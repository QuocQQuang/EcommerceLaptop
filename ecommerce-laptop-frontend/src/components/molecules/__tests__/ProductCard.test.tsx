import { ProductCard } from '@/components/molecules/ProductCard'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

// Mock next/image
jest.mock('next/image', () => {
    return function MockImage({ src, alt }: { src: string; alt: string; [key: string]: unknown }) {
        return <img src={src} alt={alt} />
    }
})

const mockAddToCart = jest.fn()

// Mock useCart hook  the hook ProductCard actually uses
jest.mock('@/hooks/useCart', () => ({
    useCart: () => ({
        addToCart: mockAddToCart,
        items: [],
        total: 0,
        itemCount: 0,
        removeItem: jest.fn(),
        updateQuantity: jest.fn(),
        clearCart: jest.fn(),
    }),
}))

// Mock additional stores used by ProductCard
jest.mock('@/store/wishlistStore', () => ({
    useWishlistStore: () => ({
        toggleItem: jest.fn(),
        isInWishlist: () => false,
    }),
}))

jest.mock('@/store/uiStore', () => ({
    useUIStore: () => ({
        setQuickViewModal: jest.fn(),
        isCartSidebarOpen: false,
        setCartSidebarOpen: jest.fn(),
    }),
}))

jest.mock('@/store/compareStore', () => ({
    useCompareStore: () => ({
        toggleItem: jest.fn(),
        isInCompare: () => false,
    }),
}))

// Prevent currency update hook from causing issues
jest.mock('@/hooks/useCurrencyUpdate', () => ({
    useCurrencyUpdate: () => 0,
}))

// Product mock using correct Product interface fields
const mockProduct = {
    id: 1,
    name: 'Test Laptop',
    slug: 'test-laptop',
    sku: 'TL-001',
    shortDescription: 'A great laptop for testing',
    description: 'Full description of the test laptop',
    brand: 'TestBrand',
    model: 'TL1',
    type: 'Laptop' as const,
    price: 18000000,
    discountPrice: 15000000,
    isActive: true,
    isFeatured: false,
    stockQuantity: 10,
    imageUrl: '/images/laptop-1.jpg',
    images: [],
    specifications: [],
    categories: [],
    createdAt: '2024-01-01',
    updatedAt: '2024-01-01',
    reviewSummary: {
        averageRating: 4.5,
        totalReviews: 123,
    },
}

describe('ProductCard', () => {
    beforeEach(() => {
        jest.clearAllMocks()
    })

    it('renders product information correctly', () => {
        render(<ProductCard product={mockProduct} />)

        expect(screen.getByText('Test Laptop')).toBeInTheDocument()
        expect(screen.getByText('TestBrand')).toBeInTheDocument()
        // discount badge: (18000000 - 15000000) / 18000000 * 100  17%
        expect(screen.getByText(/-\d+%/)).toBeInTheDocument()
    })

    it('displays correct image with alt text', () => {
        render(<ProductCard product={mockProduct} />)

        const image = screen.getByAltText('Test Laptop')
        expect(image).toBeInTheDocument()
        expect(image).toHaveAttribute('src', '/images/laptop-1.jpg')
    })

    it('shows "Ht hng" when product is out of stock', () => {
        const outOfStockProduct = { ...mockProduct, stockQuantity: 0 }
        render(<ProductCard product={outOfStockProduct} />)

        // ProductCard renders "Ht hng" in both the badge and the stock status span
        const elements = screen.getAllByText('Ht hng')
        expect(elements.length).toBeGreaterThan(0)
    })

    it('calls addToCart when "Thm vo gi" button is clicked', async () => {
        const user = userEvent.setup()
        render(<ProductCard product={mockProduct} />)

        const addButton = screen.getByText('Thm vo gi')
        await user.click(addButton)

        expect(mockAddToCart).toHaveBeenCalledWith(mockProduct, 1)
    })

    it('navigates to product detail page when clicked', () => {
        render(<ProductCard product={mockProduct} />)

        const productLink = screen.getByRole('link')
        expect(productLink).toHaveAttribute('href', '/products/test-laptop')
    })

    it('handles missing optional fields gracefully', () => {
        const minimalProduct = {
            ...mockProduct,
            id: 2,
            name: 'Minimal Laptop',
            slug: 'minimal-laptop',
            discountPrice: undefined,
            reviewSummary: undefined,
        }

        render(<ProductCard product={minimalProduct} />)

        expect(screen.getByText('Minimal Laptop')).toBeInTheDocument()
        expect(screen.getByText('Cn hng')).toBeInTheDocument()
    })

    it('applies hover effects correctly', () => {
        render(<ProductCard product={mockProduct} />)

        const link = screen.getByRole('link')
        // The Card element (first child of link) should have hover:shadow-lg class
        const card = link.firstElementChild
        if (card) {
            expect(card.className).toContain('hover:shadow-lg')
        } else {
            expect(link).toBeInTheDocument()
        }
    })

    it('shows "Cn hng" when product is in stock', () => {
        render(<ProductCard product={mockProduct} />)
        expect(screen.getByText('Cn hng')).toBeInTheDocument()
    })
})