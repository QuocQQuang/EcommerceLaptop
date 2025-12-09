import { ProductCard } from '@/components/molecules/ProductCard'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

// Mock next/image
jest.mock('next/image', () => {
    return function MockImage({ src, alt, ...props }) {
        return <img src={src} alt={alt} {...props} />
    }
})

// Mock Zustand store
jest.mock('@/stores/useCartStore', () => ({
    useCartStore: () => ({
        addToCart: jest.fn(),
        items: [],
    }),
}))

const mockProduct = {
    id: '1',
    name: 'Test Laptop',
    slug: 'test-laptop',
    shortDescription: 'A great laptop for testing',
    price: 15000000,
    originalPrice: 18000000,
    discount: 17,
    rating: 4.5,
    reviewCount: 123,
    images: ['/images/laptop-1.jpg'],
    inStock: true,
    brand: 'TestBrand',
    category: 'Laptop',
    tags: ['gaming', 'high-performance'],
    specifications: {
        processor: 'Intel i7',
        memory: '16GB RAM',
        storage: '512GB SSD',
        graphics: 'NVIDIA GTX 1660',
        display: '15.6" Full HD',
        weight: '2.1kg',
    },
    features: ['Backlit Keyboard', 'Fast Charging'],
}

describe('ProductCard', () => {
    it('renders product information correctly', () => {
        render(<ProductCard product={mockProduct} />)

        expect(screen.getByText('Test Laptop')).toBeInTheDocument()
        expect(screen.getByText('A great laptop for testing')).toBeInTheDocument()
        expect(screen.getByText('15.000.000')).toBeInTheDocument()
        expect(screen.getByText('18.000.000')).toBeInTheDocument()
        expect(screen.getByText('-17%')).toBeInTheDocument()
        expect(screen.getByText('4.5')).toBeInTheDocument()
        expect(screen.getByText('(123 nh gi)')).toBeInTheDocument()
    })

    it('displays correct image with alt text', () => {
        render(<ProductCard product={mockProduct} />)

        const image = screen.getByAltText('Test Laptop')
        expect(image).toBeInTheDocument()
        expect(image).toHaveAttribute('src', '/images/laptop-1.jpg')
    })

    it('shows "Ht hng" when product is out of stock', () => {
        const outOfStockProduct = { ...mockProduct, inStock: false }
        render(<ProductCard product={outOfStockProduct} />)

        expect(screen.getByText('Ht hng')).toBeInTheDocument()
    })

    it('calls addToCart when "Thm vo gi" button is clicked', async () => {
        const user = userEvent.setup()
        const mockAddToCart = jest.fn()

        // Mock the store to return our mock function
        jest.doMock('@/stores/useCartStore', () => ({
            useCartStore: () => ({
                addToCart: mockAddToCart,
                items: [],
            }),
        }))

        render(<ProductCard product={mockProduct} />)

        const addButton = screen.getByText('Thm vo gi')
        await user.click(addButton)

        expect(mockAddToCart).toHaveBeenCalledWith(mockProduct)
    })

    it('navigates to product detail page when clicked', async () => {
        const user = userEvent.setup()
        render(<ProductCard product={mockProduct} />)

        const productLink = screen.getByRole('link')
        expect(productLink).toHaveAttribute('href', '/products/test-laptop')
    })

    it('handles missing optional fields gracefully', () => {
        const minimalProduct = {
            id: '2',
            name: 'Minimal Laptop',
            slug: 'minimal-laptop',
            price: 10000000,
            images: ['/images/default.jpg'],
            inStock: true,
        }

        render(<ProductCard product={minimalProduct} />)

        expect(screen.getByText('Minimal Laptop')).toBeInTheDocument()
        expect(screen.getByText('10.000.000')).toBeInTheDocument()
    })

    it('applies hover effects correctly', async () => {
        const user = userEvent.setup()
        render(<ProductCard product={mockProduct} />)

        const card = screen.getByRole('link')

        await user.hover(card)
        expect(card).toHaveClass('hover:shadow-lg')
    })
})