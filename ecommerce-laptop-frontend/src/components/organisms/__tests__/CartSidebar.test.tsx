import { CartSidebar } from '@/components/organisms/CartSidebar'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

// Mock next/image
jest.mock('next/image', () => {
    return function MockImage({ src, alt, ...props }: any) {
        return <img src={src} alt={alt} {...props} />
    }
})

// Mock Zustand store
const mockCartStore = {
    isOpen: true,
    items: [
        {
            id: '1',
            name: 'Test Laptop',
            price: 15000000,
            image: '/test-image.jpg',
            quantity: 2,
        },
        {
            id: '2',
            name: 'Gaming Mouse',
            price: 500000,
            image: '/mouse.jpg',
            quantity: 1,
        },
    ],
    totalAmount: 30500000,
    totalItems: 3,
    setOpen: jest.fn(),
    removeFromCart: jest.fn(),
    updateQuantity: jest.fn(),
    clearCart: jest.fn(),
}

jest.mock('@/stores/useCartStore', () => ({
    useCartStore: () => mockCartStore,
}))

// Mock next/router
jest.mock('next/navigation', () => ({
    useRouter: () => ({
        push: jest.fn(),
        replace: jest.fn(),
        prefetch: jest.fn(),
    }),
}))

describe('CartSidebar', () => {
    beforeEach(() => {
        jest.clearAllMocks()
    })

    it('renders cart items correctly', () => {
        render(<CartSidebar />)

        expect(screen.getByText('Gi hng (3)')).toBeInTheDocument()
        expect(screen.getByText('Test Laptop')).toBeInTheDocument()
        expect(screen.getByText('Gaming Mouse')).toBeInTheDocument()
        expect(screen.getByText('15.000.000')).toBeInTheDocument()
        expect(screen.getByText('500.000')).toBeInTheDocument()
    })

    it('displays correct total amount', () => {
        render(<CartSidebar />)

        expect(screen.getByText('30.500.000')).toBeInTheDocument()
    })

    it('calls setOpen when close button is clicked', async () => {
        const user = userEvent.setup()
        render(<CartSidebar />)

        const closeButton = screen.getByRole('button', { name: /ng/i })
        await user.click(closeButton)

        expect(mockCartStore.setOpen).toHaveBeenCalledWith(false)
    })

    it('calls removeFromCart when remove button is clicked', async () => {
        const user = userEvent.setup()
        render(<CartSidebar />)

        const removeButtons = screen.getAllByRole('button', { name: /xa/i })
        await user.click(removeButtons[0])

        expect(mockCartStore.removeFromCart).toHaveBeenCalledWith('1')
    })

    it('calls updateQuantity when quantity is changed', async () => {
        const user = userEvent.setup()
        render(<CartSidebar />)

        const increaseButtons = screen.getAllByRole('button', { name: /\+/i })
        await user.click(increaseButtons[0])

        expect(mockCartStore.updateQuantity).toHaveBeenCalledWith('1', 3)
    })

    it('shows empty cart message when no items', () => {
        const emptyCartStore = {
            ...mockCartStore,
            items: [],
            totalItems: 0,
            totalAmount: 0,
        }

        jest.doMock('@/stores/useCartStore', () => ({
            useCartStore: () => emptyCartStore,
        }))

        render(<CartSidebar />)

        expect(screen.getByText('Gi hng trng')).toBeInTheDocument()
        expect(screen.getByText('Bn cha c sn phm no trong gi hng')).toBeInTheDocument()
    })

    it('navigates to checkout when checkout button is clicked', async () => {
        const mockPush = jest.fn()
        jest.doMock('next/navigation', () => ({
            useRouter: () => ({
                push: mockPush,
                replace: jest.fn(),
                prefetch: jest.fn(),
            }),
        }))

        const user = userEvent.setup()
        render(<CartSidebar />)

        const checkoutButton = screen.getByRole('button', { name: /thanh ton/i })
        await user.click(checkoutButton)

        expect(mockPush).toHaveBeenCalledWith('/checkout')
    })

    it('applies correct styling when sidebar is open', () => {
        render(<CartSidebar />)

        const sidebar = screen.getByTestId('cart-sidebar')
        expect(sidebar).toHaveClass('translate-x-0')
    })

    it('handles quantity input correctly', async () => {
        const user = userEvent.setup()
        render(<CartSidebar />)

        const quantityInputs = screen.getAllByDisplayValue('2')
        const firstInput = quantityInputs[0]

        await user.clear(firstInput)
        await user.type(firstInput, '5')

        expect(mockCartStore.updateQuantity).toHaveBeenCalledWith('1', 5)
    })

    it('disables decrease button when quantity is 1', () => {
        const singleItemStore = {
            ...mockCartStore,
            items: [
                {
                    id: '1',
                    name: 'Test Laptop',
                    price: 15000000,
                    image: '/test-image.jpg',
                    quantity: 1,
                },
            ],
        }

        jest.doMock('@/stores/useCartStore', () => ({
            useCartStore: () => singleItemStore,
        }))

        render(<CartSidebar />)

        const decreaseButton = screen.getByRole('button', { name: /-/i })
        expect(decreaseButton).toBeDisabled()
    })
})