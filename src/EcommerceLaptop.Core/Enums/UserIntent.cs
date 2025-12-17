namespace EcommerceLaptop.Core.Enums
{
    public enum UserIntent
    {
        Unknown,
        ProductSearch,
        ProductAdvice, // New intent for consultation without forcing visual search results
        GeneralChat,
        Support,
        OrderStatus,
        CartManagement,
        AccountManagement
    }
}
